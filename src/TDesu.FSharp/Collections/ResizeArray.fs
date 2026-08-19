namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

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
        if isNull source then
            ResizeArray<'T>()
        else
            ResizeArray<'T>(source)

    /// Creates a ResizeArray from a list. A null <paramref name="source"/> is treated as empty (F# lists
    /// represent <c>[]</c> this way when handed a raw null reference from a non-F# caller).
    /// <param name="source">The input list.</param>
    let inline ofList (source: 'T list) =
        if isNotNullRef source then
            ResizeArray<'T>(source :> _ seq)
        else
            ResizeArray<'T>()

    /// Creates a ResizeArray from an array. A null <paramref name="source"/> is treated as empty.
    /// <param name="source">The input array.</param>
    let inline ofArray (source: 'T[]) =
        if isNull source then
            ResizeArray<'T>()
        else
            ResizeArray<'T>(source)

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

        if not (isNull items) then
            ra.AddRange(items)

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
                if f item then
                    result.Add(item)

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
            for item in ra do
                f item

    /// Applies an action with index to each element. A null <paramref name="ra"/> is treated as empty
    /// (no-op).
    /// <param name="f">The action to apply, receiving index and element.</param>
    /// <param name="ra">The input ResizeArray.</param>
    let inline iteri ([<InlineIfLambda>] f: int -> 'T -> unit) (ra: ResizeArray<'T>) =
        if not (isNull ra) then
            for i = 0 to ra.Count - 1 do
                f i ra[i]

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
                if f ra[i] then
                    found <- true

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
                if not (f ra[i]) then
                    allTrue <- false

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
                if f ra[i] then
                    result <- Some ra[i]

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
                if f ra[i] then
                    result <- Some i

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
    let inline toArray (ra: ResizeArray<'T>) =
        if isNull ra then [||] else ra.ToArray()

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
