namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Concurrency

/// A clock the test drives by hand, so window boundaries are exact instead of timing-dependent.
type private ManualClock(start: DateTimeOffset) =
    let mutable now = start
    member _.Advance(by: TimeSpan) = now <- now + by

    interface IClock with
        member _.UtcNow = now

[<TestFixture>]
type RateLimitingBoundaryTests() =

    [<Test>]
    member _.``the window expires exactly at its length, not one tick later``() =
        // ARRANGE
        let clock = ManualClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let window = TimeSpan.FromSeconds 1.0
        let limiter = SlidingWindowLimiter(1, window, clock)
        isTrue (limiter.TryAcquire() |> Result.isOk)
        isTrue (limiter.TryAcquire() |> Result.isError)

        // ACT
        clock.Advance window

        // ASSERT
        isTrue (limiter.TryAcquire() |> Result.isOk)

    [<Test>]
    member _.``one tick before the boundary the limit still holds``() =
        // ARRANGE
        let clock = ManualClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let window = TimeSpan.FromSeconds 1.0
        let limiter = SlidingWindowLimiter(1, window, clock)
        isTrue (limiter.TryAcquire() |> Result.isOk)

        // ACT
        clock.Advance(window - TimeSpan(1L))

        // ASSERT
        isTrue (limiter.TryAcquire() |> Result.isError)

    [<Test>]
    member _.``a refused request never reports a zero wait``() =
        // ARRANGE
        let clock = ManualClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let window = TimeSpan.FromSeconds 1.0
        let limiter = SlidingWindowLimiter(1, window, clock)
        isTrue (limiter.TryAcquire() |> Result.isOk)

        // ACT
        clock.Advance(window - TimeSpan(1L))
        let result = limiter.TryAcquire()

        // ASSERT
        match result with
        | Error wait -> isTrue (wait > TimeSpan.Zero)
        | Ok () -> Assert.Fail "expected the limiter to refuse"
