namespace TDesu.FSharp.Concurrency

open System
open System.Collections.Generic
open System.Threading.Tasks
open TDesu.FSharp.IO

#if !FABLE_COMPILER
/// <summary>
/// CancellationToken helpers — reduces boilerplate for timeout + linked patterns.
/// </summary>
[<RequireQualifiedAccess>]
module CancellationToken =
    open System.Threading

    /// Creates a CTS that cancels after the given timeout. Use with <c>use</c>.
    /// <param name="timeout">The duration after which cancellation is requested.</param>
    let inline withTimeout (timeout: TimeSpan) =
        new CancellationTokenSource(timeout)

    /// <summary>
    /// Creates a linked CTS: cancels when parent is canceled OR after timeout.
    /// Disposes both internal CTS on dispose. Use with <c>use</c>.
    /// </summary>
    /// <returns>A tuple of the linked CTS and a disposable that cleans up both.</returns>
    /// <param name="timeout">The duration after which cancellation is requested.</param>
    /// <param name="parent">The parent token to link with the timeout.</param>
    let linked (timeout: TimeSpan) (parent: CancellationToken) =
        let timeoutCts = new CancellationTokenSource(timeout)
        let linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parent, timeoutCts.Token)
        linkedCts, Disposable.create (fun () -> linkedCts.Dispose(); timeoutCts.Dispose())

    /// Creates a linked CTS from a parent token (no timeout).
    /// <param name="parent">The parent token to link from.</param>
    let inline linkedFrom (parent: CancellationToken) =
        CancellationTokenSource.CreateLinkedTokenSource(parent)
#endif

#if FABLE_COMPILER
/// Atomic counter — simple mutable int (JS is single-threaded).
/// <param name="initial">The initial counter value.</param>
[<Sealed>]
type AtomicInt(initial: int) =
    let mutable value = initial
    new() = AtomicInt(0)
    member _.Value = value
    member _.Increment() = value <- value + 1; value
    member _.Decrement() = value <- value - 1; value
    /// <param name="delta">The value to add to the counter.</param>
    member _.Add(delta: int) = value <- value + delta; value
    /// <param name="newValue">The value to set.</param>
    member _.Exchange(newValue: int) = let old = value in value <- newValue; old
    /// <param name="newValue">The value to set if the current value matches the comparand.</param>
    /// <param name="comparand">The value to compare against.</param>
    member _.CompareExchange(newValue: int, comparand: int) =
        if value = comparand then value <- newValue; true else false
    member this.Reset() = this.Exchange(0)
    override _.ToString() = string value

/// Atomic int64 counter — simple mutable int64 (JS is single-threaded).
/// <param name="initial">The initial counter value.</param>
[<Sealed>]
type AtomicInt64(initial: int64) =
    let mutable value = initial
    new() = AtomicInt64(0L)
    member _.Value = value
    member _.Increment() = value <- value + 1L; value
    member _.Decrement() = value <- value - 1L; value
    /// <param name="delta">The value to add to the counter.</param>
    member _.Add(delta: int64) = value <- value + delta; value
    /// <param name="newValue">The value to set.</param>
    member _.Exchange(newValue: int64) = let old = value in value <- newValue; old
    member _.Reset() = let old = value in value <- 0L; old
    override _.ToString() = string value

/// One-shot async signal. Wraps TaskCompletionSource for idiomatic F# async coordination.
/// Fable version — single-threaded, no RunContinuationsAsynchronously needed.
[<Sealed>]
type Signal() =
    let tcs = TaskCompletionSource<unit>()
    /// Completes the signal, releasing all waiters. Idempotent.
    member _.Set() = tcs.TrySetResult() |> ignore
    /// Returns a task that completes when the signal is set.
    member _.Wait() : Task = tcs.Task
    /// Returns a task that completes when the signal is set, with a timeout.
    /// Returns true if signaled, false if timed out.
    /// <param name="timeout">The maximum duration to wait for the signal.</param>
    member _.Wait(timeout: TimeSpan) : Task<bool> = task {
        if tcs.Task.IsCompleted then return true
        else
            let! completed = Task.WhenAny(tcs.Task, Task.Delay timeout)
            return obj.ReferenceEquals(completed, tcs.Task)
    }
    /// Whether the signal has been set.
    member _.IsSet = tcs.Task.IsCompleted
