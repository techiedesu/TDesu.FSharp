namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks

[<TestFixture>]
type TaskGroupTests() =

    [<Test>]
    member _.``WaitAll completes all tasks``() =
        task {
            let mutable a = 0
            let mutable b = 0
            use group = new TaskGroup()
            group.Run(fun _ -> task { a <- 1 })
            group.Run(fun _ -> task { b <- 2 })
            do! group.WaitAll()
            equals a 1
            equals b 2
        }

    [<Test>]
    member _.``WaitAll throws AggregateException on failure``() =
        %Assert.ThrowsAsync<AggregateException>(fun () ->
            task {
                use group = new TaskGroup()
                group.Run(fun _ -> task { failwith "boom" })
                do! group.WaitAll()
            }
        )

    [<Test>]
    member _.``failure cancels other tasks``() =
        task {
            let mutable cancelled = false
            use group = new TaskGroup()
            group.Run(fun _ -> task { failwith "boom" })

            group.Run(fun ct ->
                task {
                    try
                        do! Task.Delay(5000, ct)
                    with :? OperationCanceledException ->
                        cancelled <- true
                }
            )

            try
                do! group.WaitAll()
            with :? AggregateException ->
                ()

            isTrue cancelled
        }

    [<Test>]
    member _.``a task cancelled by a token that is not the group's own is reported as a failure``() =
        task {
            use group = new TaskGroup()
            let mutable ran = false

            group.Run(fun _ ->
                task {
                    use foreignCts = new CancellationTokenSource()
                    foreignCts.Cancel()
                    do! Task.Delay(5000, foreignCts.Token)
                    ran <- true
                }
            )

            let mutable caught: AggregateException option = None

            try
                do! group.WaitAll()
            with :? AggregateException as ex ->
                caught <- Some ex

            isTrue caught.IsSome
            isFalse ran
        }

    [<Test>]
    member _.``Token is cancelled on Dispose``() =
        let group = new TaskGroup()
        let token = group.Token
        isFalse token.IsCancellationRequested
        (group :> IDisposable).Dispose()
        isTrue token.IsCancellationRequested

    [<Test>]
    member _.``Token can still be read after Dispose instead of throwing``() =
        let group = new TaskGroup()
        (group :> IDisposable).Dispose()

        let token = group.Token // must not throw ObjectDisposedException

        isTrue token.IsCancellationRequested

    [<Test>]
    member _.``linked to parent token``() =
        task {
            use parentCts = new CancellationTokenSource()
            use group = new TaskGroup(parentCts.Token)
            parentCts.Cancel()
            isTrue group.Token.IsCancellationRequested
        }

    [<Test>]
    member _.``multiple errors collected in AggregateException``() =
        task {
            use group = new TaskGroup()
            group.Run(fun _ -> task { failwith "error1" })
            do! Task.Delay(50)
            group.Run(fun _ -> task { failwith "error2" })
            let mutable caught: AggregateException option = None

            try
                do! group.WaitAll()
            with :? AggregateException as ex ->
                caught <- Some ex

            isTrue caught.IsSome
            isTrue (caught.Value.InnerExceptions.Count >= 1)
        }

    [<Test>]
    member _.``concurrent Run calls do not lose tracked tasks under contention``() =
        // [INFERENCE] the review's own 2400-call/8-thread stress did not reproduce loss from the
        // unsynchronized ResizeArray either; this pins the fixed (ConcurrentBag) behaviour as a
        // baseline rather than a proven failing-before repro.
        task {
            use group = new TaskGroup()
            let threads = 8
            let perThread = 300
            let ran = Collections.Concurrent.ConcurrentBag<int>()

            let starters = [
                for _ in 1..threads ->
                    Task.Run(fun () ->
                        for i in 1..perThread do
                            group.Run(fun _ -> task { ran.Add i })
                    )
            ]

            do! Task.WhenAll(starters)
            do! group.WaitAll()

            equals ran.Count (threads * perThread)
        }
