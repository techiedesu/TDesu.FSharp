namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

open System.IO

/// <summary>Array extensions: stream conversion and allocation-free ValueOption lookups.</summary>
[<RequireQualifiedAccess>]
module Array =
    /// Converts a <see cref="System.IO.MemoryStream"/> to a byte array. A null
    /// <paramref name="memoryStream"/> is treated as empty and yields <c>[||]</c>.
    /// <param name="memoryStream">The memory stream to convert.</param>
    let inline ofMemoryStream (memoryStream: MemoryStream) =
        if isNull memoryStream then [||] else memoryStream.ToArray()

    /// <summary>
    /// Returns the first element satisfying <paramref name="filter"/> as <c>ValueSome</c>, or
    /// <c>ValueNone</c> if none match. Allocates nothing on either path. A null <paramref name="array"/>
    /// is treated as empty and yields <c>ValueNone</c>.
    /// </summary>
    /// <param name="filter">The predicate to test each element.</param>
    /// <param name="array">The array to search.</param>
    let inline valueTryFind ([<InlineIfLambda>] filter: 'T -> bool) (array: 'T[]) : 'T voption =
        if isNull array then
            ValueNone
        else
            let mutable result = ValueNone
            let mutable i = 0

            while result.IsNone && i < array.Length do
                let current = array[i]

                if filter current then
                    result <- ValueSome current

                i <- i + 1

            result

    /// <summary>
    /// Returns the last element satisfying <paramref name="filter"/> as <c>ValueSome</c>, scanning from
    /// the end, or <c>ValueNone</c> if none match. Allocates nothing on either path. A null
    /// <paramref name="array"/> is treated as empty and yields <c>ValueNone</c>.
    /// </summary>
    /// <param name="filter">The predicate to test each element.</param>
    /// <param name="array">The array to search.</param>
    let inline valueTryFindLast ([<InlineIfLambda>] filter: 'T -> bool) (array: 'T[]) : 'T voption =
        if isNull array then
            ValueNone
        else
            let mutable result = ValueNone
            let mutable i = array.Length - 1

            while result.IsNone && i >= 0 do
                let current = array[i]

                if filter current then
                    result <- ValueSome current

                i <- i - 1

            result

    /// <summary>
    /// Applies <paramref name="chooser"/> to each element from the start and returns the first
    /// <c>ValueSome</c> result, or <c>ValueNone</c> if every application returns <c>ValueNone</c>. A
    /// null <paramref name="array"/> is treated as empty and yields <c>ValueNone</c>.
    /// </summary>
    /// <param name="chooser">The function applied to each element; the first ValueSome result is returned.</param>
    /// <param name="array">The array to search.</param>
    let inline valueChooseFirst ([<InlineIfLambda>] chooser: 'T -> 'U voption) (array: 'T[]) : 'U voption =
        if isNull array then
            ValueNone
        else
            let mutable result = ValueNone
            let mutable i = 0

            while result.IsNone && i < array.Length do
                result <- chooser array[i]
                i <- i + 1

            result

    /// <summary>
    /// Applies <paramref name="chooser"/> to each element from the end and returns the first
    /// <c>ValueSome</c> result, or <c>ValueNone</c> if every application returns <c>ValueNone</c>. A
    /// null <paramref name="array"/> is treated as empty and yields <c>ValueNone</c>.
    /// </summary>
    /// <param name="chooser">The function applied to each element; the first ValueSome result is returned.</param>
    /// <param name="array">The array to search.</param>
    let inline valueChooseLast ([<InlineIfLambda>] chooser: 'T -> 'U voption) (array: 'T[]) : 'U voption =
        if isNull array then
            ValueNone
        else
            let mutable result = ValueNone
            let mutable i = array.Length - 1

            while result.IsNone && i >= 0 do
                result <- chooser array[i]
                i <- i - 1

            result
