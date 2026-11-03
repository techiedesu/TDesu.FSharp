namespace TDesu.FSharp.Tests

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.Resilience

[<AutoOpen>]
module private ResilienceTestHelpers =
    /// Marked to resist JIT inlining, so it always keeps its own stack frame — a test asserting a
    /// caught exception's stack trace still names its true throw site would otherwise be at the
    /// mercy of whether the JIT happened to inline this one-line function away.
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    let throwingSite () : int = failwith "boom"

[<TestFixture>]
type RetryTests() =

    [<Test>]
    member _.``withBackoff succeeds on first try``() =
        let result =
            Retry.withBackoff 3 (TimeSpan.FromMilliseconds 10.) CancellationToken.None (fun () -> task { return 42 })
            |> Task.getResult

        equals result 42

    [<Test>]
    member _.``withBackoff retries on failure then succeeds``() =
        let mutable attempt = 0

        let result =
            Retry.withBackoff
                3
                (TimeSpan.FromMilliseconds 10.)
                CancellationToken.None
                (fun () ->
                    task {
                        attempt <- attempt + 1

                        if attempt < 3 then
                            failwith "not yet"

                        return 42
                    }
                )
            |> Task.getResult

        equals result 42
        equals attempt 3

    [<Test>]
    member _.``withBackoff throws after max retries``() =
        %Assert.ThrowsAsync<Exception>(fun () ->
            Retry.withBackoff
                2
                (TimeSpan.FromMilliseconds 1.)
                CancellationToken.None
                (fun () -> task { return failwith "always fails" })
            :> Task
        )

    [<Test>]
    member _.``withBackoff rejects a negative baseDelay``() =
        %Assert.Throws<ArgumentException>(fun () ->
            Retry.withBackoff 2 (TimeSpan.FromMilliseconds -1.) CancellationToken.None (fun () -> task { return 1 })
            |> ignore
        )

    [<Test>]
    member _.``withBackoff preserves the original exception's stack trace after exhausting retries``() =

        let ex =
            Assert.ThrowsAsync<Exception>(fun () ->
                Retry.withBackoff
                    0
                    (TimeSpan.FromMilliseconds 1.)
                    CancellationToken.None
                    (fun () -> task { return throwingSite () })
                :> Task
            )

        isTrue (ex.StackTrace.Contains "throwingSite")

    [<Test>]
    member _.``backoffDelay caps the exponential growth instead of reaching days-long waits``() =
        let uncappedMs = (TimeSpan.FromMilliseconds 10.).TotalMilliseconds * pown 2.0 40
        // Sanity: the raw, uncapped growth really would be enormous (far beyond the cap).
        isTrue (uncappedMs > Retry.maxBackoffDelay.TotalMilliseconds * 1000.)

        let capped = Retry.backoffDelay (TimeSpan.FromMilliseconds 10.) 40
        equals capped Retry.maxBackoffDelay

    [<Test>]
    member _.``tryWithBackoff returns Error after max retries``() =
        let result =
            Retry.tryWithBackoff
                1
                (TimeSpan.FromMilliseconds 1.)
                CancellationToken.None
                (fun () -> task { return failwith "fail" })
            |> Task.getResult

        match result with
        | Error _ -> ()
        | Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``tryWithBackoff propagates the caller's own cancellation instead of returning Error``() =
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let retryTask =
            Retry.tryWithBackoff 3 (TimeSpan.FromMilliseconds 10.) cts.Token (fun () -> task { return 1 })

        %Assert.ThrowsAsync<OperationCanceledException>(fun () -> retryTask :> Task)
        isTrue retryTask.IsCanceled

    [<Test>]
    member _.``withBackoff does not retry a cancellation from a foreign token, and does not report it as its own``() =
        task {
            let mutable attempts = 0
            use foreignCts = new CancellationTokenSource()
            foreignCts.Cancel()

            let retryTask =
                Retry.withBackoff
                    3
                    (TimeSpan.FromMilliseconds 5.)
                    CancellationToken.None
                    (fun () ->
                        task {
                            attempts <- attempts + 1
                            foreignCts.Token.ThrowIfCancellationRequested()
                            return 1
                        }
                    )

            try
                let! _ = retryTask
                Assert.Fail("expected an exception")
            with _ ->
                ()

            equals attempts 1
            isFalse retryTask.IsCanceled
            isTrue retryTask.IsFaulted
        }

    [<Test>]
    member _.``withDelay retries with fixed delay``() =
        let mutable attempt = 0

        let result =
            Retry.withDelay
                2
                (TimeSpan.FromMilliseconds 5.)
                CancellationToken.None
                (fun () ->
                    task {
                        attempt <- attempt + 1

                        if attempt < 2 then
                            failwith "not yet"

                        return "ok"
                    }
                )
            |> Task.getResult

        equals result "ok"

    [<Test>]
    member _.``withDelay rejects a negative delay``() =
        %Assert.Throws<ArgumentException>(fun () ->
            Retry.withDelay 2 (TimeSpan.FromMilliseconds -1.) CancellationToken.None (fun () -> task { return 1 })
            |> ignore
        )

