namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Concurrency

[<TestFixture>]
type ChannelWorkerTests() =

    [<Test>]
    member _.``processes posted items sequentially``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()

            let worker =
                ChannelWorker.start (fun x -> task { processed.Add(x) }) ignore cts.Token

            worker.Post(1)
            worker.Post(2)
            worker.Post(3)

            do! Task.Delay(200)
            cts.Cancel()

            try
                do! worker.Completion
            with :? OperationCanceledException ->
                ()

            let items = processed.ToArray() |> Array.sort
            equals items [| 1; 2; 3 |]
        }

    [<Test>]
    member _.``continues after handler error``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()
            let errors = Collections.Concurrent.ConcurrentBag<exn>()

            let worker =
                ChannelWorker.start
                    (fun x ->
                        task {
                            if x = 2 then
                                failwith "boom"

                            processed.Add(x)
                        }
                    )
                    (fun ex -> errors.Add(ex))
                    cts.Token

            worker.Post(1)
            worker.Post(2)
            worker.Post(3)

            do! Task.Delay(200)
            cts.Cancel()

            try
                do! worker.Completion
            with :? OperationCanceledException ->
                ()

            let items = processed.ToArray() |> Array.sort
            equals items [| 1; 3 |]
            equals errors.Count 1
        }

    [<Test>]
    member _.``cancellation stops the worker``() =
        task {
            use cts = new CancellationTokenSource()

            let worker =
                ChannelWorker.start (fun (_: int) -> Task.CompletedTask) ignore cts.Token

            cts.Cancel()
            do! Task.Delay(100)

            isTrue worker.Completion.IsCompleted
        }

    [<Test>]
    member _.``post after cancellation does not throw``() =
        task {
            use cts = new CancellationTokenSource()

            let worker =
                ChannelWorker.start (fun (_: int) -> Task.CompletedTask) ignore cts.Token

            cts.Cancel()
            do! Task.Delay(100)

            // Post after worker stopped — should not throw
            worker.Post(42)
            worker.Post(99)
        }

    [<Test>]
    member _.``PendingCount reflects queued items``() =
        task {
            use cts = new CancellationTokenSource()
            let gate = new SemaphoreSlim(0)

            let worker =
                ChannelWorker.start (fun (_: int) -> task { do! gate.WaitAsync() }) ignore cts.Token

            worker.Post(1)
            worker.Post(2)
            worker.Post(3)

            do! Task.Delay(100)

            // At least 1 item should be pending (2 or 3 depending on timing)
            isTrue (worker.PendingCount >= 1)

            gate.Release(3) |> ignore
            do! Task.Delay(200)
            equals worker.PendingCount 0

            cts.Cancel()
            gate.Dispose()
        }

    [<Test>]
    member _.``onError swallowed if it throws``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()

            let worker =
                ChannelWorker.start
                    (fun x ->
                        task {
                            if x = 1 then
                                failwith "handler boom"

                            processed.Add(x)
                        }
                    )
                    (fun _ -> failwith "onError boom")
                    cts.Token

            worker.Post(1)
            worker.Post(2)

            do! Task.Delay(200)
            cts.Cancel()

            try
                do! worker.Completion
            with :? OperationCanceledException ->
                ()

            // Item 2 should still be processed despite onError throwing
            isTrue (processed.ToArray() |> Array.contains 2)
        }

