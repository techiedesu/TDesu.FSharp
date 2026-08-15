namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

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

    /// <summary>
    /// Converts a sequence to a <see cref="System.Collections.Generic.List{T}"/> (ResizeArray) in a
    /// single enumeration pass. When <paramref name="source"/> implements
    /// <see cref="System.Collections.Generic.ICollection{T}"/>, the result is pre-sized to its
    /// <c>Count</c> so filling it never grows-and-copies. A null <paramref name="source"/> is treated
    /// as empty.
    /// </summary>
    /// <param name="source">The sequence to convert.</param>
    let toResizeArray (source: 'T seq) : ResizeArray<'T> =
        if isNull source then ResizeArray<'T>() else ResizeArray<'T>(source)
