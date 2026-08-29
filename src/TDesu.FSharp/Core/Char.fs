namespace TDesu.FSharp

open System

module Char =
    /// Parses a single-character string as a char, returning <c>Some(value)</c> or <c>None</c>. A
    /// string of any other length, including the empty one, is a failure rather than a truncation.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Char.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a single-character string as a char, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Char.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Converts a character to uppercase using the current culture.
    /// <param name="c">Character to convert.</param>
    let inline toUpper (c: Char) = Char.ToUpper(c)

    /// Converts a character to uppercase using invariant culture.
    /// <param name="c">Character to convert.</param>
    let inline toUpperInv (c: Char) = Char.ToUpperInvariant(c)
