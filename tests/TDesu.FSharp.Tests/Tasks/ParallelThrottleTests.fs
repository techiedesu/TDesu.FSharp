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
            Task.parallelThrottle 2 items CancellationToken.None f
            |> Task.getResult
            |> ignore

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
            Task.parallelThrottle 4 items CancellationToken.None (fun i -> task { return i })
            |> Task.getResult

        // ASSERT
        equals result.Length 0

    [<Test>]
    member _.``a null input sequence is treated as empty``() =
        // ARRANGE
        let items: int seq = null

        // ACT
        let result =
            Task.parallelThrottle 4 items CancellationToken.None (fun i -> task { return i })
            |> Task.getResult

        // ASSERT
        equals result.Length 0

    [<Test>]
    member _.``maxConcurrent of one still processes every item in order``() =
        // ARRANGE
        let items = [ 1; 2; 3 ]

        // ACT
        let result =
            Task.parallelThrottle 1 items CancellationToken.None (fun i -> task { return i * 10 })
            |> Task.getResult

        // ASSERT
        equals (List.ofArray result) [ 10; 20; 30 ]

    [<Test>]
    member _.``maxConcurrent of zero throws synchronously instead of hanging forever``() =
        let act () =
            Task.parallelThrottle 0 [ 1 ] CancellationToken.None (fun i -> task { return i })
            |> ignore

        let ex = Assert.Throws<ArgumentException>(act)
        equals ex.ParamName "maxConcurrent"

    [<Test>]
    member _.``negative maxConcurrent throws synchronously``() =
        let act () =
            Task.parallelThrottle -1 [ 1 ] CancellationToken.None (fun i -> task { return i })
            |> ignore

        %Assert.Throws<ArgumentException>(act)

    [<Test>]
    member _.``parallelThrottleUnit also rejects a non-positive maxConcurrent synchronously``() =
        let act () =
            Task.parallelThrottleUnit 0 [ 1 ] CancellationToken.None (fun _ -> task { return () })
            |> ignore

        %Assert.Throws<ArgumentException>(act)

    [<Test>]
    member _.``an already-cancelled token stops the batch before any item starts``() =
        task {
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let mutable started = 0

            let f i =
                task {
                    Interlocked.Increment(&started) |> ignore
                    return i
                }

            let t = Task.parallelThrottle 2 [ 1; 2; 3 ] cts.Token f

            try
                do! t :> Task
            with :? OperationCanceledException ->
                ()

            equals t.Status TaskStatus.Canceled
            equals started 0
        }

    [<Test>]
    member _.``cancelling the token mid-batch stops queueing further items``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()

            let f i =
                task {
                    if i = 2 then
                        cts.Cancel()

                    processed.Add i
                    return i
                }

            let t = Task.parallelThrottle 1 [ 1; 2; 3; 4; 5 ] cts.Token f

            try
                do! t :> Task
            with :? OperationCanceledException ->
                ()

            equals t.Status TaskStatus.Canceled
            isTrue (processed.Count < 5)
        }

    [<Test>]
    member _.``the surfaced exception keeps the stack trace of where it was originally thrown``() =
        let failingItem (i: int) : Task<int> =
            task {
                do! Task.Yield()

                if i = 1 then
                    failwith "boom from failingItem"

                return i
            }

        let act () =
            Task.parallelThrottle 2 [ 1 ] CancellationToken.None failingItem
            |> Task.getResult
            |> ignore

        let ex = Assert.Throws<Exception>(act)
        isTrue (ex.StackTrace.Contains "failingItem")

/// A stream that reports a fixed length of readable bytes and counts disposals.
