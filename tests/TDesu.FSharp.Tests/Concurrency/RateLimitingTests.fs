namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Concurrency

[<TestFixture>]
type SlidingWindowLimiterTests() =

    [<Test>]
    member _.``allows requests under limit``() =
        let limiter = SlidingWindowLimiter(3, TimeSpan.FromSeconds 60.)

        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())

        equals limiter.Count 3

    [<Test>]
    member _.``rejects requests over limit``() =
        let limiter = SlidingWindowLimiter(2, TimeSpan.FromSeconds 60.)

        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())

        let result = limiter.TryAcquire()
        isError result

        match result with
        | Error waitTime -> isTrue (waitTime.Ticks > 0L)
        | _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``window slides after expiry``() =
        let clock = FakeClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let limiter = SlidingWindowLimiter(1, TimeSpan.FromMilliseconds 50., clock)

        isOk () (limiter.TryAcquire())
        isError (limiter.TryAcquire())

        clock.Advance(TimeSpan.FromMilliseconds 50.)

        isOk () (limiter.TryAcquire())

    [<Test>]
    member _.``reset clears state``() =
        let limiter = SlidingWindowLimiter(2, TimeSpan.FromSeconds 60.)

        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())
        isError (limiter.TryAcquire())

        limiter.Reset()
        equals limiter.Count 0

        isOk () (limiter.TryAcquire())

    [<Test>]
    member _.``properties are accessible``() =
        let limiter = SlidingWindowLimiter(10, TimeSpan.FromMinutes 1.)
        equals limiter.MaxRequests 10
        equals limiter.Window (TimeSpan.FromMinutes 1.)

    [<Test>]
    member _.``rejects zero maxRequests``() =
        Assert.Throws<ArgumentException>(fun () -> SlidingWindowLimiter(0, TimeSpan.FromSeconds 1.) |> ignore)
        |> ignore

    [<Test>]
    member _.``rejects negative maxRequests``() =
        Assert.Throws<ArgumentException>(fun () -> SlidingWindowLimiter(-1, TimeSpan.FromSeconds 1.) |> ignore)
        |> ignore

    [<Test>]
    member _.``rejects zero window``() =
        Assert.Throws<ArgumentException>(fun () -> SlidingWindowLimiter(10, TimeSpan.Zero) |> ignore)
        |> ignore

    [<Test>]
    member _.``rejects negative window``() =
        Assert.Throws<ArgumentException>(fun () -> SlidingWindowLimiter(10, TimeSpan.FromSeconds -1.) |> ignore)
        |> ignore

    [<Test>]
    member _.``single request allowed with maxRequests 1``() =
        let limiter = SlidingWindowLimiter(1, TimeSpan.FromSeconds 60.)
        isOk () (limiter.TryAcquire())
        isError (limiter.TryAcquire())

    [<Test>]
    member _.``Count expires with the window instead of reporting a stale permit count forever``() =
        let clock = FakeClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let limiter = SlidingWindowLimiter(5, TimeSpan.FromSeconds 1., clock)

        for _ in 1..3 do
            limiter.TryAcquire() |> ignore

        equals limiter.Count 3

        clock.Advance(TimeSpan.FromSeconds 60.)

        equals limiter.Count 0

    [<Test>]
    member _.``only the expired permit frees up, not the whole window``() =
        // Pins the sliding-window fix: a fixed-window implementation resets wholesale once the
        // first permit's own window elapses, admitting up to maxRequests more at once instead of
        // freeing exactly the one permit that actually aged out.
        let clock = FakeClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let window = TimeSpan.FromSeconds 1.
        let limiter = SlidingWindowLimiter(5, window, clock)

        isOk () (limiter.TryAcquire()) // t=0

        clock.Advance(TimeSpan.FromMilliseconds 1.)

        for _ in 1..4 do
            isOk () (limiter.TryAcquire()) // t=1ms, four more permits

        clock.Advance(window - TimeSpan.FromMilliseconds 1.) // now at t=window

        isOk () (limiter.TryAcquire()) // only the t=0 permit has aged out; this fills that slot
        isError (limiter.TryAcquire()) // the four from t=1ms are still inside the window
