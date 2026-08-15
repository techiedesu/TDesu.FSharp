namespace TDesu.FSharp.Resilience

open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Thread-safe memoization with optional TTL.
/// </summary>
/// <remarks>
/// On .NET uses <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// TTL variants periodically clean up stale entries every 1000 operations.
/// </remarks>
[<RequireQualifiedAccess>]
module Memoize =

    open System.Collections.Concurrent

    /// Memoizes a function. Cached forever (or until process restart).
    /// <param name="f">Function whose results are cached by input key.</param>
    let create (f: 'TKey -> 'TValue) =
        let cache = ConcurrentDictionary<'TKey, 'TValue>()
        fun key -> cache.GetOrAdd(key, f)

    /// Memoizes an async function. Cached forever.
    /// Under contention, f may execute more than once per key; only the last result is kept.
    /// <param name="f">Async function whose results are cached by input key.</param>
    let createAsync (f: 'TKey -> Task<'TValue>) =
        let cache = ConcurrentDictionary<'TKey, 'TValue>()
        fun key -> task {
            match cache.TryGetValue key with
            | true, v -> return v
            | _ ->
                let! v = f key
                cache[key] <- v
                return v
        }

    /// Memoizes a function with time-to-live. Expired entries are recomputed.
    /// Stale entries are periodically cleaned up to prevent unbounded memory growth.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Function whose results are cached by input key.</param>
    let withTtl (ttl: TimeSpan) (f: 'TKey -> 'TValue) =
        let cache = ConcurrentDictionary<'TKey, struct('TValue * int64)>()
        let ttlTicks = ttl.Ticks
        let mutable ops = 0L
        fun key ->
            let nowTicks = DateTime.UtcNow.Ticks
            match cache.TryGetValue key with
            | true, struct(v, ts) when nowTicks - ts < ttlTicks -> v
            | _ ->
                let v = f key
                cache[key] <- struct(v, nowTicks)
                let n = Interlocked.Increment(&ops)
                if n % 1000L = 0L then
                    let cleanupNow = DateTime.UtcNow.Ticks
                    for kvp in cache do
                        let struct(_, ts) = kvp.Value
                        if cleanupNow - ts >= ttlTicks then
                            cache.TryRemove(kvp.Key) |> ignore
                v

    /// Memoizes an async function with time-to-live.
    /// Stale entries are periodically cleaned up to prevent unbounded memory growth.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Async function whose results are cached by input key.</param>
    let withTtlAsync (ttl: TimeSpan) (f: 'TKey -> Task<'TValue>) =
        let cache = ConcurrentDictionary<'TKey, struct('TValue * int64)>()
        let ttlTicks = ttl.Ticks
        let mutable ops = 0L
        fun key -> task {
            let nowTicks = DateTime.UtcNow.Ticks
            match cache.TryGetValue key with
            | true, struct(v, ts) when nowTicks - ts < ttlTicks -> return v
            | _ ->
                let! v = f key
                cache[key] <- struct(v, nowTicks)
                let n = Interlocked.Increment(&ops)
                if n % 1000L = 0L then
                    let cleanupNow = DateTime.UtcNow.Ticks
                    for kvp in cache do
                        let struct(_, ts) = kvp.Value
                        if cleanupNow - ts >= ttlTicks then
                            cache.TryRemove(kvp.Key) |> ignore
                return v
        }
