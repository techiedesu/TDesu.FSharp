namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks

[<TestFixture>]
type ParallelThrottleSafetyTests() =

    [<Test>]
    member _.``a throwing mapping function surfaces without leaving unobserved exceptions``() =
        // ARRANGE
        let mutable unobserved = 0

        let handler =
            EventHandler<UnobservedTaskExceptionEventArgs>(fun _ _ -> Interlocked.Increment(&unobserved) |> ignore)

        TaskScheduler.UnobservedTaskException.AddHandler handler
        let items = [ 1; 2; 3; 4 ]

        let f i =
            task { return if i = 3 then failwith "boom" else i }

        // ACT
        let act () =
            Task.parallelThrottle 2 items f |> Task.getResult |> ignore

        %Assert.Throws<Exception>(fun () -> act ())
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        TaskScheduler.UnobservedTaskException.RemoveHandler handler

        // ASSERT
        equals unobserved 0

    [<Test>]
    member _.``an empty input produces an empty result``() =
        // ARRANGE
        let items: int list = []

        // ACT
        let result =
            Task.parallelThrottle 4 items (fun i -> task { return i }) |> Task.getResult

        // ASSERT
        equals result.Length 0

    [<Test>]
    member _.``a null input sequence is treated as empty``() =
        // ARRANGE
        let items: int seq = null

        // ACT
        let result =
            Task.parallelThrottle 4 items (fun i -> task { return i }) |> Task.getResult

        // ASSERT
        equals result.Length 0

    [<Test>]
    member _.``maxConcurrent of one still processes every item in order``() =
        // ARRANGE
        let items = [ 1; 2; 3 ]

        // ACT
        let result =
            Task.parallelThrottle 1 items (fun i -> task { return i * 10 })
            |> Task.getResult

        // ASSERT
        equals (List.ofArray result) [ 10; 20; 30 ]

/// A stream that reports a fixed length of readable bytes and counts disposals.
