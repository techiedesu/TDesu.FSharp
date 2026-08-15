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
    type private CalData(seconds: int64, ms: int64, ticks: int64) =
        member _.Seconds = seconds
        member _.Ms = ms
        member _.Ticks = ticks

    let private sw = Stopwatch.StartNew()
    let mutable private cal =
        let now = DateTimeOffset.UtcNow
        CalData(now.ToUnixTimeSeconds(), now.ToUnixTimeMilliseconds(), sw.ElapsedTicks)

    /// Recalibrate from system clock. Called automatically if drift > 1 minute.
    /// Thread-safe: the CalData reference is swapped atomically.
    let recalibrate () =
        let now = DateTimeOffset.UtcNow
        Threading.Volatile.Write(&cal,
            CalData(now.ToUnixTimeSeconds(), now.ToUnixTimeMilliseconds(), sw.ElapsedTicks))

    /// <summary>
    /// Current Unix timestamp in seconds (fast, cached).
    /// </summary>
    /// <returns>Unix seconds since epoch.</returns>
    let seconds () : int64 =
        let c = Threading.Volatile.Read(&cal)
        let elapsed = (sw.ElapsedTicks - c.Ticks) / Stopwatch.Frequency
        let result = c.Seconds + elapsed
        if elapsed > 60L then recalibrate ()
        result

    /// <summary>
    /// Current Unix timestamp in milliseconds (fast, cached).
    /// </summary>
    /// <returns>Unix milliseconds since epoch.</returns>
    let milliseconds () : int64 =
        let c = Threading.Volatile.Read(&cal)
        let elapsedMs = (sw.ElapsedTicks - c.Ticks) * 1000L / Stopwatch.Frequency
        let result = c.Ms + elapsedMs
        if elapsedMs > 60000L then recalibrate ()
        result

    /// Current Unix timestamp in seconds as int32 (for protocols that use 32-bit timestamps).
    let inline seconds32 () : int32 = int32 (seconds ())
