namespace TDesu.FSharp.Concurrency

open System
open TDesu.FSharp

/// <summary>
/// True sliding-window rate limiter. Every granted request's timestamp is kept in a ring buffer
/// and expires individually once it ages out of the window, rather than resetting a fixed window
/// wholesale — so no span of <paramref name="window"/> length ever sees more than
/// <paramref name="maxRequests"/> admitted requests, including one straddling what would be a
/// fixed window's boundary. Thread-safe.
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

    let lockObj = obj ()
    // Ring buffer of the ticks at which each currently-live permit was granted, oldest at `head`;
    // `count` of the `maxRequests` slots are in use.
    let timestamps = Array.zeroCreate<int64> maxRequests
    let mutable head = 0
    let mutable count = 0

    /// Drops every timestamp that has aged out of the window. Caller must hold lockObj.
    let evictExpired (nowTicks: int64) =
        while count > 0 && nowTicks - timestamps[head] >= windowTicks do
            head <- (head + 1) % maxRequests
            count <- count - 1

    /// Creates a limiter using the system clock.
    new(maxRequests: int, window: TimeSpan) = SlidingWindowLimiter(maxRequests, window, SystemClock.Instance)

    /// <summary>
    /// Try to acquire a permit. Thread-safe.
    /// Returns <c>Ok()</c> if allowed, <c>Error(waitTime)</c> if rate limited.
    /// </summary>
    member _.TryAcquire() : Result<unit, TimeSpan> =
        lock
            lockObj
            (fun () ->
                let nowTicks = clock.UtcNow.Ticks
                evictExpired nowTicks

                if count < maxRequests then
                    timestamps[(head + count) % maxRequests] <- nowTicks
                    count <- count + 1
                    Ok()
                else
                    Error(TimeSpan(windowTicks - (nowTicks - timestamps[head])))
            )

    /// Reset the limiter to initial state. Thread-safe.
    member _.Reset() =
        lock
            lockObj
            (fun () ->
                head <- 0
                count <- 0
            )

    /// Current number of requests in the active window. Expires with the window, like
    /// <see cref="M:TDesu.FSharp.Concurrency.SlidingWindowLimiter.TryAcquire"/> does, rather than
    /// reporting stale permits that have already aged out.
    member _.Count =
        lock
            lockObj
            (fun () ->
                evictExpired (clock.UtcNow.Ticks)
                count
            )

    /// Maximum requests allowed per window.
    member _.MaxRequests = maxRequests

    /// Window duration.
    member _.Window = window
