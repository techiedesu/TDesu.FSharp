namespace TDesu.FSharp.Concurrency

open System.Collections.Generic

/// <summary>
/// Bounded dictionary — auto-evicts the first-inserted key when capacity is reached.
/// Not thread-safe.
/// </summary>
/// <param name="capacity">The maximum number of entries the dictionary can hold. Must be positive.</param>
[<Sealed>]
type BoundedDict<'TKey, 'TValue when 'TKey: equality>(capacity: int) =
    do
        if capacity <= 0 then
            invalidArg (nameof capacity) $"Must be positive, got %d{capacity}"

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
    member _.Item
        with get key = dict[key]

    /// Returns true if the key exists.
    /// <param name="key">The key to check for.</param>
    member _.ContainsKey(key: 'TKey) = dict.ContainsKey(key)

    /// Number of items.
    member _.Count = dict.Count

    /// Removes a key. Returns true if removed.
    /// <param name="key">The key to remove.</param>
    member _.Remove(key: 'TKey) =
        if dict.Remove(key) then
            match nodeMap.TryGetValue(key) with
            | true, node ->
                order.Remove(node)
                nodeMap.Remove(key) |> ignore
            | _ -> ()

            true
        else
            false

    /// Clears all items.
    member _.Clear() =
        dict.Clear()
        order.Clear()
        nodeMap.Clear()
