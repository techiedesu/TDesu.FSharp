namespace TDesu.FSharp.Concurrency

open System.Collections.Generic

/// <summary>
/// Bounded queue — auto-evicts oldest elements when capacity is reached.
/// Not thread-safe.
/// </summary>
/// <param name="capacity">The maximum number of items the queue can hold. Must be positive.</param>
[<Sealed>]
type BoundedQueue<'T>(capacity: int) =
    do
        if capacity <= 0 then
            invalidArg (nameof capacity) $"Must be positive, got %d{capacity}"

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
    member _.Clear() = queue.Clear()

    interface IEnumerable<'T> with
        member _.GetEnumerator() = queue.GetEnumerator()

    interface System.Collections.IEnumerable with
        member _.GetEnumerator() = queue.GetEnumerator()
