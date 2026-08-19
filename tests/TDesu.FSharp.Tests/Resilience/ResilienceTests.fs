namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.Resilience

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

[<TestFixture>]
type CircuitBreakerTests() =

    [<Test>]
    member _.``closed circuit passes calls through``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 3
                Cooldown = TimeSpan.FromSeconds 10.
            }

        let result = breaker (fun () -> task { return 42 }) |> Task.getResult
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
                %(breaker (fun () -> task { return failwith "fail" }) |> Task.getResult)
            with _ ->
                ()
        // Third call should be rejected (circuit open)
        %Assert.ThrowsAsync<InvalidOperationException>(fun () -> breaker (fun () -> task { return 42 }) :> Task)

    [<Test>]
    member _.``circuit resets on success``() =
        let breaker =
            CircuitBreaker.create {
                Threshold = 3
                Cooldown = TimeSpan.FromSeconds 10.
            }
        // One failure
        try
            %(breaker (fun () -> task { return failwith "fail" }) |> Task.getResult)
        with _ ->
            ()
        // Success resets counter
        let result = breaker (fun () -> task { return 42 }) |> Task.getResult
        equals result 42

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
