namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
#if !FABLE_COMPILER
open System.IO
#endif
open TDesu.FSharp
open TDesu.FSharp.Operators

/// <namespacedoc>
///   <summary>Collection extensions: Dictionary, ResizeArray, Seq, List, Stack helpers.</summary>
/// </namespacedoc>
[<RequireQualifiedAccess>]
module Dictionary =
    /// Gets the value for the given key; throws if key is missing.
    /// A null <paramref name="d"/> is treated as an empty dictionary, so a missing key still raises
    /// <see cref="System.Collections.Generic.KeyNotFoundException"/> rather than a null-reference error.
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">When the key is not present, including when <paramref name="d"/> is null.</exception>
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline getValue key (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then
            raise (KeyNotFoundException($"The given key '%O{key}' was not present in the dictionary."))
        else
            d[key]

    /// Tries to get a value, returning <c>Some(value)</c> or <c>None</c>. A null <paramref name="d"/> is
    /// treated as an empty dictionary and also returns <c>None</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline tryGetValue key (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then None
        else d.TryGetValue key |> Option.ofCSharpTryPattern

    /// Tries to get a value, returning <c>ValueSome(value)</c> or <c>ValueNone</c>. A null
    /// <paramref name="d"/> is treated as an empty dictionary and also returns <c>ValueNone</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline tryGetValueV key (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then ValueNone
        else d.TryGetValue key |> ValueOption.ofCSharpTryPattern

    /// Get value or default — replaces <c>match d.TryGetValue(k) with true, v -> v | _ -> def</c>.
    /// A null <paramref name="d"/> is treated as an empty dictionary and also returns
    /// <paramref name="defaultValue"/>.
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline getOrDefault key defaultValue (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then
            defaultValue
        else
            match d.TryGetValue key with
            | true, v -> v
            | false, _ -> defaultValue

module Stack =
    /// Returns the top element as Some, or None if the stack is empty or null.
    /// <param name="stack">The stack to peek into.</param>
    let inline tryPeek (stack: Stack<'T>) =
        if isNull stack then
            None
        else
            let x, y = stack.TryPeek()
            if x then
                Some y
            else
                None

    /// Removes and returns the top element from the stack.
    /// A null <paramref name="stack"/> is treated as an empty stack, so it raises the same
    /// <see cref="System.InvalidOperationException"/> a real empty stack would, not a null-reference error.
    /// <exception cref="System.InvalidOperationException">When the stack is empty, including when <paramref name="stack"/> is null.</exception>
    /// <param name="stack">The stack to pop from.</param>
    let inline pop (stack: Stack<'T>) =
        match stack with
        | null ->
            raise (invalidOp "Stack empty.")
        | _ ->
            stack.Pop()

    /// Pushes an item onto the top of the stack.
    /// <paramref name="stack"/> is the mutation target: unlike the read-only helpers in this module, a
    /// null <paramref name="stack"/> has no empty instance to push onto, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="stack"/> is null.</exception>
    /// <param name="item">The item to push.</param>
    /// <param name="stack">The stack to push onto.</param>
    let inline push (item: 'T) (stack: Stack<'T>) =
        Guard.notNull "stack" stack
        stack.Push(item)

    /// Returns a new stack with elements in reverse order. A null <paramref name="stack"/> is treated as
    /// empty and yields a new empty stack.
    /// <param name="stack">The stack to reverse.</param>
    let reverse (stack: Stack<'T>) =
        let newStack = Stack<'T>()
        match stack with
        | null ->
            newStack
        | _ ->
            for item in stack do
                newStack.Push(item)
            newStack

[<RequireQualifiedAccess>]
module Seq =
    /// <summary>
    /// Returns <c>Some(max)</c>, or <c>None</c> for an empty or null sequence.
    /// </summary>
    /// <param name="source">The input sequence.</param>
    let tryMax (source: 'T seq) : 'T option =
        if isNull source then
            None
        else
            use e = source.GetEnumerator()
            if e.MoveNext() then
                let mutable best = e.Current
                while e.MoveNext() do
                    if e.Current > best then
                        best <- e.Current
                Some best
            else
                None

    /// <summary>
    /// Returns <c>Some(min)</c>, or <c>None</c> for an empty or null sequence.
    /// </summary>
    /// <param name="source">The input sequence.</param>
    let tryMin (source: 'T seq) : 'T option =
        if isNull source then
            None
        else
            use e = source.GetEnumerator()
            if e.MoveNext() then
                let mutable best = e.Current
                while e.MoveNext() do
                    if e.Current < best then
                        best <- e.Current
                Some best
            else
                None

    /// <summary>
    /// Returns <c>Some(element)</c> with the greatest key returned by <paramref name="projection"/>, or
    /// <c>None</c> for an empty or null sequence. Enumerates <paramref name="source"/> exactly once; when
    /// several elements share the greatest key, the first one encountered is returned.
    /// </summary>
    /// <param name="projection">The function used to extract a comparable key from each element.</param>
    /// <param name="source">The input sequence.</param>
    let tryMaxBy (projection: 'T -> 'Key) (source: 'T seq) : 'T option =
        if isNull source then
            None
        else
            use e = source.GetEnumerator()
            if e.MoveNext() then
                let mutable bestItem = e.Current
                let mutable bestKey = projection bestItem
                while e.MoveNext() do
                    let key = projection e.Current
                    if key > bestKey then
                        bestItem <- e.Current
                        bestKey <- key
                Some bestItem
            else
                None

    /// <summary>
    /// Returns <c>Some(element)</c> with the smallest key returned by <paramref name="projection"/>, or
    /// <c>None</c> for an empty or null sequence. Enumerates <paramref name="source"/> exactly once; when
    /// several elements share the smallest key, the first one encountered is returned.
    /// </summary>
    /// <param name="projection">The function used to extract a comparable key from each element.</param>
    /// <param name="source">The input sequence.</param>
    let tryMinBy (projection: 'T -> 'Key) (source: 'T seq) : 'T option =
        if isNull source then
            None
        else
            use e = source.GetEnumerator()
            if not (e.MoveNext()) then
                None
            else
                let mutable bestItem = e.Current
                let mutable bestKey = projection bestItem
                while e.MoveNext() do
                    let key = projection e.Current
                    if key < bestKey then
                        bestItem <- e.Current
                        bestKey <- key
                Some bestItem

    /// Returns Some(average) or None for empty or null sequences of floats.
    /// <param name="source">The input sequence of floats.</param>
    let tryAverage (source: float seq) : float option =
        if isNull source then
            None
        else
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
    /// Converts a <see cref="System.IO.MemoryStream"/> to a byte array. A null
    /// <paramref name="memoryStream"/> is treated as empty and yields <c>[||]</c>.
    /// <param name="memoryStream">The memory stream to convert.</param>
    let inline ofMemoryStream (memoryStream: MemoryStream) =
        if isNull memoryStream then [||] else memoryStream.ToArray()

[<RequireQualifiedAccess>]
module MemoryStream =
    /// Resets the stream position to the beginning.
    /// <paramref name="memoryStream"/> is the mutation target: a null <paramref name="memoryStream"/> has
    /// no instance whose position can be reset, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="memoryStream"/> is null.</exception>
    /// <param name="memoryStream">The memory stream to reset.</param>
    let inline reset (memoryStream: MemoryStream) =
        Guard.notNull "memoryStream" memoryStream
        memoryStream.Position <- 0
#endif

module List =
    /// Converts a Stack to a list (top element first). A null <paramref name="stack"/> is treated as
    /// empty and yields <c>[]</c>.
    /// <param name="stack">The stack to convert.</param>
    let inline ofStack (stack: Stack<'T>) =
        if isNull stack then [] else stack |> Seq.toList

    /// Returns Some(max) or None for empty or null lists.
    /// <param name="xs">The input list.</param>
    let tryMax (xs: 'T list) =
        if isNotNullRef xs then
            match xs with
            | [] -> None
            | _ -> Some(List.max xs)
        else
            None

    /// Returns Some(min) or None for empty or null lists.
    /// <param name="xs">The input list.</param>
    let tryMin (xs: 'T list) =
        if isNotNullRef xs then
            match xs with
            | [] -> None
            | _ -> Some(List.min xs)
        else
            None

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

    /// Creates a ResizeArray from a sequence. A null <paramref name="source"/> is treated as empty.
    /// <param name="source">The input sequence.</param>
    let inline ofSeq (source: 'T seq) =
        if isNull source then ResizeArray<'T>() else ResizeArray<'T>(source)

    /// Creates a ResizeArray from a list. A null <paramref name="source"/> is treated as empty (F# lists
    /// represent <c>[]</c> this way when handed a raw null reference from a non-F# caller).
    /// <param name="source">The input list.</param>
    let inline ofList (source: 'T list) =
        if isNotNullRef source then ResizeArray<'T>(source :> _ seq) else ResizeArray<'T>()

    /// Creates a ResizeArray from an array. A null <paramref name="source"/> is treated as empty.
    /// <param name="source">The input array.</param>
    let inline ofArray (source: 'T[]) =
        if isNull source then ResizeArray<'T>() else ResizeArray<'T>(source)

    /// Adds an item and returns the ResizeArray (pipeable).
    /// <paramref name="ra"/> is the mutation target: unlike the read-only helpers in this module, a null
    /// <paramref name="ra"/> has no empty instance to add to, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="item">The item to add.</param>
    /// <param name="ra">The ResizeArray to add to.</param>
    let inline add (item: 'T) (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        ra.Add(item)
        ra

    /// Adds multiple items and returns the ResizeArray (pipeable). A null <paramref name="items"/> source
    /// is treated as empty (nothing is added). <paramref name="ra"/> is the mutation target, so — unlike a
    /// null source — a null <paramref name="ra"/> has no empty instance to add to and is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="items">The items to add.</param>
    /// <param name="ra">The ResizeArray to add to.</param>
    let inline addRange (items: 'T seq) (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        if not (isNull items) then ra.AddRange(items)
        ra

    /// Maps each element, returning a new ResizeArray. A null <paramref name="ra"/> is treated as empty
    /// and yields an empty result.
    /// <param name="f">The mapping function.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline map ([<InlineIfLambda>] f: 'T -> 'TResult) (ra: ResizeArray<'T>) =
        if isNull ra then
            ResizeArray<'TResult>()
        else
            let result = ResizeArray<'TResult>(ra.Count)
            for item in ra do
                result.Add(f item)
            result

    /// Maps each element together with its index, returning a new ResizeArray. A null <paramref name="ra"/>
    /// is treated as empty and yields an empty result.
    /// <param name="f">The mapping function, receiving the index and the element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline mapi ([<InlineIfLambda>] f: int -> 'T -> 'TResult) (ra: ResizeArray<'T>) =
        if isNull ra then
            ResizeArray<'TResult>()
        else
            let result = ResizeArray<'TResult>(ra.Count)
            for i = 0 to ra.Count - 1 do
                result.Add(f i ra[i])
            result

    /// Filters elements, returning a new ResizeArray. A null <paramref name="ra"/> is treated as empty
    /// and yields an empty result.
    /// <param name="f">The predicate to filter by.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline filter ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        let result = ResizeArray<'T>()
        if not (isNull ra) then
            for item in ra do
                if f item then result.Add(item)
        result

    /// Applies a function to each element, keeping the results that are <c>Some</c>. Returns a new
    /// ResizeArray. A null <paramref name="ra"/> is treated as empty and yields an empty result.
    /// <param name="f">The function to apply; an element is kept when it returns <c>Some</c>.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline choose ([<InlineIfLambda>] f: 'T -> 'TResult option) (ra: ResizeArray<'T>) =
        let result = ResizeArray<'TResult>()
        if not (isNull ra) then
            for item in ra do
                match f item with
                | Some v -> result.Add(v)
                | None -> ()
        result

    /// Splits into two new ResizeArrays: elements satisfying the predicate, then the rest. A null
    /// <paramref name="ra"/> is treated as empty and yields two empty ResizeArrays.
    /// <param name="f">The predicate to test each element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline partition ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        let matching = ResizeArray<'T>()
        let rest = ResizeArray<'T>()
        if not (isNull ra) then
            for item in ra do
                if f item then matching.Add(item) else rest.Add(item)
        matching, rest

    /// Applies an action to each element. A null <paramref name="ra"/> is treated as empty (no-op).
    /// <param name="f">The action to apply to each element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline iter ([<InlineIfLambda>] f: 'T -> unit) (ra: ResizeArray<'T>) =
        if not (isNull ra) then
            for item in ra do f item

    /// Applies an action with index to each element. A null <paramref name="ra"/> is treated as empty
    /// (no-op).
    /// <param name="f">The action to apply, receiving index and element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline iteri ([<InlineIfLambda>] f: int -> 'T -> unit) (ra: ResizeArray<'T>) =
        if not (isNull ra) then
            for i = 0 to ra.Count - 1 do f i ra[i]

    /// Returns true if any element satisfies the predicate. False for an empty or null ResizeArray.
    /// <param name="f">The predicate to test each element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline exists ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        if isNull ra then
            false
        else
            let mutable found = false
            let mutable i = 0
            while not found && i < ra.Count do
                if f ra[i] then found <- true
                i <- i + 1
            found

    /// Returns true if all elements satisfy the predicate. Vacuously true for an empty or null
    /// ResizeArray.
    /// <param name="f">The predicate to test each element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline forall ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        if isNull ra then
            true
        else
            let mutable allTrue = true
            let mutable i = 0
            while allTrue && i < ra.Count do
                if not (f ra[i]) then allTrue <- false
                i <- i + 1
            allTrue

    /// Returns the first element matching the predicate, or None. A null <paramref name="ra"/> is
    /// treated as empty and yields <c>None</c>.
    /// <param name="f">The predicate to match against.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline tryFind ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        if isNull ra then
            None
        else
            let mutable result = None
            let mutable i = 0
            while result.IsNone && i < ra.Count do
                if f ra[i] then result <- Some ra[i]
                i <- i + 1
            result

    /// Returns the index of the first element matching the predicate, or None. A null
    /// <paramref name="ra"/> is treated as empty and yields <c>None</c>.
    /// <param name="f">The predicate to match against.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline tryFindIndex ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        if isNull ra then
            None
        else
            let mutable result = None
            let mutable i = 0
            while result.IsNone && i < ra.Count do
                if f ra[i] then result <- Some i
                i <- i + 1
            result

    /// Safe index access. A null <paramref name="ra"/>, or an out-of-range <paramref name="index"/>,
    /// yields <c>None</c>.
    /// <param name="index">The zero-based index to access.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline tryItem (index: int) (ra: ResizeArray<'T>) =
        if isNull ra then None
        elif index >= 0 && index < ra.Count then Some ra[index]
        else None

    /// Converts to an F# list. A null <paramref name="ra"/> is treated as empty and yields <c>[]</c>.
    /// <param name="ra">The ResizeArray to convert.</param>
    let inline toList (ra: ResizeArray<'T>) = if isNull ra then [] else Seq.toList ra

    /// Converts to an array. A null <paramref name="ra"/> is treated as empty and yields <c>[||]</c>.
    /// <param name="ra">The ResizeArray to convert.</param>
    let inline toArray (ra: ResizeArray<'T>) = if isNull ra then [||] else ra.ToArray()

    /// Returns a new ResizeArray with elements in reverse order. A null <paramref name="ra"/> is treated
    /// as empty and yields an empty result.
    /// <param name="ra">The input ResizeArray.</param>
    let inline rev (ra: ResizeArray<'T>) =
        if isNull ra then
            ResizeArray<'T>()
        else
            let result = ResizeArray<'T>(ra.Count)
            for i = ra.Count - 1 downto 0 do
                result.Add(ra[i])
            result

    /// Returns the number of elements. A null <paramref name="ra"/> is treated as empty (0).
    /// <param name="ra">The ResizeArray to count.</param>
    let inline count (ra: ResizeArray<'T>) = if isNull ra then 0 else ra.Count

    /// Returns true if the ResizeArray is empty. A null <paramref name="ra"/> counts as empty.
    /// <param name="ra">The ResizeArray to check.</param>
    let inline isEmpty (ra: ResizeArray<'T>) = isNull ra || ra.Count = 0

    /// Sorts the ResizeArray in-place and returns it (pipeable).
    /// <paramref name="ra"/> is the mutation target: a null <paramref name="ra"/> has no empty instance
    /// to sort, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="ra">The ResizeArray to sort.</param>
    let inline sort (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        ra.Sort()
        ra

    /// Sorts with a comparison function in-place and returns it.
    /// <paramref name="ra"/> is the mutation target: a null <paramref name="ra"/> has no empty instance
    /// to sort, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="comparer">The comparison function.</param>
    /// <param name="ra">The ResizeArray to sort.</param>
    let inline sortWith (comparer: 'T -> 'T -> int) (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        ra.Sort(Comparison(comparer))
        ra

    /// Sorts by a key projection in-place and returns it.
    /// <paramref name="ra"/> is the mutation target: a null <paramref name="ra"/> has no empty instance
    /// to sort, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="projection">The function to extract a comparison key.</param>
    /// <param name="ra">The ResizeArray to sort.</param>
    let inline sortBy ([<InlineIfLambda>] projection: 'T -> 'Key) (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        ra.Sort(fun a b -> compare (projection a) (projection b))
        ra

    /// Removes all elements matching the predicate. Returns the ResizeArray.
    /// <paramref name="ra"/> is the mutation target: a null <paramref name="ra"/> has no empty instance
    /// to remove from, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="f">The predicate identifying elements to remove.</param>
    /// <param name="ra">The ResizeArray to remove from.</param>
    let inline removeWhere ([<InlineIfLambda>] f: 'T -> bool) (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        %ra.RemoveAll(Predicate(f))
        ra

    /// Clears all elements. Returns the ResizeArray.
    /// <paramref name="ra"/> is the mutation target: a null <paramref name="ra"/> has no empty instance
    /// to clear, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="ra"/> is null.</exception>
    /// <param name="ra">The ResizeArray to clear.</param>
    let inline clear (ra: ResizeArray<'T>) =
        Guard.notNull "ra" ra
        ra.Clear()
        ra

    /// Folds over the elements from left to right. A null <paramref name="ra"/> is treated as empty and
    /// yields <paramref name="state"/> unchanged.
    /// <param name="f">The accumulator function.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline fold ([<InlineIfLambda>] f: 'State -> 'T -> 'State) (state: 'State) (ra: ResizeArray<'T>) =
        if isNull ra then
            state
        else
            let mutable acc = state
            for item in ra do
                acc <- f acc item
            acc

    /// Joins elements as strings with a separator. A null <paramref name="ra"/> is treated as empty and
    /// yields <c>""</c>.
    /// <param name="separator">The separator string.</param>
    /// <param name="ra">The ResizeArray of strings to join.</param>
    let inline joinWith (separator: string) (ra: ResizeArray<string>) =
        if isNull ra then "" else String.Join(separator, ra)
