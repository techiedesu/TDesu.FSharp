namespace TDesu.FSharp.Resilience

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Thread-safe memoization with optional TTL.
/// </summary>
/// <remarks>
/// On .NET uses <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// TTL variants periodically clean up stale entries: every 1000 recomputes (cache misses and
/// expirations), a cleanup pass removes every entry it observes as expired, conditional on the
/// exact value it observed — a concurrent refresh of that same key in the meantime is never undone
/// by the sweep.
/// Elapsed time for TTL is measured with <see cref="System.Diagnostics.Stopwatch"/>, which is
/// monotonic — unlike <see cref="System.DateTime.UtcNow"/>, a backward wall-clock step (an NTP
/// correction) cannot make an expired entry look fresh again, nor a forward step expire everything
/// at once.
/// </remarks>
[<RequireQualifiedAccess>]
module Memoize =

    open System.Collections.Concurrent
    open TDesu.FSharp.Concurrency

    /// Converts a <see cref="System.TimeSpan"/> to an equivalent span of
    /// <see cref="System.Diagnostics.Stopwatch"/> ticks.
    let inline private stopwatchTicksOf (span: TimeSpan) : int64 =
        int64 (span.TotalSeconds * float Stopwatch.Frequency)

    /// Removes <paramref name="entry"/> from <paramref name="cache"/> only if the key still maps to
    /// exactly that value — <c>ConcurrentDictionary.TryRemove(KeyValuePair)</c> isn't part of the
    /// netstandard2.1 surface, so this goes through the same conditional removal via the explicit
    /// <see cref="System.Collections.Generic.ICollection{T}"/> implementation every dictionary has had
    /// since .NET Framework 4.
    /// <param name="cache">The dictionary to remove from.</param>
    /// <param name="entry">The key/value pair that must still be current for the removal to apply.</param>
    let inline private removeIfUnchanged
        (cache: ConcurrentDictionary<'TKey, 'TEntry>)
        (entry: KeyValuePair<'TKey, 'TEntry>)
        =
        (cache :> ICollection<KeyValuePair<'TKey, 'TEntry>>).Remove(entry) |> ignore

    /// Memoizes a function. Cached forever (or until process restart).
    /// <param name="f">Function whose results are cached by input key.</param>
    let create (f: 'TKey -> 'TValue) =
        let cache = ConcurrentDictionary<'TKey, 'TValue>()
        let factory = Func<'TKey, 'TValue>(f)

        fun key ->
            match cache.TryGetValue key with
            | true, v -> v
            | _ -> cache.GetOrAdd(key, factory)

    /// Memoizes an async function. Cached forever.
    /// Concurrent calls for a key that is not cached yet share one execution of f
    /// (<see cref="T:TDesu.FSharp.Concurrency.SingleFlight`2"/>). A failure is not cached: every
    /// caller that shared the failed execution sees it, and the next call runs f again.
    /// <param name="f">Async function whose results are cached by input key.</param>
    let createAsync (f: 'TKey -> Task<'TValue>) =
        let cache = ConcurrentDictionary<'TKey, Task<'TValue>>()
        let flights = SingleFlight<'TKey, 'TValue>()

        fun key ->
            match cache.TryGetValue key with
            | true, t -> t
            | _ ->
                flights.Run(
                    key,
                    fun () ->
                        task {
                            // A run that finished between the miss above and this one already
                            // filled the cache; asking f again would be the duplicate this prevents.
                            match cache.TryGetValue key with
                            | true, t -> return! t
                            | _ ->
                                let! v = f key
                                cache[key] <- Task.FromResult v
                                return v
                        }
                )

    /// Memoizes a function with time-to-live. Expired entries are recomputed.
    /// Stale entries are periodically cleaned up to prevent unbounded memory growth.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Function whose results are cached by input key.</param>
    let withTtl (ttl: TimeSpan) (f: 'TKey -> 'TValue) =
        let cache = ConcurrentDictionary<'TKey, struct ('TValue * int64)>()
        let ttlStopwatchTicks = stopwatchTicksOf ttl
        let mutable misses = 0L

        fun key ->
            match cache.TryGetValue key with
            | true, struct (v, ts) when Stopwatch.GetTimestamp() - ts < ttlStopwatchTicks -> v
            | _ ->
                // The clock is read only after f has run, so f's own duration is never charged
                // against the TTL — a factory slower than the TTL would otherwise never cache.
                let v = f key
                let nowTicks = Stopwatch.GetTimestamp()
                cache[key] <- struct (v, nowTicks)
                let n = Interlocked.Increment(&misses)

                if n % 1000L = 0L then
                    let cleanupNow = Stopwatch.GetTimestamp()

                    for kvp in cache do
                        let struct (_, ts) = kvp.Value

                        if cleanupNow - ts >= ttlStopwatchTicks then
                            // Conditional on the exact entry observed: a concurrent refresh of this
                            // key between the enumeration and this removal leaves the refreshed
                            // value in place instead of being undone by the sweep.
                            removeIfUnchanged cache kvp

                v

    /// Memoizes an async function with time-to-live.
    /// Concurrent calls for a key that is missing or expired share one execution of f, as in
    /// <see cref="createAsync"/>.
    /// Stale entries are periodically cleaned up to prevent unbounded memory growth.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Async function whose results are cached by input key.</param>
    let withTtlAsync (ttl: TimeSpan) (f: 'TKey -> Task<'TValue>) =
        let cache = ConcurrentDictionary<'TKey, struct (Task<'TValue> * int64)>()
        let flights = SingleFlight<'TKey, 'TValue>()
        let ttlStopwatchTicks = stopwatchTicksOf ttl
        let mutable misses = 0L

        let fresh key =
            match cache.TryGetValue key with
            | true, struct (t, ts) when Stopwatch.GetTimestamp() - ts < ttlStopwatchTicks -> ValueSome t
            | _ -> ValueNone

        fun key ->
            match fresh key with
            | ValueSome t -> t
            | ValueNone ->
                flights.Run(
                    key,
                    fun () ->
                        task {
                            match fresh key with
                            | ValueSome t -> return! t
                            | ValueNone ->
                                let! v = f key
                                let nowTicks = Stopwatch.GetTimestamp()
                                cache[key] <- struct (Task.FromResult v, nowTicks)
                                let n = Interlocked.Increment(&misses)

                                if n % 1000L = 0L then
                                    let cleanupNow = Stopwatch.GetTimestamp()

                                    for kvp in cache do
                                        let struct (_, ts) = kvp.Value

                                        if cleanupNow - ts >= ttlStopwatchTicks then
                                            removeIfUnchanged cache kvp

                                return v
                        }
                )