#else
/// <summary>
/// Thread-safe atomic counter using <see cref="System.Threading.Interlocked"/>. Zero-allocation reads.
/// </summary>
/// <param name="initial">The initial counter value.</param>
[<Sealed>]
type AtomicInt(initial: int) =
    let mutable value = initial
    new() = AtomicInt(0)

    /// Gets the current value.
    member _.Value =
        System.Threading.Volatile.Read(&value)

    /// Increments and returns the new value.
    member _.Increment() =
        System.Threading.Interlocked.Increment(&value)

    /// Decrements and returns the new value.
    member _.Decrement() = System.Threading.Interlocked.Decrement(&value)

    /// Adds delta and returns the new value.
    /// <param name="delta">The value to add to the counter.</param>
    member _.Add(delta: int) =
        System.Threading.Interlocked.Add(&value, delta)

    /// Sets to newValue, returns the old value.
    /// <param name="newValue">The value to set.</param>
    member _.Exchange(newValue: int) =
        System.Threading.Interlocked.Exchange(&value, newValue)

    /// Sets to newValue if current equals comparand. Returns true if exchanged.
    /// <param name="newValue">The value to set if the current value matches the comparand.</param>
    /// <param name="comparand">The value to compare against.</param>
    member _.CompareExchange(newValue: int, comparand: int) =
        System.Threading.Interlocked.CompareExchange(&value, newValue, comparand) = comparand

    /// Resets to 0 and returns previous value.
    member this.Reset() =
        this.Exchange(0)

    override _.ToString() = string value

/// Thread-safe atomic int64 counter.
/// <param name="initial">The initial counter value.</param>
[<Sealed>]
type AtomicInt64(initial: int64) =
    let mutable value = initial
    new() = AtomicInt64(0L)
    member _.Value = System.Threading.Volatile.Read(&value)
    member _.Increment() = System.Threading.Interlocked.Increment(&value)
    member _.Decrement() = System.Threading.Interlocked.Decrement(&value)
    member _.Add(delta: int64) = System.Threading.Interlocked.Add(&value, delta)
    member _.Exchange(newValue: int64) = System.Threading.Interlocked.Exchange(&value, newValue)
    member _.Reset() = System.Threading.Interlocked.Exchange(&value, 0L)
    override _.ToString() = string value

/// <summary>
/// One-shot async signal. Wraps <see cref="TaskCompletionSource{T}"/> for idiomatic F# async coordination.
/// </summary>
/// <remarks>
/// Thread-safe — uses <c>RunContinuationsAsynchronously</c> to avoid inline continuations.
/// </remarks>
[<Sealed>]
type Signal() =
    let tcs = TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)
    /// Completes the signal, releasing all waiters. Idempotent.
    member _.Set() = tcs.TrySetResult() |> ignore
    /// Returns a task that completes when the signal is set.
    member _.Wait() : Task = tcs.Task
    /// Returns a task that completes when the signal is set, with a timeout.
    /// Returns true if signaled, false if timed out.
    /// <param name="timeout">The maximum duration to wait for the signal.</param>
    member _.Wait(timeout: TimeSpan) : Task<bool> = task {
        if tcs.Task.IsCompleted then return true
        else
            use delayCts = new System.Threading.CancellationTokenSource()
            let! completed = Task.WhenAny(tcs.Task, Task.Delay(timeout, delayCts.Token))
            let signaled = obj.ReferenceEquals(completed, tcs.Task)
            if signaled then delayCts.Cancel()
            return signaled
    }
    /// Whether the signal has been set.
    member _.IsSet = tcs.Task.IsCompleted
#endif

