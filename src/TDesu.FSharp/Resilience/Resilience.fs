namespace TDesu.FSharp.Resilience

open System
open System.Threading
open System.Threading.Tasks
open TDesu.FSharp.Operators

/// <summary>
/// Retry combinators with exponential backoff and fixed delay strategies.
/// </summary>
[<RequireQualifiedAccess>]
module Retry =
    /// <summary>
    /// Retries <paramref name="f"/> up to <paramref name="maxRetries"/> times with exponential backoff.
    /// Base delay doubles each attempt. Throws the last exception if all attempts fail.
    /// </summary>
    /// <example>
    /// <code>
    /// let! result = Retry.withBackoff 3 (TimeSpan.FromSeconds 1.) (fun () -> httpClient.GetAsync(url))
    /// </code>
    /// </example>
    /// <exception cref="System.Exception">Re-raises the last exception after all retries are exhausted.</exception>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="baseDelay">Initial delay; doubles after each failed attempt.</param>
    /// <param name="ct">Cancellation token to abort the retry loop.</param>
    /// <param name="f">Async factory to invoke on each attempt.</param>
    let withBackoff (maxRetries: int) (baseDelay: TimeSpan) (ct: CancellationToken) (f: unit -> Task<'T>) : Task<'T> =
        if maxRetries < 0 then invalidArg (nameof maxRetries) $"Must be non-negative, got %d{maxRetries}"
        task {
            let mutable attempt = 0
            let mutable lastEx: exn = null
            let mutable result = Unchecked.defaultof<'T>
            let mutable success = false
            while not success && attempt <= maxRetries do
                ct.ThrowIfCancellationRequested()
                try
                    let! v = f ()
                    result <- v
                    success <- true
                with ex ->
                    lastEx <- ex
                    if attempt < maxRetries then
                        let delay = baseDelay.TotalMilliseconds * (pown 2.0 attempt)
                        do! Task.Delay(int (min delay (float Int32.MaxValue)), ct)
                    attempt <- attempt + 1
            if not success then raise lastEx
            return result
        }

    /// <summary>
    /// Retries <paramref name="f"/> with exponential backoff, returning <c>Result</c> instead of throwing.
    /// </summary>
    /// <returns><c>Ok(value)</c> on success, <c>Error(exn)</c> after all retries fail.</returns>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="baseDelay">Initial delay; doubles after each failed attempt.</param>
    /// <param name="ct">Cancellation token to abort the retry loop.</param>
    /// <param name="f">Async factory to invoke on each attempt.</param>
    let tryWithBackoff (maxRetries: int) (baseDelay: TimeSpan) (ct: CancellationToken) (f: unit -> Task<'T>) : Task<Result<'T, exn>> =
        task {
            try
                let! v = withBackoff maxRetries baseDelay ct f
                return Ok v
            with ex ->
                return Error ex
        }

    /// Retries f with a fixed delay between attempts.
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="delay">Fixed delay between each attempt.</param>
    /// <param name="ct">Cancellation token to abort the retry loop.</param>
    /// <param name="f">Async factory to invoke on each attempt.</param>
    let withDelay (maxRetries: int) (delay: TimeSpan) (ct: CancellationToken) (f: unit -> Task<'T>) : Task<'T> =
        if maxRetries < 0 then invalidArg (nameof maxRetries) $"Must be non-negative, got %d{maxRetries}"
        task {
            let mutable attempt = 0
            let mutable lastEx: exn = null
            let mutable result = Unchecked.defaultof<'T>
            let mutable success = false
            while not success && attempt <= maxRetries do
                ct.ThrowIfCancellationRequested()
                try
                    let! v = f ()
                    result <- v
                    success <- true
                with ex ->
                    lastEx <- ex
                    if attempt < maxRetries then
                        do! Task.Delay(delay, ct)
                    attempt <- attempt + 1
            if not success then raise lastEx
            return result
        }

#if !FABLE_COMPILER
/// <summary>
/// Timeout combinators — enforce deadlines on async operations.
/// </summary>
[<RequireQualifiedAccess>]
module Timeout =
    /// <summary>
    /// Runs <paramref name="work"/> with a hard deadline. Throws <see cref="TimeoutException"/> if exceeded.
    /// Propagates <see cref="CancellationToken"/> so underlying work can cooperatively stop.
    /// </summary>
    /// <exception cref="System.TimeoutException">When the operation exceeds <paramref name="duration"/>.</exception>
    /// <param name="duration">Maximum time allowed before the operation is cancelled.</param>
    /// <param name="work">Async work that receives a cancellation token linked to the deadline.</param>
    let after (duration: TimeSpan) (work: CancellationToken -> Task<'T>) : Task<'T> =
        task {
            use cts = new CancellationTokenSource(duration)
            try
                return! work cts.Token
            with :? OperationCanceledException when cts.IsCancellationRequested ->
                return timedOutf "Operation exceeded %gms" duration.TotalMilliseconds
        }

    /// Runs work with a deadline, linked to a parent cancellation token.
    /// <param name="duration">Maximum time allowed before the operation is cancelled.</param>
    /// <param name="parentCt">Parent token that can also trigger cancellation.</param>
    /// <param name="work">Async work that receives a cancellation token linked to both the deadline and the parent token.</param>
    let afterLinked (duration: TimeSpan) (parentCt: CancellationToken) (work: CancellationToken -> Task<'T>) : Task<'T> =
        task {
            use cts = new CancellationTokenSource(duration)
            use linked = CancellationTokenSource.CreateLinkedTokenSource(parentCt, cts.Token)
            try
                return! work linked.Token
            with :? OperationCanceledException when cts.IsCancellationRequested && not parentCt.IsCancellationRequested ->
                return timedOutf "Operation exceeded %gms" duration.TotalMilliseconds
        }
#endif

/// <summary>
/// Simple circuit breaker — prevents cascading failures by tracking consecutive errors.
/// </summary>
/// <remarks>
/// Thread-safe on .NET (uses <c>lock</c>). On Fable, single-threaded by design.
/// State transitions: <c>Closed → Open → HalfOpen → Closed</c>.
/// </remarks>
[<RequireQualifiedAccess>]
module CircuitBreaker =

    /// Circuit breaker state.
    type State =
        | Closed of failures: int
        | Open of resetAt: DateTime
        | HalfOpen

    /// Circuit breaker configuration.
    type Config = {
        /// Number of consecutive failures before opening the circuit.
        Threshold: int

        /// How long the circuit stays open before allowing a probe.
        Cooldown: TimeSpan
    }

    /// <summary>
    /// Creates a circuit breaker. Returns a function that wraps calls.
    /// </summary>
    /// <remarks>
    /// State transitions are atomic; the wrapped function <c>f</c> runs outside the lock.
    /// After <see cref="Config.Threshold"/> consecutive failures, the circuit opens for <see cref="Config.Cooldown"/>.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">Thrown when calling through an open circuit.</exception>
    /// <param name="config">Circuit breaker configuration specifying threshold and cooldown.</param>
    let create (config: Config) =
        let state = ref (Closed 0)
#if !FABLE_COMPILER
        let sync = obj ()
        let inline writeState v = lock sync (fun () -> state.Value <- v)
        let inline atomicCheckAndTransition () =
            lock sync (fun () ->
                let now = DateTime.UtcNow
                match state.Value with
                | Open resetAt when now < resetAt -> state.Value
                | Open _ -> state.Value <- HalfOpen; HalfOpen
                | s -> s)
        let inline atomicRecordFailure () =
            lock sync (fun () ->
                let now = DateTime.UtcNow
                match state.Value with
                | Closed failures ->
                    let n = failures + 1
                    state.Value <-
                        if n >= config.Threshold then Open(now.Add config.Cooldown)
                        else Closed n
                | _ ->
                    state.Value <- Open(now.Add config.Cooldown)
                state.Value)
#else
        let inline writeState v = state.Value <- v
        let inline atomicCheckAndTransition () =
            let now = DateTime.UtcNow
            match state.Value with
            | Open resetAt when now < resetAt -> state.Value
            | Open _ -> state.Value <- HalfOpen; HalfOpen
            | s -> s
        let inline atomicRecordFailure () =
            let now = DateTime.UtcNow
            match state.Value with
            | Closed failures ->
                let n = failures + 1
                state.Value <-
                    if n >= config.Threshold then Open(now.Add config.Cooldown)
                    else Closed n
            | _ ->
                state.Value <- Open(now.Add config.Cooldown)
            state.Value
#endif

        fun (f: unit -> Task<'T>) -> task {
            match atomicCheckAndTransition () with
            | Open _ ->
                return invalidOp "Circuit is open"
            | HalfOpen ->
                try
                    let! result = f ()
                    writeState (Closed 0)
                    return result
                with ex ->
                    let _ = atomicRecordFailure ()
                    return raise ex
            | Closed failures ->
                try
                    let! result = f ()
                    if failures > 0 then
                        writeState (Closed 0)
                    return result
                with ex ->
                    let _ = atomicRecordFailure ()
                    return raise ex
        }

/// <summary>
/// Thread-safe memoization with optional TTL.
/// </summary>
/// <remarks>
/// On .NET uses <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// On Fable uses plain <c>Dictionary</c> (JS is single-threaded).
/// TTL variants periodically clean up stale entries every 1000 operations.
/// </remarks>
[<RequireQualifiedAccess>]
module Memoize =

#if FABLE_COMPILER
    open System.Collections.Generic

    /// Memoizes a function. Cached forever (or until process restart).
    /// <param name="f">Function whose results are cached by input key.</param>
    let create (f: 'TKey -> 'TValue) =
        let cache = Dictionary<'TKey, 'TValue>()
        fun key ->
            match cache.TryGetValue key with
            | true, v -> v
            | _ ->
                let v = f key
                cache[key] <- v
                v

    /// Memoizes an async function. Cached forever.
    /// <param name="f">Async function whose results are cached by input key.</param>
    let createAsync (f: 'TKey -> Task<'TValue>) =
        let cache = Dictionary<'TKey, 'TValue>()
        fun key -> task {
            match cache.TryGetValue key with
            | true, v -> return v
            | _ ->
                let! v = f key
                cache[key] <- v
                return v
        }

    /// Memoizes a function with time-to-live. Expired entries are recomputed.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Function whose results are cached by input key.</param>
    let withTtl (ttl: TimeSpan) (f: 'TKey -> 'TValue) =
        let cache = Dictionary<'TKey, struct('TValue * DateTime)>()
        fun key ->
            let now = DateTime.UtcNow
            match cache.TryGetValue key with
            | true, struct(v, ts) when now - ts < ttl -> v
            | _ ->
                let v = f key
                cache[key] <- struct(v, now)
                v

    /// Memoizes an async function with time-to-live.
    /// <param name="ttl">Duration before a cached entry expires.</param>
    /// <param name="f">Async function whose results are cached by input key.</param>
    let withTtlAsync (ttl: TimeSpan) (f: 'TKey -> Task<'TValue>) =
        let cache = Dictionary<'TKey, struct('TValue * DateTime)>()
        fun key -> task {
            let now = DateTime.UtcNow
            match cache.TryGetValue key with
            | true, struct(v, ts) when now - ts < ttl -> return v
            | _ ->
                let! v = f key
                cache[key] <- struct(v, now)
                return v
        }
#else
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
#endif

/// <summary>
/// Saga orchestrator — executes steps sequentially with automatic compensation on failure.
/// </summary>
/// <remarks>
/// On failure, compensates all completed steps in reverse order (LIFO).
/// If compensations also fail, returns <see cref="AggregateException"/> containing all errors.
/// </remarks>
[<RequireQualifiedAccess>]
module Saga =
    /// A saga step: an action that can be compensated (rolled back).
    [<NoEquality; NoComparison>]
    type Step<'ctx> = {
        Name: string
        Execute: 'ctx -> Task<'ctx>
        Compensate: 'ctx -> Task<unit>
    }

    /// Creates a saga step.
    /// <param name="name">Descriptive name for the step (used in diagnostics).</param>
    /// <param name="execute">Async action that advances the saga context.</param>
    /// <param name="compensate">Async rollback action invoked on failure.</param>
    let step name execute compensate =
        { Name = name; Execute = execute; Compensate = compensate }

    /// Creates a saga step with no compensation (fire-and-forget).
    /// <param name="name">Descriptive name for the step (used in diagnostics).</param>
    /// <param name="execute">Async action that advances the saga context.</param>
    let stepNoCompensate name execute =
        { Name = name; Execute = execute; Compensate = fun _ -> task { return () } }

    /// Runs saga steps sequentially. On failure, compensates all completed steps in reverse.
    /// Each compensation receives the context that was the output of that step.
    /// If compensations also fail, returns AggregateException containing the original + compensation errors.
    /// <param name="steps">Ordered list of saga steps to execute sequentially.</param>
    /// <param name="ctx">Initial context passed to the first step.</param>
    let run (steps: Step<'ctx> list) (ctx: 'ctx) : Task<Result<'ctx, exn>> =
        task {
            let mutable completed: (Step<'ctx> * 'ctx) list = []
            let mutable current = ctx
            try
                for s in steps do
                    let! next = s.Execute current
                    completed <- (s, next) :: completed
                    current <- next
                return Ok current
            with ex ->
                let mutable compensationErrors = []
                for s, ctxAfterStep in completed do
                    try do! s.Compensate ctxAfterStep
                    with cex -> compensationErrors <- cex :: compensationErrors
                let error : exn =
                    match compensationErrors with
                    | [] -> ex
                    | _ -> AggregateException(ex :: List.rev compensationErrors)
                return Error error
        }
