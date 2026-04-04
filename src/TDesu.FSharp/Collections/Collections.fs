namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
#if !FABLE_COMPILER
open System.IO
#endif
open TDesu.FSharp
open TDesu.FSharp.Operators

[<RequireQualifiedAccess>]
module Dictionary =
    /// Gets the value for the given key; throws if key is missing.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline getValue key (d: #IDictionary<'TKey, 'TValue>) = d[key]

    /// Tries to get a value, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline tryGetValue key (d: #IDictionary<'TKey, 'TValue>) =
        d.TryGetValue key |> Option.ofCSharpTryPattern

    /// Tries to get a value, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline tryGetValueV key (d: #IDictionary<'TKey, 'TValue>) =
        d.TryGetValue key |> ValueOption.ofCSharpTryPattern

    /// Get value or default — replaces <c>match d.TryGetValue(k) with true, v -> v | _ -> def</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline getOrDefault key defaultValue (d: #IDictionary<'TKey, 'TValue>) =
        match d.TryGetValue key with
        | true, v -> v
        | false, _ -> defaultValue

module Stack =
    /// Returns the top element as Some, or None if the stack is empty.
    /// <param name="stack">The stack to peek into.</param>
    let inline tryPeek (stack: Stack<'T>) =
        let x, y = stack.TryPeek()
        if x then
            Some y
        else
            None

    /// Removes and returns the top element from the stack.
    /// <param name="stack">The stack to pop from.</param>
    let inline pop (stack: Stack<'T>) =
        stack.Pop()

    /// Pushes an item onto the top of the stack.
    /// <param name="item">The item to push.</param>
    /// <param name="stack">The stack to push onto.</param>
    let inline push (item: 'T) (stack: Stack<'T>) =
        stack.Push(item)

    /// Returns a new stack with elements in reverse order.
    /// <param name="stack">The stack to reverse.</param>
    let reverse (stack: Stack<'T>) =
        let newStack = Stack<'T>()
        for item in stack do
            newStack.Push(item)
        newStack

[<RequireQualifiedAccess>]
module Seq =
    /// <summary>
    /// Returns <c>Some(max)</c> or <c>None</c> for empty sequences.
    /// </summary>
    /// <param name="source">The input sequence.</param>
    let tryMax (source: 'T seq) : 'T option =
        use e = source.GetEnumerator()
        if not (e.MoveNext()) then
            None
        else
            let mutable best = e.Current
            while e.MoveNext() do
                if e.Current > best then
                    best <- e.Current
            Some best

    /// <summary>
    /// Returns <c>Some(min)</c> or <c>None</c> for empty sequences.
    /// </summary>
    /// <param name="source">The input sequence.</param>
    let tryMin (source: 'T seq) : 'T option =
        use e = source.GetEnumerator()
        if not (e.MoveNext()) then None
        else
            let mutable best = e.Current
            while e.MoveNext() do
                if e.Current < best then
                    best <- e.Current
            Some best

    /// Returns Some(average) or None for empty sequences of floats.
    /// <param name="source">The input sequence of floats.</param>
    let tryAverage (source: float seq) : float option =
        use e = source.GetEnumerator()
        if e.MoveNext() then
            let mutable sum = e.Current
            let mutable count = 1
            while e.MoveNext() do
                sum <- sum + e.Current
                count <- count + 1
            Some(sum / float count)
        else
            None

#if !FABLE_COMPILER
[<RequireQualifiedAccess>]
module Array =
    /// Converts a <see cref="System.IO.MemoryStream"/> to a byte array.
    /// <param name="memoryStream">The memory stream to convert.</param>
    let inline ofMemoryStream (memoryStream: MemoryStream) =
        memoryStream.ToArray()

[<RequireQualifiedAccess>]
module MemoryStream =
    /// Resets the stream position to the beginning.
    /// <param name="memoryStream">The memory stream to reset.</param>
    let inline reset (memoryStream: MemoryStream) =
        memoryStream.Position <- 0
#endif

module List =
    /// Converts a Stack to a list (top element first).
    /// <param name="stack">The stack to convert.</param>
    let inline ofStack (stack: Stack<'T>) =
        stack |> Seq.toList

    /// Returns Some(max) or None for empty lists.
    /// <param name="xs">The input list.</param>
    let tryMax (xs: 'T list) =
        match xs with [] -> None | _ -> Some(List.max xs)

    /// Returns Some(min) or None for empty lists.
    /// <param name="xs">The input list.</param>
    let tryMin (xs: 'T list) =
        match xs with [] -> None | _ -> Some(List.min xs)

/// <summary>
/// Functional operations on <see cref="System.Collections.Generic.List{T}"/> (ResizeArray).
/// </summary>
[<RequireQualifiedAccess>]
module ResizeArray =
    /// Creates an empty ResizeArray.
    let inline create<'T> () = ResizeArray<'T>()

    /// Creates a ResizeArray with the given initial capacity.
    /// <param name="capacity">The initial capacity.</param>
    let inline withCapacity<'T> (capacity: int) = ResizeArray<'T>(capacity)

    /// Creates a ResizeArray with a single element.
    /// <param name="item">The single element to include.</param>
    let inline singleton (item: 'T) =
        let ra = ResizeArray<'T>(1)
        ra.Add(item)
        ra

    /// Creates a ResizeArray from a sequence.
    /// <param name="source">The input sequence.</param>
    let inline ofSeq (source: 'T seq) = ResizeArray<'T>(source)

    /// Creates a ResizeArray from a list.
    /// <param name="source">The input list.</param>
    let inline ofList (source: 'T list) = ResizeArray<'T>(source :> _ seq)

    /// Creates a ResizeArray from an array.
    /// <param name="source">The input array.</param>
    let inline ofArray (source: 'T[]) = ResizeArray<'T>(source)

    /// Adds an item and returns the ResizeArray (pipeable).
    /// <param name="item">The item to add.</param>
    /// <param name="ra">The ResizeArray to add to.</param>
    let inline add (item: 'T) (ra: ResizeArray<'T>) =
        ra.Add(item)
        ra

    /// Adds multiple items and returns the ResizeArray (pipeable).
    /// <param name="items">The items to add.</param>
    /// <param name="ra">The ResizeArray to add to.</param>
    let inline addRange (items: 'T seq) (ra: ResizeArray<'T>) =
        ra.AddRange(items)
        ra

    /// Maps each element, returning a new ResizeArray.
    /// <param name="f">The mapping function.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline map ([<InlineIfLambda>] f: 'T -> 'TResult) (ra: ResizeArray<'T>) =
        let result = ResizeArray<'TResult>(ra.Count)
        for item in ra do
            result.Add(f item)
        result

    /// Filters elements, returning a new ResizeArray.
    /// <param name="f">The predicate to filter by.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline filter ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        let result = ResizeArray<'T>()
        for item in ra do
            if f item then result.Add(item)
        result

    /// Applies an action to each element.
    /// <param name="f">The action to apply to each element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline iter ([<InlineIfLambda>] f: 'T -> unit) (ra: ResizeArray<'T>) =
        for item in ra do f item

    /// Applies an action with index to each element.
    /// <param name="f">The action to apply, receiving index and element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline iteri ([<InlineIfLambda>] f: int -> 'T -> unit) (ra: ResizeArray<'T>) =
        for i = 0 to ra.Count - 1 do f i ra[i]

    /// Returns true if any element satisfies the predicate.
    /// <param name="f">The predicate to test each element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline exists ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        let mutable found = false
        let mutable i = 0
        while not found && i < ra.Count do
            if f ra[i] then found <- true
            i <- i + 1
        found

    /// Returns the first element matching the predicate, or None.
    /// <param name="f">The predicate to match against.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline tryFind ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        let mutable result = None
        let mutable i = 0
        while result.IsNone && i < ra.Count do
            if f ra[i] then result <- Some ra[i]
            i <- i + 1
        result

    /// Safe index access.
    /// <param name="index">The zero-based index to access.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline tryItem (index: int) (ra: ResizeArray<'T>) =
        if index >= 0 && index < ra.Count then Some ra[index]
        else None

    /// Converts to an F# list.
    /// <param name="ra">The ResizeArray to convert.</param>
    let inline toList (ra: ResizeArray<'T>) = Seq.toList ra

    /// Converts to an array.
    /// <param name="ra">The ResizeArray to convert.</param>
    let inline toArray (ra: ResizeArray<'T>) = ra.ToArray()

    /// Returns the number of elements.
    /// <param name="ra">The ResizeArray to count.</param>
    let inline count (ra: ResizeArray<'T>) = ra.Count

    /// Returns true if the ResizeArray is empty.
    /// <param name="ra">The ResizeArray to check.</param>
    let inline isEmpty (ra: ResizeArray<'T>) = ra.Count = 0

    /// Sorts the ResizeArray in-place and returns it (pipeable).
    /// <param name="ra">The ResizeArray to sort.</param>
    let inline sort (ra: ResizeArray<'T>) =
        ra.Sort()
        ra

    /// Sorts with a comparison function in-place and returns it.
    /// <param name="comparer">The comparison function.</param>
    /// <param name="ra">The ResizeArray to sort.</param>
    let inline sortWith (comparer: 'T -> 'T -> int) (ra: ResizeArray<'T>) =
        ra.Sort(Comparison(comparer))
        ra

    /// Sorts by a key projection in-place and returns it.
    /// <param name="projection">The function to extract a comparison key.</param>
    /// <param name="ra">The ResizeArray to sort.</param>
    let inline sortBy ([<InlineIfLambda>] projection: 'T -> 'Key) (ra: ResizeArray<'T>) =
        ra.Sort(fun a b -> compare (projection a) (projection b))
        ra

    /// Removes all elements matching the predicate. Returns the ResizeArray.
    /// <param name="f">The predicate identifying elements to remove.</param>
    /// <param name="ra">The ResizeArray to remove from.</param>
    let inline removeWhere ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        %ra.RemoveAll(Predicate(f))
        ra

    /// Clears all elements. Returns the ResizeArray.
    /// <param name="ra">The ResizeArray to clear.</param>
    let inline clear (ra: ResizeArray<'T>) =
        ra.Clear()
        ra

    /// Folds over the elements from left to right.
    /// <param name="f">The accumulator function.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline fold ([<InlineIfLambda>] f: 'State -> 'T -> 'State) (state: 'State) (ra: ResizeArray<'T>) =
        let mutable acc = state
        for item in ra do
            acc <- f acc item
        acc

    /// Joins elements as strings with a separator.
    /// <param name="separator">The separator string.</param>
    /// <param name="ra">The ResizeArray of strings to join.</param>
    let inline joinWith (separator: string) (ra: ResizeArray<string>) =
        String.Join(separator, ra)
