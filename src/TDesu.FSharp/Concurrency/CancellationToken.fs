namespace TDesu.FSharp.Concurrency

open System

/// <summary>
/// CancellationToken helpers — reduces boilerplate for timeout + linked patterns.
/// </summary>
/// <namespacedoc>
///   <summary>Concurrency primitives: AtomicInt/Int64, BoundedDict, BoundedQueue, Signal, PeriodicTimer, ChannelWorker, SlidingWindowLimiter.</summary>
/// </namespacedoc>
[<RequireQualifiedAccess>]
module CancellationToken =
    open System.Threading

    /// Creates a CTS that cancels after the given timeout. Use with <c>use</c>.
    /// <param name="timeout">The duration after which cancellation is requested.</param>
    let inline withTimeout (timeout: TimeSpan) = new CancellationTokenSource(timeout)

    /// <summary>
    /// Creates a linked CTS: cancels when parent is canceled OR after timeout. A single object —
    /// disposing it tears down both the parent link and the timeout timer, so <c>use</c> applies
    /// directly and there is no second disposable to forget.
    /// </summary>
    /// <returns>The linked CTS. Dispose it to release both the link and the timeout timer.</returns>
    /// <param name="timeout">The duration after which cancellation is requested.</param>
    /// <param name="parent">The parent token to link with the timeout.</param>
    let linked (timeout: TimeSpan) (parent: CancellationToken) : CancellationTokenSource =
        let cts = CancellationTokenSource.CreateLinkedTokenSource(parent)
        cts.CancelAfter(timeout)
        cts

    /// Creates a linked CTS from a parent token (no timeout).
    /// <param name="parent">The parent token to link from.</param>
    let inline linkedFrom (parent: CancellationToken) =
        CancellationTokenSource.CreateLinkedTokenSource(parent)
