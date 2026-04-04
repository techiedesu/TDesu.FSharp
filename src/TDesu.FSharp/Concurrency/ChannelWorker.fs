namespace TDesu.FSharp.Concurrency

#if !FABLE_COMPILER

open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks

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
            try signal.Release() |> ignore
            with :? System.ObjectDisposedException -> ()

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
        let workerTask = task {
            try
                try
                    while not ct.IsCancellationRequested do
                        do! signal.WaitAsync(ct)
                        match queue.TryDequeue() with
                        | true, item ->
                            try
                                do! handler item
                            with ex ->
                                try onError ex with _ -> ()
                        | _ -> ()
                with :? System.OperationCanceledException -> ()
            finally
                signal.Dispose()
        }
        Handle(queue, signal, workerTask)

#endif
