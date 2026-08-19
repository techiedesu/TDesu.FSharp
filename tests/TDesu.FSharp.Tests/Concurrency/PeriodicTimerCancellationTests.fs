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