[<TestFixture>]
type TimeoutTests() =

    [<Test>]
    member _.``after completes within deadline``() =
        let result =
            Timeout.after (TimeSpan.FromSeconds 5.) (fun _ -> task { return 42 })
            |> Task.getResult

        equals result 42

    [<Test>]
    member _.``after throws TimeoutException on expiry``() =
        %Assert.ThrowsAsync<TimeoutException>(fun () ->
            Timeout.after
                (TimeSpan.FromMilliseconds 10.)
                (fun ct ->
                    task {
                        do! Task.Delay(5000, ct)
                        return 42
                    }
                )
            :> Task
        )

    [<Test>]
    member _.``after does not forcibly abort work that ignores the token``() =
        task {
            let! result =
                Timeout.after
                    (TimeSpan.FromMilliseconds 20.)
                    (fun _ ->
                        task {
                            do! Task.Delay 100
                            return 42
                        }
                    )

            equals result 42
        }

    [<Test>]
    member _.``after does not relabel an unrelated cancellation as a timeout``() =
        use unrelatedCts = new CancellationTokenSource()

        %Assert.ThrowsAsync<OperationCanceledException>(fun () ->
            Timeout.after
                (TimeSpan.FromMilliseconds 20.)
                (fun _ ->
                    task {
                        do! Task.Delay 60
                        unrelatedCts.Cancel()
                        unrelatedCts.Token.ThrowIfCancellationRequested()
                        return 0
                    }
                )
            :> Task
        )

    [<Test>]
    member _.``afterLinked respects parent cancellation``() =
        use parentCts = new CancellationTokenSource()
        parentCts.Cancel()

        %Assert.ThrowsAsync<OperationCanceledException>(fun () ->
            Timeout.afterLinked
                (TimeSpan.FromSeconds 10.)
                parentCts.Token
                (fun ct ->
                    task {
                        ct.ThrowIfCancellationRequested()
                        return 42
                    }
                )
            :> Task
        )

    [<Test>]
    member _.``afterLinked still reports TimeoutException when the parent is cancelled after the deadline fires``() =
        use parentCts = new CancellationTokenSource()

        let work (ct: CancellationToken) =
            task {
                try
                    do! Task.Delay(5000, ct)
                    return 0
                with :? OperationCanceledException as oce ->
                    // Simulates a caller reacting to the deadline by cancelling the parent too,
                    // after the deadline — not the parent — is what actually stopped the work.
                    parentCts.Cancel()
                    return raise oce
            }

        %Assert.ThrowsAsync<TimeoutException>(fun () ->
            Timeout.afterLinked (TimeSpan.FromMilliseconds 20.) parentCts.Token work :> Task
        )

    [<Test>]
    member _.``afterLinked does not relabel an unrelated cancellation as a timeout``() =
        use parentCts = new CancellationTokenSource()
        use unrelatedCts = new CancellationTokenSource()

        %Assert.ThrowsAsync<OperationCanceledException>(fun () ->
            Timeout.afterLinked
                (TimeSpan.FromMilliseconds 20.)
                parentCts.Token
                (fun _ ->
                    task {
                        do! Task.Delay 60
                        unrelatedCts.Cancel()
                        unrelatedCts.Token.ThrowIfCancellationRequested()
                        return 0
                    }
                )
            :> Task
        )

