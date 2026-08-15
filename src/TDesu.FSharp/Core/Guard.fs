namespace TDesu.FSharp

open System

/// <summary>
/// Guard module — validate-and-throw in one call.
/// </summary>
/// <remarks>
/// All guards are <c>inline</c> for zero-cost abstraction at call sites.
/// </remarks>
[<RequireQualifiedAccess>]
module Guard =
    /// <summary>
    /// Throws <see cref="System.ArgumentNullException"/> if value is null.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">When <paramref name="value"/> is null.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="value">Value to check for null.</param>
    let inline notNull (paramName: string) (value: 'T when 'T: not struct) =
        if Operators.isNullRef value then
            nullArg paramName

    /// <summary>
    /// Throws <see cref="System.ArgumentException"/> if string is null or empty.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <paramref name="value"/> is null or empty.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="value">String to check.</param>
    let inline notEmpty (paramName: string) (value: string) =
        if String.IsNullOrEmpty(value) then
            invalidArg paramName "Value must not be null or empty."

    /// <summary>
    /// Throws <see cref="System.ArgumentException"/> if string is null, empty, or whitespace.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <paramref name="value"/> is null, empty, or whitespace.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="value">String to check.</param>
    let inline notWhiteSpace (paramName: string) (value: string) =
        if String.IsNullOrWhiteSpace(value) then
            invalidArg paramName "Value must not be null, empty, or whitespace."

    /// <summary>
    /// Throws <see cref="System.ArgumentException"/> if condition is false.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <paramref name="condition"/> is false.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="msg">Exception message.</param>
    /// <param name="condition">Condition that must be true.</param>
    let inline isTrue (paramName: string) (msg: string) (condition: bool) =
        if not condition then invalidArg paramName msg

    /// <summary>
    /// Throws <see cref="System.ArgumentException"/> if condition is true.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <paramref name="condition"/> is true.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="msg">Exception message.</param>
    /// <param name="condition">Condition that must be false.</param>
    let inline isFalse (paramName: string) (msg: string) (condition: bool) =
        if condition then invalidArg paramName msg

    /// <summary>
    /// Throws <see cref="System.ArgumentOutOfRangeException"/> if value is not between lo and hi (inclusive).
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">When <paramref name="value"/> is outside [<paramref name="lo"/>, <paramref name="hi"/>].</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="lo">Inclusive lower bound.</param>
    /// <param name="hi">Inclusive upper bound.</param>
    /// <param name="value">Value to validate.</param>
    let inline inRange (paramName: string) (lo: 'T) (hi: 'T) (value: 'T) =
        if value < lo || value > hi then
            Operators.argRangeVal paramName (value :> obj) $"Must be between %A{lo} and %A{hi}."

    /// <summary>
    /// Throws <see cref="System.ArgumentOutOfRangeException"/> if value is less than or equal to zero.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">When <paramref name="value"/> &lt;= 0.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="value">Value that must be positive.</param>
    let inline positive (paramName: string) (value: 'T) =
        if value <= LanguagePrimitives.GenericZero then
            Operators.argRangeVal paramName (value :> obj) $"Must be positive, got %A{value}."

    /// <summary>
    /// Throws <see cref="System.ArgumentOutOfRangeException"/> if value is negative.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">When <paramref name="value"/> &lt; 0.</exception>
    /// <param name="paramName">Name of the parameter for the exception.</param>
    /// <param name="value">Value that must not be negative.</param>
    let inline notNegative (paramName: string) (value: 'T) =
        if value < LanguagePrimitives.GenericZero then
            Operators.argRangeVal paramName (value :> obj) $"Must not be negative, got %A{value}."
