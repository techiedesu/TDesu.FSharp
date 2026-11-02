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
    open TDesu.FSharp.Concurrency

    /// Memoizes a function. Cached forever (or until process restart).
    /// <param name="f">Function whose results are cached by input key.</param>
    let create (f: 'TKey -> 'TValue) =
        let cache = ConcurrentDictionary<'TKey, 'TValue>()
        fun key -> cache.GetOrAdd(key, f)

    /// Memoizes an async function. Cached forever.
    /// Concurrent calls for a key that is not cached yet share one execution of f
    /// (<see cref="T:TDesu.FSharp.Concurrency.SingleFlight`2"/>). A failure is not cached: every
    /// caller that shared the failed execution sees it, and the next call runs f again.
    /// <param name="f">Async function whose results are cached by input key.</param>
    let createAsync (f: 'TKey -> Task<'TValue>) =
        let cache = ConcurrentDictionary<'TKey, 'TValue>()
        let flights = SingleFlight<'TKey, 'TValue>()

        fun key ->
            match cache.TryGetValue key with
            | true, v -> Task.FromResult v
            | _ ->
                flights.Run(
                    key,
                    fun () ->
                        task {
                            // A run that finished between the miss above and this one already
                            // filled the cache; asking f again would be the duplicate this prevents.
                            match cache.TryGetValue key with
                            | true, v -> return v
                            | _ ->
                                let! v = f key
                                cache[key] <- v
                                return v
                        }
                )

    /// Memoizes a function with time-to-live. Expired entries are recomputed.
    /// Stale entries are periodically cleaned up to prevent unbounded memory growth.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Function whose results are cached by input key.</param>
    let withTtl (ttl: TimeSpan) (f: 'TKey -> 'TValue) =
        let cache = ConcurrentDictionary<'TKey, struct ('TValue * int64)>()
        let ttlTicks = ttl.Ticks
        let mutable ops = 0L

        fun key ->
            let nowTicks = DateTime.UtcNow.Ticks

            match cache.TryGetValue key with
            | true, struct (v, ts) when nowTicks - ts < ttlTicks -> v
            | _ ->
                let v = f key
                cache[key] <- struct (v, nowTicks)
                let n = Interlocked.Increment(&ops)

                if n % 1000L = 0L then
                    let cleanupNow = DateTime.UtcNow.Ticks

                    for kvp in cache do
                        let struct (_, ts) = kvp.Value

                        if cleanupNow - ts >= ttlTicks then
                            cache.TryRemove(kvp.Key) |> ignore

                v

    /// Memoizes an async function with time-to-live.
    /// Concurrent calls for a key that is missing or expired share one execution of f, as in
    /// <see cref="createAsync"/>.
    /// Stale entries are periodically cleaned up to prevent unbounded memory growth.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Async function whose results are cached by input key.</param>
    let withTtlAsync (ttl: TimeSpan) (f: 'TKey -> Task<'TValue>) =
        let cache = ConcurrentDictionary<'TKey, struct ('TValue * int64)>()
        let flights = SingleFlight<'TKey, 'TValue>()
        let ttlTicks = ttl.Ticks
        let mutable ops = 0L

        let fresh key =
            match cache.TryGetValue key with
            | true, struct (v, ts) when DateTime.UtcNow.Ticks - ts < ttlTicks -> ValueSome v
            | _ -> ValueNone

        fun key ->
            match fresh key with
            | ValueSome v -> Task.FromResult v
            | ValueNone ->
                flights.Run(
                    key,
                    fun () ->
                        task {
                            match fresh key with
                            | ValueSome v -> return v
                            | ValueNone ->
                                let! v = f key
                                let nowTicks = DateTime.UtcNow.Ticks
                                cache[key] <- struct (v, nowTicks)
                                let n = Interlocked.Increment(&ops)

                                if n % 1000L = 0L then
                                    for kvp in cache do
                                        let struct (_, ts) = kvp.Value

                                        if nowTicks - ts >= ttlTicks then
                                            cache.TryRemove(kvp.Key) |> ignore

                                return v
                        }
                )