[<TestFixture>]
type CircuitBreakerTests() =

    [<Test>]
    member _.``closed circuit passes calls through``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 3
                Cooldown = TimeSpan.FromSeconds 10.
            }

        let result = breaker.Run(fun () -> task { return 42 }) |> Task.getResult
        equals result 42

    [<Test>]
    member _.``circuit opens after threshold failures``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 2
                Cooldown = TimeSpan.FromSeconds 10.
            }
        // Fail twice
        for _ in 1..2 do
            try
                %(breaker.Run(fun () -> task { return failwith "fail" }) |> Task.getResult)
            with _ ->
                ()
        // Third call should be rejected (circuit open)
        %Assert.ThrowsAsync<InvalidOperationException>(fun () -> breaker.Run(fun () -> task { return 42 }) :> Task)

    [<Test>]
    member _.``circuit resets on success``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 3
                Cooldown = TimeSpan.FromSeconds 10.
            }
        // One failure
        try
            %(breaker.Run(fun () -> task { return failwith "fail" }) |> Task.getResult)
        with _ ->
            ()
        // Success resets counter
        let result = breaker.Run(fun () -> task { return 42 }) |> Task.getResult
        equals result 42

    [<Test>]
    member _.``an in-flight call that started before the trip does not re-close the circuit once it opens``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 2
                Cooldown = TimeSpan.FromSeconds 30.
            }

        // One failure: Closed 0 -> Closed 1 (not yet open).
        try
            %(breaker.Run(fun () -> task { return failwith "fail" }: Task<int>)
              |> Task.getResult)
        with _ ->
            ()

        // A starts while the circuit is Closed 1 and is kept running.
        let gate = TaskCompletionSource<int>()
        let callA = breaker.Run(fun () -> gate.Task)

        // A second, unrelated failure trips the breaker to Open while A is still in flight.
        try
            %(breaker.Run(fun () -> task { return failwith "fail" }: Task<int>)
              |> Task.getResult)
        with _ ->
            ()

        // A finally succeeds, after the breaker is already Open.
        gate.SetResult 7
        %(Task.getResult callA)

        %Assert.ThrowsAsync<InvalidOperationException>(fun () -> breaker.Run(fun () -> task { return 1 }) :> Task)

    [<Test>]
    member _.``HalfOpen admits exactly one probe; concurrent callers are rejected``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 1
                Cooldown = TimeSpan.FromMilliseconds 20.
            }

        try
            %(breaker.Run(fun () -> task { return failwith "fail" }: Task<int>)
              |> Task.getResult)
        with _ ->
            ()

        Thread.Sleep 50 // past the cooldown; the circuit is now eligible for a probe

        let mutable invocations = 0
        let release = TaskCompletionSource<int>()

        let probe () =
            Interlocked.Increment(&invocations) |> ignore
            release.Task

        let calls = [ for _ in 1..10 -> breaker.Run(fun () -> probe ()) ]

        Thread.Sleep 50 // let every rejected caller resolve; the admitted one still waits on release
        release.SetResult 1

        let outcomes =
            calls
            |> List.map (fun t ->
                try
                    Task.getResult t |> ignore
                    true
                with _ ->
                    false
            )

        equals invocations 1
        equals (outcomes |> List.filter id |> List.length) 1

    [<Test>]
    member _.``one breaker instance guards calls of different result types``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 3
                Cooldown = TimeSpan.FromSeconds 10.
            }

        let intResult = breaker.Run(fun () -> task { return 42 }) |> Task.getResult

        let stringResult =
            breaker.Run(fun () -> task { return "forty two" }) |> Task.getResult

        equals intResult 42
        equals stringResult "forty two"

    [<Test>]
    member _.``a rejection through the breaker preserves the original exception's stack trace``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 100
                Cooldown = TimeSpan.FromSeconds 10.
            }

        let ex =
            Assert.ThrowsAsync<Exception>(fun () -> breaker.Run(fun () -> task { return throwingSite () }) :> Task)

        isTrue (ex.StackTrace.Contains "throwingSite")