[<TestFixture>]
type ChannelWorkerBoundedTests() =

    [<Test>]
    member _.``processes posted items sequentially, in order``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = ResizeArray<int>()

            let worker =
                ChannelWorker.startBounded 10 (fun x -> task { processed.Add(x) }) ignore cts.Token

            worker.TryPost(1) |> ignore
            worker.TryPost(2) |> ignore
            worker.TryPost(3) |> ignore

            do! Task.Delay(200)
            cts.Cancel()

            equals (processed |> List.ofSeq) [ 1; 2; 3 ]
        }

    [<Test>]
    member _.``TryPost returns false at capacity while the handler is blocked``() =
        task {
            use cts = new CancellationTokenSource()
            let gate = new SemaphoreSlim(0)

            let worker =
                ChannelWorker.startBounded 1 (fun (_: int) -> task { do! gate.WaitAsync() }) ignore cts.Token

            // Dequeued immediately, leaving the buffer empty but the handler blocked on the gate.
            isTrue (worker.TryPost(1))
            do! Task.Delay(100)
            equals worker.PendingCount 0

            isTrue (worker.TryPost(2)) // fills the buffer (capacity 1)
            isFalse (worker.TryPost(3)) // buffer full, handler still blocked

            gate.Release(2) |> ignore
            do! Task.Delay(100)
            cts.Cancel()
            gate.Dispose()
        }

    [<Test>]
    member _.``PostAsync waits for room, then delivers``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()
            let gate = new SemaphoreSlim(0)

            let worker =
                ChannelWorker.startBounded
                    1
                    (fun x ->
                        task {
                            do! gate.WaitAsync()
                            processed.Add(x)
                        }
                    )
                    ignore
                    cts.Token

            worker.TryPost(1) |> ignore // dequeued immediately, blocks the handler on the gate
            do! Task.Delay(100)
            worker.TryPost(2) |> ignore // fills the buffer (capacity 1)

            let postTask = worker.PostAsync(3, CancellationToken.None)
            do! Task.Delay(100)
            isFalse postTask.IsCompleted // still waiting for room

            gate.Release(3) |> ignore
            do! postTask // completes once item 2 is dequeued and room opens up
            do! Task.Delay(100)
            cts.Cancel()

            equals (processed.ToArray() |> Array.sort) [| 1; 2; 3 |]
            gate.Dispose()
        }

    [<Test>]
    member _.``a throwing handler reports to onError and the worker continues``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()
            let errors = Collections.Concurrent.ConcurrentBag<exn>()

            let worker =
                ChannelWorker.startBounded
                    10
                    (fun x ->
                        task {
                            if x = 2 then
                                failwith "boom"

                            processed.Add(x)
                        }
                    )
                    (fun ex -> errors.Add(ex))
                    cts.Token

            worker.TryPost(1) |> ignore
            worker.TryPost(2) |> ignore
            worker.TryPost(3) |> ignore

            do! Task.Delay(200)
            cts.Cancel()

            equals (processed.ToArray() |> Array.sort) [| 1; 3 |]
            equals errors.Count 1
        }

    [<Test>]
    member _.``cancellation stops the worker and completes its Completion task``() =
        task {
            use cts = new CancellationTokenSource()

            let worker =
                ChannelWorker.startBounded 10 (fun (_: int) -> Task.CompletedTask) ignore cts.Token

            cts.Cancel()
            do! Task.Delay(100)

            isTrue worker.Completion.IsCompleted
            equals worker.Completion.Status TaskStatus.RanToCompletion
            isFalse (worker.TryPost(1))
        }

    [<Test>]
    member _.``Complete lets queued items drain, then Completion finishes only once all are handled``() =
        task {
            use cts = new CancellationTokenSource()
            let processed = Collections.Concurrent.ConcurrentBag<int>()

            let handler (x: int) : Task =
                task {
                    do! Task.Delay(100)
                    processed.Add(x)
                }

            let worker = ChannelWorker.startBounded 10 handler ignore cts.Token

            worker.TryPost(1) |> ignore
            worker.TryPost(2) |> ignore
            worker.TryPost(3) |> ignore

            worker.Complete()

            isFalse (worker.TryPost(4)) // refuses new work immediately
            isFalse worker.Completion.IsCompleted // three 100ms handlers have not all run yet

            do! worker.Completion

            equals (processed.ToArray() |> Array.sort) [| 1; 2; 3 |]
            cts.Cancel()
        }
