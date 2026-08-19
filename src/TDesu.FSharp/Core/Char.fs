namespace TDesu.FSharp

open System

module Char =
    /// Converts a character to uppercase using the current culture.
    /// <param name="c">Character to convert.</param>
    let inline toUpper (c: Char) = Char.ToUpper(c)

    /// Converts a character to uppercase using invariant culture.
    /// <param name="c">Character to convert.</param>
    let inline toUpperInv (c: Char) = Char.ToUpperInvariant(c)