/// <summary>
/// Bounded queue — auto-evicts oldest elements when capacity is reached.
/// Not thread-safe.
/// </summary>
/// <param name="capacity">The maximum number of items the queue can hold. Must be positive.</param>
[<Sealed>]
type BoundedQueue<'T>(capacity: int) =
    do if capacity <= 0 then invalidArg (nameof capacity) $"Must be positive, got %d{capacity}"
    let queue = Queue<'T>(capacity)

    /// Enqueues an item. If at capacity, dequeues the oldest first.
    /// <param name="item">The item to enqueue.</param>
    member _.Enqueue(item: 'T) =
        if queue.Count >= capacity then
            queue.Dequeue() |> ignore
        queue.Enqueue(item)

    /// Number of items in the queue.
    member _.Count = queue.Count

    /// Dequeues the oldest item.
    member _.Dequeue() = queue.Dequeue()

    /// Peeks at the oldest item without removing.
    member _.Peek() = queue.Peek()

    /// Tries to dequeue, returns true + value if successful.
    /// <param name="result">When successful, receives the dequeued item.</param>
    member _.TryDequeue([<System.Runtime.InteropServices.Out>] result: 'T byref) =
        if queue.Count > 0 then
            result <- queue.Dequeue()
            true
        else
            false

    /// Returns all items as a sequence.
    member _.ToSeq() = queue :> 'T seq

    /// Clears all items.
    member _.Clear() =
        queue.Clear()

    interface IEnumerable<'T> with
        member _.GetEnumerator() =
            queue.GetEnumerator()

    interface System.Collections.IEnumerable with
        member _.GetEnumerator() =
            queue.GetEnumerator()

/// <summary>
/// Bounded dictionary — auto-evicts the first-inserted key when capacity is reached.
/// Not thread-safe.
/// </summary>
/// <param name="capacity">The maximum number of entries the dictionary can hold. Must be positive.</param>
[<Sealed>]
type BoundedDict<'TKey, 'TValue when 'TKey: equality>(capacity: int) =
    do if capacity <= 0 then invalidArg (nameof capacity) $"Must be positive, got %d{capacity}"
    let dict = Dictionary<'TKey, 'TValue>(capacity)
    let order = LinkedList<'TKey>()
    let nodeMap = Dictionary<'TKey, LinkedListNode<'TKey>>(capacity)

    /// Adds or updates a key. Evicts the oldest if at capacity.
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to associate with the key.</param>
    member _.Set(key: 'TKey, value: 'TValue) =
        if not (dict.ContainsKey key) then
            while order.Count >= capacity do
                let oldest = order.First.Value
                order.RemoveFirst()
                nodeMap.Remove(oldest) |> ignore
                dict.Remove(oldest) |> ignore
            let node = order.AddLast(key)
            nodeMap[key] <- node
        dict[key] <- value

    /// Tries to get a value by key.
    /// <param name="key">The key to look up.</param>
    member _.TryGet(key: 'TKey) =
        match dict.TryGetValue(key) with
        | true, v -> Some v
        | _ -> None

    /// Gets a value by key, throws if not found.
    /// <param name="key">The key to retrieve.</param>
    member _.Item with get key = dict[key]

    /// Returns true if the key exists.
    /// <param name="key">The key to check for.</param>
    member _.ContainsKey(key: 'TKey) =
        dict.ContainsKey(key)

    /// Number of items.
    member _.Count = dict.Count

    /// Removes a key. Returns true if removed.
    /// <param name="key">The key to remove.</param>
    member _.Remove(key: 'TKey) =
        if dict.Remove(key) then
            match nodeMap.TryGetValue(key) with
            | true, node -> order.Remove(node); nodeMap.Remove(key) |> ignore
            | _ -> ()
            true
        else false

    /// Clears all items.
    member _.Clear() =
        dict.Clear()
        order.Clear()
        nodeMap.Clear()

/// <summary>
/// Periodic background timer — runs an action at fixed intervals with cancellation.
/// </summary>
[<RequireQualifiedAccess>]
module PeriodicTimer =
#if FABLE_COMPILER
    /// Starts a background loop that runs action every interval.
    /// <param name="interval">The delay between each tick.</param>
    /// <param name="action">The async action to execute on each tick.</param>
    /// <param name="_ct">Cancellation token (unused in Fable).</param>
    let start (interval: TimeSpan) (action: unit -> Task<unit>) (_ct: Threading.CancellationToken) =
        let t = task {
            while true do
                try
                    do! Task.Delay(int interval.TotalMilliseconds)
                    do! action ()
                with _ -> ()
        }
        t :> Task

    /// Like start, but action receives a counter (0-based) for each tick.
    /// <param name="interval">The delay between each tick.</param>
    /// <param name="action">The async action to execute, receiving the current tick index.</param>
    /// <param name="_ct">Cancellation token (unused in Fable).</param>
    let startCounted (interval: TimeSpan) (action: int -> Task<unit>) (_ct: Threading.CancellationToken) =
        let mutable tick = 0
        let t = task {
            while true do
                try
                    do! Task.Delay(int interval.TotalMilliseconds)
                    do! action tick
                    tick <- tick + 1
                with _ -> ()
        }
        t :> Task
#else
    /// Starts a background loop that runs action every interval.
    /// <param name="interval">The delay between each tick.</param>
    /// <param name="action">The async action to execute on each tick.</param>
    /// <param name="ct">The cancellation token to stop the loop.</param>
    /// <param name="onError">Handler invoked when the action throws a non-cancellation exception.</param>
    let start (interval: TimeSpan) (action: unit -> Task<unit>) (ct: Threading.CancellationToken) (onError: exn -> unit) =
        let t = task {
            while not ct.IsCancellationRequested do
                try
                    do! Task.Delay(interval, ct)
                    do! action ()
                with
                | :? OperationCanceledException -> ()
                | ex -> try onError ex with _ -> ()
        }
        t :> Task

    /// Like start, but action receives a counter (0-based) for each tick.
    /// <param name="interval">The delay between each tick.</param>
    /// <param name="action">The async action to execute, receiving the current tick index.</param>
    /// <param name="ct">The cancellation token to stop the loop.</param>
    /// <param name="onError">Handler invoked when the action throws a non-cancellation exception.</param>
    let startCounted (interval: TimeSpan) (action: int -> Task<unit>) (ct: Threading.CancellationToken) (onError: exn -> unit) =
        let mutable tick = 0
        let t = task {
            while not ct.IsCancellationRequested do
                try
                    do! Task.Delay(interval, ct)
                    do! action tick
                    tick <- tick + 1
                with
                | :? OperationCanceledException -> ()
                | ex -> try onError ex with _ -> ()
        }
        t :> Task
#endif

/// <summary>
/// Snapshot throttle — tracks message count and triggers save at threshold (thread-safe).
/// </summary>
/// <param name="threshold">The number of messages before a snapshot is triggered. Must be positive.</param>
[<Sealed>]
type SnapshotThrottle(threshold: int) =
    do if threshold <= 0 then invalidArg (nameof threshold) $"Must be positive, got %d{threshold}"
    let mutable counter = 0

    /// <summary>
    /// Records a message. Returns <c>true</c> if threshold reached (caller should save snapshot).
    /// </summary>
    /// <remarks>Thread-safe: uses atomic increment. Under contention, may trigger slightly past threshold.</remarks>
    member _.Record() =
#if FABLE_COMPILER
        counter <- counter + 1
        if counter >= threshold then
            counter <- 0
            true
        else
            false
#else
        let newVal = Threading.Interlocked.Increment(&counter)
        if newVal >= threshold then
            // CAS: only the first thread past the threshold resets and triggers
            Threading.Interlocked.CompareExchange(&counter, 0, newVal) = newVal
        else
            false
#endif

    /// Resets the counter.
    member _.Reset() =
#if FABLE_COMPILER
        counter <- 0
#else
        Threading.Interlocked.Exchange(&counter, 0) |> ignore
#endif

    /// Current count since last snapshot.
    member _.Count =
#if FABLE_COMPILER
        counter
#else
        Threading.Volatile.Read(&counter)
#endif
