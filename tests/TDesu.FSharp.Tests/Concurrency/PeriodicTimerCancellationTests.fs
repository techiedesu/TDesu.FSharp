namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Tasks
open TDesu.FSharp.Concurrency

[<TestFixture>]
type PeriodicTimerCancellationTests() =

    [<Test>]
    member _.``startCounted invokes the action with sequentially increasing tick indices``() =
        task {
            // ARRANGE
            let seen = ResizeArray<int>()
            use cts = new CancellationTokenSource()

            let action tick =
                task {
                    seen.Add tick

                    if seen.Count >= 3 then
                        cts.Cancel()
                }

            // ACT
            let _ =
                PeriodicTimer.startCounted (TimeSpan.FromMilliseconds 20.) action cts.Token ignore

            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> seen.Count >= 3)

            // ASSERT
            isTrue reached
            equals (seen |> Seq.toList) [ 0; 1; 2 ]
        }

    [<Test>]
    member _.``cancelling the token stops the loop and completes the returned task``() =
        task {
            // ARRANGE
            let mutable count = 0
            use cts = new CancellationTokenSource()
            let action () = task { count <- count + 1 }

            // ACT
            let loopTask =
                PeriodicTimer.start (TimeSpan.FromMilliseconds 20.) action cts.Token ignore

            let! ticked = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> count >= 2)
            isTrue ticked
            cts.Cancel()

            let! completed =
                Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> loopTask.IsCompleted)

            // ASSERT
            isTrue completed
            equals loopTask.Status TaskStatus.RanToCompletion
        }

    [<Test>]
    member _.``the loop keeps ticking after the action throws, and onError observes the exception``() =
        task {
            // ARRANGE
            let mutable tickCount = 0
            let errors = ResizeArray<exn>()
            use cts = new CancellationTokenSource()

            let action () =
                task {
                    tickCount <- tickCount + 1

                    if tickCount = 1 then
                        failwith "boom"

                    if tickCount >= 3 then
                        cts.Cancel()
                }

            let onError (ex: exn) = errors.Add ex

            // ACT
            let _ = PeriodicTimer.start (TimeSpan.FromMilliseconds 20.) action cts.Token onError
            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> tickCount >= 3)

            // ASSERT
            isTrue reached
            equals tickCount 3
            equals errors.Count 1
            equals errors[0].Message "boom"
        }

[<TestFixture>]
type PeriodicTimerStartImmediateTests() =

    [<Test>]
    member _.``runs the action immediately, well before the first interval elapses``() =
        task {
            // ARRANGE
            use cts = new CancellationTokenSource()
            let sw = Diagnostics.Stopwatch.StartNew()
            let mutable firstTickMs = -1L

            let action () =
                task {
                    if firstTickMs < 0L then
                        firstTickMs <- sw.ElapsedMilliseconds

                    cts.Cancel()
                }

            // ACT
            let _ =
                PeriodicTimer.startImmediate (TimeSpan.FromSeconds 5.) action cts.Token ignore

            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> firstTickMs >= 0L)

            // ASSERT
            isTrue reached
            isTrue (firstTickMs < 1000L) // nowhere near the 5-second interval
        }

    [<Test>]
    member _.``keeps running after the action throws, and onError observes the exception``() =
        task {
            // ARRANGE
            let mutable tickCount = 0
            let errors = ResizeArray<exn>()
            use cts = new CancellationTokenSource()

            let action () =
                task {
                    tickCount <- tickCount + 1

                    if tickCount = 1 then
                        failwith "boom"

                    if tickCount >= 3 then
                        cts.Cancel()
                }

            // ACT
            let _ =
                PeriodicTimer.startImmediate (TimeSpan.FromMilliseconds 20.) action cts.Token (fun ex -> errors.Add ex)

            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> tickCount >= 3)

            // ASSERT
            isTrue reached
            equals tickCount 3
            equals errors.Count 1
            equals errors[0].Message "boom"
        }

    [<Test>]
    member _.``cancelling the token stops the loop and completes the returned task``() =
        task {
            // ARRANGE
            let mutable count = 0
            use cts = new CancellationTokenSource()
            let action () = task { count <- count + 1 }

            // ACT
            let loopTask =
                PeriodicTimer.startImmediate (TimeSpan.FromMilliseconds 20.) action cts.Token ignore

            let! ticked = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> count >= 2)
            isTrue ticked
            cts.Cancel()

            let! completed =
                Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> loopTask.IsCompleted)

            // ASSERT
            isTrue completed
            equals loopTask.Status TaskStatus.RanToCompletion
        }

[<TestFixture>]
type PeriodicTimerRunTests() =

    [<Test>]
    member _.``honours the pause a step answers with``() =
        task {
            // ARRANGE
            use cts = new CancellationTokenSource()
            let sw = Diagnostics.Stopwatch.StartNew()
            let times = ResizeArray<int64>()
            let pause = TimeSpan.FromMilliseconds 150.

            let step () =
                task {
                    times.Add(sw.ElapsedMilliseconds)

                    if times.Count >= 3 then
                        cts.Cancel()

                    return pause
                }

            // ACT
            let _ = PeriodicTimer.run step cts.Token (fun _ -> TimeSpan.Zero)
            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> times.Count >= 3)

            // ASSERT
            isTrue reached
            isTrue (times[1] - times[0] >= 100L) // clear of a busy-loop reading as "honoured"
            isTrue (times[2] - times[1] >= 100L)
        }

    [<Test>]
    member _.``uses onError's pause after a throw, and keeps running``() =
        task {
            // ARRANGE
            use cts = new CancellationTokenSource()
            let sw = Diagnostics.Stopwatch.StartNew()
            let times = ResizeArray<int64>()
            let mutable count = 0

            let step () =
                task {
                    count <- count + 1
                    times.Add(sw.ElapsedMilliseconds)

                    if count = 1 then
                        failwith "boom"

                    if count >= 3 then
                        cts.Cancel()

                    return TimeSpan.FromMilliseconds 10.
                }

            // ACT
            let _ = PeriodicTimer.run step cts.Token (fun _ -> TimeSpan.FromMilliseconds 200.)
            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> count >= 3)

            // ASSERT
            isTrue reached
            equals count 3
            // The gap after the throw reflects onError's 200ms answer, not the step's normal 10ms.
            isTrue (times[1] - times[0] >= 150L)
        }

    [<Test>]
    member _.``cancelling the token stops the loop and completes the returned task``() =
        task {
            // ARRANGE
            let mutable count = 0
            use cts = new CancellationTokenSource()

            let step () =
                task {
                    count <- count + 1
                    return TimeSpan.FromSeconds 5.
                }

            // ACT
            let loopTask = PeriodicTimer.run step cts.Token (fun _ -> TimeSpan.Zero)
            let! ticked = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> count >= 1)
            isTrue ticked
            cts.Cancel()

            let! completed =
                Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> loopTask.IsCompleted)

            // ASSERT
            isTrue completed
            equals loopTask.Status TaskStatus.RanToCompletion
        }
