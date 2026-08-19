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
