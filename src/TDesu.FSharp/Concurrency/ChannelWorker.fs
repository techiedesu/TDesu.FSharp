namespace TDesu.FSharp.Concurrency

open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels

/// <summary>
/// Generic background worker that processes items sequentially from an internal queue.
/// Fault-tolerant: errors in handlers are reported via callback without crashing the worker.
/// Stops gracefully on cancellation.
/// </summary>
/// <example>
/// <code>
/// use cts = new CancellationTokenSource()
/// let worker = ChannelWorker.start (fun msg -> task { printfn "%s" msg }) (fun ex -> eprintfn "%A" ex) cts.Token
/// worker.Post("hello")
/// worker.Post("world")
/// cts.Cancel()
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module ChannelWorker =

    /// Handle for a running channel worker. Post items for background processing.
    [<Sealed>]
    type Handle<'T> internal (queue: ConcurrentQueue<'T>, signal: SemaphoreSlim, completion: Task) =
        /// <summary>
        /// Post an item for background processing. Non-blocking, unbounded.
        /// Safe to call after cancellation (silently ignored).
        /// </summary>
        /// <param name="item">The item to enqueue for processing.</param>
        member _.Post(item: 'T) =
            queue.Enqueue(item)

            try
                signal.Release() |> ignore
            with :? System.ObjectDisposedException ->
                ()

        /// Current number of queued (unprocessed) items.
        member _.PendingCount = queue.Count

        /// The task representing the worker loop. Completes when cancelled.
        member _.Completion = completion

    /// <summary>
    /// Starts a background worker that processes items sequentially.
    /// If processing an item throws, <paramref name="onError"/> is called and the worker continues.
    /// </summary>
    /// <remarks>
    /// Items posted after cancellation are silently ignored.
    /// Items already queued at cancellation time may not be processed.
    /// </remarks>
    /// <param name="handler">Async function to process each item.</param>
    /// <param name="onError">Called when handler throws. If this also throws, the exception is swallowed.</param>
    /// <param name="ct">Cancellation token to stop the worker.</param>
    let start (handler: 'T -> Task) (onError: exn -> unit) (ct: CancellationToken) : Handle<'T> =
        let queue = ConcurrentQueue<'T>()
        let signal = new SemaphoreSlim(0)

        let workerTask =
            task {
                try
                    try
                        while not ct.IsCancellationRequested do
                            do! signal.WaitAsync(ct)

                            match queue.TryDequeue() with
                            | true, item ->
                                try
                                    do! handler item
                                with ex ->
                                    try
                                        onError ex
                                    with _ ->
                                        ()
                            | _ -> ()
                    with :? System.OperationCanceledException ->
                        ()
                finally
                    signal.Dispose()
            }

        Handle(queue, signal, workerTask)

    /// <summary>
    /// Handle for a running bounded channel worker. Backed by
    /// <see cref="System.Threading.Channels.Channel{T}"/> instead of <see cref="Handle{T}"/>'s
    /// unbounded queue, so a producer that outruns the handler waits for room instead of growing
    /// memory without limit.
    /// </summary>
    [<Sealed>]
    type BoundedHandle<'T> internal (channel: Channel<'T>, workerCt: CancellationToken, completion: Task) =

        /// <summary>
        /// Queues <paramref name="item"/> if there is room; <c>false</c> when the queue is full,
        /// when <see cref="Complete"/> has been called, or once the worker's own cancellation
        /// token has fired. Never blocks.
        /// </summary>
        /// <param name="item">The item to enqueue for processing.</param>
        member _.TryPost(item: 'T) : bool = channel.Writer.TryWrite(item)

        /// <summary>
        /// Waits for room, then queues <paramref name="item"/>.
        /// </summary>
        /// <exception cref="System.OperationCanceledException">
        /// When <paramref name="ct"/> fires, or the worker's own cancellation token fires first.
        /// </exception>
        /// <exception cref="System.Threading.Channels.ChannelClosedException">
        /// When <see cref="Complete"/> was already called.
        /// </exception>
        /// <param name="item">The item to enqueue for processing.</param>
        /// <param name="ct">Cancellation token for this call.</param>
        member _.PostAsync(item: 'T, ct: CancellationToken) : Task =
            task {
                use linked = CancellationTokenSource.CreateLinkedTokenSource(ct, workerCt)
                do! channel.Writer.WriteAsync(item, linked.Token)
            }
            :> Task

        /// Current number of queued (unprocessed) items.
        member _.PendingCount = channel.Reader.Count

        /// <summary>
        /// Stops accepting new items but lets the worker finish everything already queued —
        /// <see cref="Completion"/> completes once the queue drains. The graceful counterpart to
        /// cancelling the token passed to <see cref="startBounded"/>, which abandons the queue as
        /// soon as the handler in progress returns and can leave items unprocessed.
        /// </summary>
        /// <remarks>Idempotent — safe to call more than once, or after the worker has already stopped.</remarks>
        member _.Complete() : unit = channel.Writer.TryComplete() |> ignore

        /// The task representing the worker loop. Completes once the cancellation token passed to
        /// <see cref="startBounded"/> fires — possibly with items still unprocessed — or once
        /// <see cref="Complete"/> was called and the queue has fully drained.
        member _.Completion = completion

    /// <summary>
    /// Starts a background worker that processes items sequentially from a queue of at most
    /// <paramref name="capacity"/> items. If processing an item throws, <paramref name="onError"/>
    /// is called and the worker continues.
    /// </summary>
    /// <remarks>
    /// Cancelling <paramref name="ct"/> abandons the queue: the worker stops as soon as the handler
    /// in progress returns, and items still queued may never be processed. Call
    /// <see cref="Complete"/> instead for a shutdown that drains them first.
    /// </remarks>
    /// <param name="capacity">Maximum number of items the queue holds at once.</param>
    /// <param name="handler">Async function to process each item.</param>
    /// <param name="onError">Called when handler throws. If this also throws, the exception is swallowed.</param>
    /// <param name="ct">Cancellation token to stop the worker.</param>
    let startBounded
        (capacity: int)
        (handler: 'T -> Task)
        (onError: exn -> unit)
        (ct: CancellationToken)
        : BoundedHandle<'T> =
        if capacity <= 0 then
            invalidArg (nameof capacity) $"Must be positive, got %d{capacity}"

        let channel =
            Channel.CreateBounded<'T>(
                BoundedChannelOptions(capacity, SingleReader = true, FullMode = BoundedChannelFullMode.Wait)
            )

        // Completing the writer here — rather than leaving the loop below to notice `ct` on its
        // own — is what makes TryPost/PostAsync fail the moment the caller cancels, instead of
        // racing the loop's own teardown.
        ct.Register(fun () -> channel.Writer.TryComplete() |> ignore) |> ignore

        let workerTask =
            task {
                try
                    let mutable running = true

                    while running && not ct.IsCancellationRequested do
                        match channel.Reader.TryRead() with
                        | true, item ->
                            try
                                do! handler item
                            with ex ->
                                try
                                    onError ex
                                with _ ->
                                    ()
                        | false, _ ->
                            let! canRead = channel.Reader.WaitToReadAsync(ct)
                            running <- canRead
                with :? System.OperationCanceledException ->
                    ()
            }

        BoundedHandle(channel, ct, workerTask)
