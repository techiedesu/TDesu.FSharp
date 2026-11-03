namespace TDesu.FSharp

open System

/// <summary>
/// Fast cached Unix timestamp — avoids repeated syscalls.
/// Updated lazily, accurate to ~15ms (system timer resolution).
/// </summary>
/// <remarks>
/// Thread-safe. The <see cref="CalData"/> reference is swapped atomically.
/// On .NET, uses <see cref="System.Diagnostics.Stopwatch"/> for high-resolution elapsed time.
/// </remarks>
[<RequireQualifiedAccess>]
module UnixTime =
    open System.Diagnostics

    /// Calibration snapshot — swapped atomically as a single reference.
    [<Sealed>]
    type private CalData(ms: int64, ticks: int64) =
        member _.Ms = ms
        member _.Ticks = ticks

    let private sw = Stopwatch.StartNew()

    let mutable private cal =
        let now = DateTimeOffset.UtcNow
        CalData(now.ToUnixTimeMilliseconds(), sw.ElapsedTicks)

    /// Recalibrates from the system clock. Not a drift check: there is no comparison against the
    /// live clock here, only a swap to its current reading, and it is called automatically once
    /// elapsed time since the last calibration exceeds 60 seconds — so a system clock step (an NTP
    /// correction, say) is not noticed until that window next elapses.
    /// Thread-safe: the CalData reference is swapped atomically.
    let recalibrate () =
        let now = DateTimeOffset.UtcNow
        Threading.Volatile.Write(&cal, CalData(now.ToUnixTimeMilliseconds(), sw.ElapsedTicks))

    /// <summary>
    /// Current Unix timestamp in seconds (fast, cached).
    /// </summary>
    /// <returns>Unix seconds since epoch.</returns>
    let seconds () : int64 =
        let c = Threading.Volatile.Read(&cal)
        let elapsedMs = (sw.ElapsedTicks - c.Ticks) * 1000L / Stopwatch.Frequency
        let result = (c.Ms + elapsedMs) / 1000L

        if elapsedMs > 60000L then
            recalibrate ()

        result

    /// <summary>
    /// Current Unix timestamp in milliseconds (fast, cached).
    /// </summary>
    /// <returns>Unix milliseconds since epoch.</returns>
    let milliseconds () : int64 =
        let c = Threading.Volatile.Read(&cal)
        let elapsedMs = (sw.ElapsedTicks - c.Ticks) * 1000L / Stopwatch.Frequency
        let result = c.Ms + elapsedMs

        if elapsedMs > 60000L then
            recalibrate ()

        result

    /// Current Unix timestamp in seconds as int32 (for protocols that use 32-bit timestamps).
    let inline seconds32 () : int32 = int32 (seconds ())
