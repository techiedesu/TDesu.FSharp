namespace TDesu.FSharp.Resilience

open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Retry combinators with exponential backoff and fixed delay strategies.
/// </summary>
/// <namespacedoc>
///   <summary>Resilience patterns: Retry (exponential backoff), CircuitBreaker, Timeout, Memoize (with TTL), Saga (transactional orchestration).</summary>
/// </namespacedoc>
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
        if maxRetries < 0 then
            invalidArg (nameof maxRetries) $"Must be non-negative, got %d{maxRetries}"

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

            if not success then
                raise lastEx

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
    let tryWithBackoff
        (maxRetries: int)
        (baseDelay: TimeSpan)
        (ct: CancellationToken)
        (f: unit -> Task<'T>)
        : Task<Result<'T, exn>> =
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
        if maxRetries < 0 then
            invalidArg (nameof maxRetries) $"Must be non-negative, got %d{maxRetries}"

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

            if not success then
                raise lastEx

            return result
        }