[<TestFixture>]
type MemoizeTests() =

    [<Test>]
    member _.``create caches result``() =
        let mutable callCount = 0

        let cached =
            Memoize.create (fun (k: int) ->
                callCount <- callCount + 1
                k * 2
            )

        equals (cached 5) 10
        equals (cached 5) 10
        equals callCount 1

    [<Test>]
    member _.``create does not allocate on a cache hit``() =
        let cached = Memoize.create (fun (k: int) -> k + 123456)
        cached 999999 |> ignore // populate the cache and let the hit path JIT

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let before = GC.GetAllocatedBytesForCurrentThread()

        for _ in 1..10_000 do
            cached 999999 |> ignore

        let after = GC.GetAllocatedBytesForCurrentThread()
        let bytesPerHit = float (after - before) / 10_000.

        isTrue (bytesPerHit < 16.)

    [<Test>]
    member _.``createAsync caches async result``() =
        let mutable callCount = 0

        let cached =
            Memoize.createAsync (fun (k: int) ->
                task {
                    callCount <- callCount + 1
                    return k * 2
                }
            )

        equals (cached 5 |> Task.getResult) 10
        equals (cached 5 |> Task.getResult) 10
        equals callCount 1

    [<Test>]
    member _.``createAsync does not allocate a new Task on a cache hit``() =
        let cached = Memoize.createAsync (fun (k: int) -> task { return k + 123456 })
        cached 999999 |> Task.getResult |> ignore // populate the cache and let the hit path JIT

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let before = GC.GetAllocatedBytesForCurrentThread()

        for _ in 1..10_000 do
            cached 999999 |> ignore

        let after = GC.GetAllocatedBytesForCurrentThread()
        let bytesPerHit = float (after - before) / 10_000.

        isTrue (bytesPerHit < 16.)

    [<Test>]
    member _.``createAsync runs f once for concurrent misses on one key``() =
        task {
            let mutable callCount = 0
            let release = TaskCompletionSource<int>()

            let cached =
                Memoize.createAsync (fun (_: int) ->
                    Interlocked.Increment(&callCount) |> ignore
                    release.Task
                )

            let callers = [ for _ in 1..3 -> cached 5 ]
            release.SetResult 10
            let! values = Task.WhenAll callers

            equals values [| 10; 10; 10 |]
            equals callCount 1
        }

    [<Test>]
    member _.``createAsync does not cache a failure``() =
        task {
            let mutable callCount = 0

            let cached =
                Memoize.createAsync (fun (k: int) ->
                    task {
                        callCount <- callCount + 1

                        if callCount = 1 then
                            failwith "transient"

                        return k * 2
                    }
                )

            let! first = Task.catch (cached 5)
            let! second = cached 5

            equals (Result.isError first) true
            equals second 10
            equals callCount 2
        }

    [<Test>]
    member _.``withTtlAsync runs f once for concurrent misses on one key``() =
        task {
            let mutable callCount = 0
            let release = TaskCompletionSource<int>()

            let cached =
                Memoize.withTtlAsync
                    (TimeSpan.FromMinutes 1.)
                    (fun (_: int) ->
                        Interlocked.Increment(&callCount) |> ignore
                        release.Task
                    )

            let callers = [ for _ in 1..3 -> cached 5 ]
            release.SetResult 10
            let! values = Task.WhenAll callers

            equals values [| 10; 10; 10 |]
            equals callCount 1
        }

    [<Test>]
    member _.``withTtl expires after TTL``() =
        task {
            let mutable callCount = 0

            let cached =
                Memoize.withTtl
                    (TimeSpan.FromMilliseconds 50.)
                    (fun (k: int) ->
                        callCount <- callCount + 1
                        k * 2
                    )

            equals (cached 5) 10
            equals (cached 5) 10
            equals callCount 1
            do! Task.Delay(100)
            equals (cached 5) 10
            equals callCount 2 // recomputed after TTL
        }

    [<Test>]
    member _.``withTtl does not cache a factory slower than the TTL``() =
        let mutable callCount = 0

        let cached =
            Memoize.withTtl
                (TimeSpan.FromMilliseconds 50.)
                (fun (k: int) ->
                    callCount <- callCount + 1
                    Thread.Sleep 150
                    k * 2
                )

        for _ in 1..3 do
            equals (cached 5) 10

        equals callCount 1

    [<Test>]
    member _.``different keys cached separately``() =
        let mutable callCount = 0

        let cached =
            Memoize.create (fun (k: int) ->
                callCount <- callCount + 1
                k * 2
            )

        equals (cached 1) 2
        equals (cached 2) 4
        equals callCount 2

[<TestFixture>]
type SagaTests() =

    [<Test>]
    member _.``run succeeds with all steps``() =
        let result =
            Saga.run
                [
                    Saga.step "step1" (fun ctx -> task { return ctx + 10 }) (fun _ -> task { return () })
                    Saga.step "step2" (fun ctx -> task { return ctx * 2 }) (fun _ -> task { return () })
                ]
                5
            |> Task.getResult

        match result with
        | Ok v -> equals v 30
        | Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``run compensates on failure``() =
        let mutable compensated = false

        let result =
            Saga.run
                [
                    Saga.step "step1" (fun ctx -> task { return ctx + 10 }) (fun _ -> task { compensated <- true })
                    Saga.step "step2" (fun _ -> task { return failwith "boom" }) (fun _ -> task { return () })
                ]
                5
            |> Task.getResult

        match result with
        | Error _ -> ()
        | Ok _ -> Assert.Fail("Expected Error")

        isTrue compensated

    [<Test>]
    member _.``stepNoCompensate creates step without compensation``() =
        let result =
            Saga.run [ Saga.stepNoCompensate "only" (fun ctx -> task { return ctx + 1 }) ] 0
            |> Task.getResult

        match result with
        | Ok v -> equals v 1
        | Error _ -> Assert.Fail("Expected Ok")
