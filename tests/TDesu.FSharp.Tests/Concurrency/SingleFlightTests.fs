namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Concurrency

[<TestFixture>]
type SingleFlightTests() =

    /// Work that counts its runs and finishes only when the test releases it, so callers can be
    /// lined up behind a run that is provably still in progress.
    let gatedWork (runs: int ref) (release: Task<'T>) () =
        Interlocked.Increment(&runs.contents) |> ignore
        release

    [<Test>]
    member _.``concurrent callers for one key share a single run``() =
        task {
            let flights = SingleFlight<int, string>()
            let runs = ref 0
            let release = TaskCompletionSource<string>()

            let callers = [ for _ in 1..3 -> flights.Run(4, gatedWork runs release.Task) ]

            equals (flights.IsRunning 4) true
            release.SetResult "dc4"
            let! values = Task.WhenAll callers

            equals runs.Value 1
            equals values [| "dc4"; "dc4"; "dc4" |]
        }

    [<Test>]
    member _.``different keys run independently``() =
        task {
            let flights = SingleFlight<int, int>()
            let runs = ref 0
            let release = TaskCompletionSource<int>()

            let a = flights.Run(1, gatedWork runs release.Task)
            let b = flights.Run(2, gatedWork runs release.Task)

            equals flights.Count 2
            release.SetResult 7
            let! _ = Task.WhenAll [ a; b ]
            equals runs.Value 2
        }

    [<Test>]
    member _.``a finished run frees its key, so the next call runs again``() =
        task {
            let flights = SingleFlight<int, int>()
            let runs = ref 0

            let! first =
                flights.Run(
                    4,
                    fun () ->
                        task {
                            runs.Value <- runs.Value + 1
                            return 1
                        }
                )

            let! second =
                flights.Run(
                    4,
                    fun () ->
                        task {
                            runs.Value <- runs.Value + 1
                            return 2
                        }
                )

            equals (first, second) (1, 2)
            equals runs.Value 2
            equals (flights.IsRunning 4) false
            equals flights.Count 0
        }

    [<Test>]
    member _.``a failure reaches every caller of the run and is not replayed``() =
        task {
            let flights = SingleFlight<int, int>()
            let runs = ref 0
            let release = TaskCompletionSource<int>()

            let callers = [ for _ in 1..2 -> flights.Run(4, gatedWork runs release.Task) ]
            release.SetException(InvalidOperationException "AUTH_BYTES_INVALID")

            for caller in callers do
                let! outcome = Task.WhenAny(caller)
                equals outcome.IsFaulted true
                equals outcome.Exception.InnerException.Message "AUTH_BYTES_INVALID"

            let! retried = flights.Run(4, fun () -> task { return 9 })
            equals retried 9
            equals runs.Value 1
        }

    [<Test>]
    member _.``work that throws before returning a task still frees its key``() =
        task {
            let flights = SingleFlight<int, int>()

            let failed = flights.Run(4, fun () -> raise (InvalidOperationException "sync"))
            let! outcome = Task.WhenAny(failed)

            equals outcome.IsFaulted true
            equals (flights.IsRunning 4) false
        }

    [<Test>]
    member _.``a cancelled run is cancelled for everyone who joined it``() =
        task {
            let flights = SingleFlight<int, int>()
            let runs = ref 0
            let release = TaskCompletionSource<int>()

            let callers = [ for _ in 1..2 -> flights.Run(4, gatedWork runs release.Task) ]
            release.SetCanceled()

            for caller in callers do
                let! outcome = Task.WhenAny(caller)
                equals outcome.IsCanceled true

            equals (flights.IsRunning 4) false
        }

    [<Test>]
    member _.``a joiner that stops waiting does not stop the run``() =
        task {
            let flights = SingleFlight<int, int>()
            let runs = ref 0
            let release = TaskCompletionSource<int>()
            use giveUp = new CancellationTokenSource()

            let owner = flights.Run(4, gatedWork runs release.Task)
            let joiner = flights.Run(4, gatedWork runs release.Task).WaitAsync(giveUp.Token)

            giveUp.Cancel()
            let! joined = Task.WhenAny(joiner)
            equals joined.IsCanceled true

            release.SetResult 5
            let! value = owner
            equals value 5
            equals runs.Value 1
        }
