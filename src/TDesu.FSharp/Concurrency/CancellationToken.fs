namespace TDesu.FSharp.Concurrency

open System
open TDesu.FSharp.IO

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
    /// Creates a linked CTS: cancels when parent is canceled OR after timeout.
    /// Disposes both internal CTS on dispose. Use with <c>use</c>.
    /// </summary>
    /// <returns>A tuple of the linked CTS and a disposable that cleans up both.</returns>
    /// <param name="timeout">The duration after which cancellation is requested.</param>
    /// <param name="parent">The parent token to link with the timeout.</param>
    let linked (timeout: TimeSpan) (parent: CancellationToken) =
        let timeoutCts = new CancellationTokenSource(timeout)

        let linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(parent, timeoutCts.Token)

        linkedCts,
        Disposable.create (fun () ->
            linkedCts.Dispose()
            timeoutCts.Dispose()
        )

    /// Creates a linked CTS from a parent token (no timeout).
    /// <param name="parent">The parent token to link from.</param>
    let inline linkedFrom (parent: CancellationToken) =
        CancellationTokenSource.CreateLinkedTokenSource(parent)
