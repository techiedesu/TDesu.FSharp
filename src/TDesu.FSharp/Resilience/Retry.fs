namespace TDesu.FSharp.Resilience

open System
open System.Runtime.ExceptionServices
open System.Threading
open System.Threading.Tasks
open TDesu.FSharp

/// <summary>
/// Retry combinators with exponential backoff and fixed delay strategies.
/// </summary>
/// <namespacedoc>
///   <summary>Resilience patterns: Retry (exponential backoff), CircuitBreaker, Timeout, Memoize (with TTL), Saga (transactional orchestration).</summary>
/// </namespacedoc>
[<RequireQualifiedAccess>]
module Retry =
    /// Re-raises <paramref name="ex"/> with its original stack trace intact, usable where an
    /// expression of type <c>'a</c> is required (a <c>task</c> exception handler cannot use
    /// F#'s <c>reraise()</c>).
    /// <param name="ex">The exception to re-raise.</param>
    let inline private reraiseCaptured (ex: exn) : 'a =
        ExceptionDispatchInfo.Capture(ex).Throw()
        Unchecked.defaultof<'a>

    /// True when <paramref name="oce"/> was raised by <paramref name="ct"/> itself.
    /// <param name="ct">The retry loop's own cancellation token.</param>
    /// <param name="oce">The caught cancellation.</param>
    let inline private isOwnCancellation (ct: CancellationToken) (oce: OperationCanceledException) =
        ct.IsCancellationRequested && oce.CancellationToken = ct

    /// Wraps a cancellation raised by a token other than the retry loop's own: the resulting task
    /// must fault rather than cancel, since the loop's own <paramref name="ct"/> was never cancelled
    /// and a caller checking <c>Task.IsCanceled</c> must not be misled into thinking it was.
    /// <param name="oce">The foreign cancellation to wrap.</param>
    let inline private foreignCancellation (oce: OperationCanceledException) : exn =
        InvalidOperationException(
            "The operation was canceled by a cancellation token other than the one given to the retry loop. Not retried, and not reported as the retry loop's own cancellation.",
            oce
        )
        :> exn

    /// <summary>
    /// The maximum delay <see cref="backoffDelay"/> (and so <see cref="withBackoff"/>) will ever wait
    /// between attempts, regardless of how large the exponential computation grows.
    /// </summary>
    let maxBackoffDelay = TimeSpan.FromMinutes 1.

    /// <summary>
    /// The delay before retry number <paramref name="attempt"/> (zero-based): <paramref name="baseDelay"/>
    /// doubled <paramref name="attempt"/> times, capped at <see cref="maxBackoffDelay"/>.
    /// </summary>
    /// <param name="baseDelay">Initial delay; doubles after each failed attempt.</param>
    /// <param name="attempt">The zero-based attempt number that just failed.</param>
    let backoffDelay (baseDelay: TimeSpan) (attempt: int) : TimeSpan =
        let rawMs = baseDelay.TotalMilliseconds * pown 2.0 attempt
        TimeSpan.FromMilliseconds(min rawMs maxBackoffDelay.TotalMilliseconds)

    /// <summary>
    /// Retries <paramref name="f"/> up to <paramref name="maxRetries"/> times with exponential backoff.
    /// Base delay doubles each attempt, capped at <see cref="maxBackoffDelay"/>. Throws the last
    /// exception if all attempts fail.
    /// </summary>
    /// <remarks>
    /// Cancellation is never retried. An <see cref="System.OperationCanceledException"/> raised by
    /// <paramref name="ct"/> itself propagates as-is — the returned task ends up <c>Canceled</c>, like
    /// any task cancelled through its own token. One raised through any other token is not retried
    /// either, but surfaces as a <c>Faulted</c> task wrapping the original exception: <paramref name="ct"/>
    /// was never cancelled, so reporting <c>Canceled</c> would mislead a caller that checks it.
    /// </remarks>
    /// <example>
    /// <code>
    /// let! result = Retry.withBackoff 3 (TimeSpan.FromSeconds 1.) (fun () -> httpClient.GetAsync(url))
    /// </code>
    /// </example>
    /// <exception cref="System.ArgumentException">When <paramref name="maxRetries"/> or <paramref name="baseDelay"/> is negative.</exception>
    /// <exception cref="System.Exception">Re-raises the last exception after all retries are exhausted.</exception>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="baseDelay">Initial delay; doubles after each failed attempt.</param>
    /// <param name="ct">Cancellation token to abort the retry loop.</param>
    /// <param name="f">Async factory to invoke on each attempt.</param>
    let withBackoff (maxRetries: int) (baseDelay: TimeSpan) (ct: CancellationToken) (f: unit -> Task<'T>) : Task<'T> =
        if maxRetries < 0 then
            invalidArg (nameof maxRetries) $"Must be non-negative, got %d{maxRetries}"

        if baseDelay < TimeSpan.Zero then
            invalidArg (nameof baseDelay) $"Must be non-negative, got %A{baseDelay}"

        task {
            let mutable attempt = 0
            let mutable lastEx: exn = null
            let mutable result = Unchecked.defaultof<'T>
            let mutable success = false
            let mutable stop = false

            while not success && not stop && attempt <= maxRetries do
                ct.ThrowIfCancellationRequested()

                try
                    let! v = f ()
                    result <- v
                    success <- true
                with
                | :? OperationCanceledException as oce when isOwnCancellation ct oce ->
                    ExceptionDispatchInfo.Capture(oce).Throw()
                | :? OperationCanceledException as oce ->
                    lastEx <- foreignCancellation oce
                    stop <- true
                | ex ->
                    lastEx <- ex

                    if attempt < maxRetries then
                        do!
                            Task.Delay(
                                int (min (backoffDelay baseDelay attempt).TotalMilliseconds (float Int32.MaxValue)),
                                ct
                            )

                    attempt <- attempt + 1

            if not success then
                ExceptionDispatchInfo.Capture(lastEx).Throw()

            return result
        }

    /// <summary>
    /// Retries <paramref name="f"/> with exponential backoff, returning <c>Result</c> instead of throwing.
    /// </summary>
    /// <remarks>
    /// Cancellation through the retry loop's own <paramref name="ct"/> is never turned into an
    /// <c>Error</c>: it propagates, leaving the returned task itself <c>Canceled</c>. Every other
    /// failure — including a cancellation from a foreign token, which <see cref="withBackoff"/>
    /// already turns into an ordinary failure — becomes <c>Error</c>.
    /// </remarks>
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
            with
            | :? OperationCanceledException as oce when isOwnCancellation ct oce -> return reraiseCaptured oce
            | ex -> return Error ex
        }

    /// <summary>
    /// Retries <paramref name="f"/> with a fixed delay between attempts.
    /// </summary>
    /// <remarks>
    /// Cancellation is handled exactly as in <see cref="withBackoff"/>: never retried, the retry
    /// loop's own cancellation propagates as <c>Canceled</c>, a foreign one surfaces as <c>Faulted</c>.
    /// </remarks>
    /// <exception cref="System.ArgumentException">When <paramref name="maxRetries"/> or <paramref name="delay"/> is negative.</exception>
    /// <exception cref="System.Exception">Re-raises the last exception after all retries are exhausted.</exception>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="delay">Fixed delay between each attempt.</param>
    /// <param name="ct">Cancellation token to abort the retry loop.</param>
    /// <param name="f">Async factory to invoke on each attempt.</param>
    let withDelay (maxRetries: int) (delay: TimeSpan) (ct: CancellationToken) (f: unit -> Task<'T>) : Task<'T> =
        if maxRetries < 0 then
            invalidArg (nameof maxRetries) $"Must be non-negative, got %d{maxRetries}"

        if delay < TimeSpan.Zero then
            invalidArg (nameof delay) $"Must be non-negative, got %A{delay}"

        task {
            let mutable attempt = 0
            let mutable lastEx: exn = null
            let mutable result = Unchecked.defaultof<'T>
            let mutable success = false
            let mutable stop = false

            while not success && not stop && attempt <= maxRetries do
                ct.ThrowIfCancellationRequested()

                try
                    let! v = f ()
                    result <- v
                    success <- true
                with
                | :? OperationCanceledException as oce when isOwnCancellation ct oce ->
                    ExceptionDispatchInfo.Capture(oce).Throw()
                | :? OperationCanceledException as oce ->
                    lastEx <- foreignCancellation oce
                    stop <- true
                | ex ->
                    lastEx <- ex

                    if attempt < maxRetries then
                        do! Task.Delay(delay, ct)

                    attempt <- attempt + 1

            if not success then
                ExceptionDispatchInfo.Capture(lastEx).Throw()

            return result
        }
