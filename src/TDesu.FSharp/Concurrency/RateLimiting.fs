namespace TDesu.FSharp.Concurrency

open System
open TDesu.FSharp

/// <summary>
/// Sliding-window rate limiter. Tracks request count within a time window.
/// Thread-safe on .NET; single-threaded safe on Fable.
/// </summary>
/// <example>
/// <code>
/// let limiter = SlidingWindowLimiter(100, TimeSpan.FromMinutes 1.)
/// match limiter.TryAcquire() with
/// | Ok () -> processRequest()
/// | Error waitTime -> rejectWith429 waitTime
/// </code>
/// </example>
/// <param name="maxRequests">Maximum number of requests allowed per window. Must be positive.</param>
/// <param name="window">Duration of the sliding window. Must be positive.</param>
/// <param name="clock">Clock implementation for time. Defaults to SystemClock.</param>
[<Sealed>]
type SlidingWindowLimiter(maxRequests: int, window: TimeSpan, clock: IClock) =
    do
        if maxRequests <= 0 then
            invalidArg (nameof maxRequests) $"Must be positive, got %d{maxRequests}"
        if window.Ticks <= 0L then
            invalidArg (nameof window) $"Must be positive, got %A{window}"

    let windowTicks = window.Ticks

#if FABLE_COMPILER
    let mutable count = 0
    let mutable windowStart = 0L
#else
    let lockObj = obj()
    let mutable count = 0
    let mutable windowStart = 0L
#endif

    /// Creates a limiter using the system clock.
    new(maxRequests: int, window: TimeSpan) =
        SlidingWindowLimiter(maxRequests, window, SystemClock.Instance)

#if FABLE_COMPILER
    /// <summary>
    /// Try to acquire a permit.
    /// Returns <c>Ok()</c> if allowed, <c>Error(waitTime)</c> if rate limited.
    /// </summary>
    member _.TryAcquire() : Result<unit, TimeSpan> =
        let nowTicks = clock.UtcNow.Ticks
        if nowTicks - windowStart > windowTicks then
            count <- 1
            windowStart <- nowTicks
            Ok()
        elif count < maxRequests then
            count <- count + 1
            Ok()
        else
            Error(TimeSpan(windowTicks - (nowTicks - windowStart)))

    /// Reset the limiter to initial state.
    member _.Reset() =
        count <- 0
        windowStart <- 0L

    /// Current number of requests in the active window.
    member _.Count = count
#else
    /// <summary>
    /// Try to acquire a permit. Thread-safe.
    /// Returns <c>Ok()</c> if allowed, <c>Error(waitTime)</c> if rate limited.
    /// </summary>
    member _.TryAcquire() : Result<unit, TimeSpan> =
        lock lockObj (fun () ->
            let nowTicks = clock.UtcNow.Ticks
            if nowTicks - windowStart > windowTicks then
                count <- 1
                windowStart <- nowTicks
                Ok()
            elif count < maxRequests then
                count <- count + 1
                Ok()
            else
                Error(TimeSpan(windowTicks - (nowTicks - windowStart))))

    /// Reset the limiter to initial state. Thread-safe.
    member _.Reset() =
        lock lockObj (fun () ->
            count <- 0
            windowStart <- 0L)

    /// Current number of requests in the active window.
    member _.Count =
        lock lockObj (fun () -> count)
#endif

    /// Maximum requests allowed per window.
    member _.MaxRequests = maxRequests

    /// Window duration.
    member _.Window = window
