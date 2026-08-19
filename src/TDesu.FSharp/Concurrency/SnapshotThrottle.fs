namespace TDesu.FSharp.Concurrency

open System

/// <summary>
/// Snapshot throttle — tracks message count and triggers save at threshold (thread-safe).
/// </summary>
/// <param name="threshold">The number of messages before a snapshot is triggered. Must be positive.</param>
[<Sealed>]
type SnapshotThrottle(threshold: int) =
    do
        if threshold <= 0 then
            invalidArg (nameof threshold) $"Must be positive, got %d{threshold}"

    let mutable counter = 0

    /// <summary>
    /// Records a message. Returns <c>true</c> if threshold reached (caller should save snapshot).
    /// </summary>
    /// <remarks>Thread-safe: uses atomic increment. Under contention, may trigger slightly past threshold.</remarks>
    member _.Record() =
        let newVal = Threading.Interlocked.Increment(&counter)

        if newVal >= threshold then
            // CAS: only the first thread past the threshold resets and triggers
            Threading.Interlocked.CompareExchange(&counter, 0, newVal) = newVal
        else
            false

    /// Resets the counter.
    member _.Reset() =
        Threading.Interlocked.Exchange(&counter, 0) |> ignore

    /// Current count since last snapshot.
    member _.Count = Threading.Volatile.Read(&counter)
