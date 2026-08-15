namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

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
