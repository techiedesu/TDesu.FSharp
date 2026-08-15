namespace TDesu.FSharp

/// <summary>
/// Named, in-place mutation helpers for <c>byref</c> values. Every function takes the byref first, so
/// they read the same way at the call site as the built-in <c>&lt;-</c> assignment they replace.
/// </summary>
/// <example>
/// <code>
/// let mutable total = 0
/// for x in items do
///     Byref.add &amp;total x
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Byref =
    /// <summary>
    /// Increments a byref numeric value in-place by one.
    /// </summary>
    /// <param name="a">The byref value to increment.</param>
    let inline inc (a: 'a byref) = a <- a + LanguagePrimitives.GenericOne

    /// <summary>
    /// Decrements a byref numeric value in-place by one.
    /// </summary>
    /// <param name="a">The byref value to decrement.</param>
    let inline dec (a: 'a byref) = a <- a - LanguagePrimitives.GenericOne

    /// <summary>
    /// Assigns a value to a byref in-place.
    /// </summary>
    /// <param name="a">The byref to assign to.</param>
    /// <param name="v">The value to assign.</param>
    let inline setv (a: 'a byref) v = a <- v

    /// <summary>
    /// Adds a value to a byref in-place.
    /// </summary>
    /// <param name="a">The byref to add to.</param>
    /// <param name="v">The value to add.</param>
    let inline add (a: 'a byref) v = a <- a + v

    /// <summary>
    /// Subtracts a value from a byref in-place.
    /// </summary>
    /// <param name="a">The byref to subtract from.</param>
    /// <param name="v">The value to subtract.</param>
    let inline sub (a: 'a byref) v = a <- a - v

    /// <summary>
    /// Multiplies a byref by a value in-place.
    /// </summary>
    /// <param name="a">The byref to multiply.</param>
    /// <param name="v">The value to multiply by.</param>
    let inline mul (a: 'a byref) v = a <- a * v

    /// <summary>
    /// Divides a byref by a value in-place.
    /// </summary>
    /// <param name="a">The byref to divide.</param>
    /// <param name="v">The value to divide by.</param>
    let inline div (a: 'a byref) v = a <- a / v
