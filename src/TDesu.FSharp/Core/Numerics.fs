namespace TDesu.FSharp

open System

/// Generic numeric helpers: clamping, linear interpolation, and inclusive-range checks.
/// Built on F#'s built-in arithmetic/comparison constraints, so they work for <c>float</c>,
/// <c>decimal</c>, custom numeric types — anything with the required operators — not just <c>int</c>.
[<RequireQualifiedAccess>]
module Numeric =

    /// <summary>
    /// Restricts <paramref name="value"/> to the inclusive range [<paramref name="lo"/>, <paramref name="hi"/>].
    /// </summary>
    /// <remarks>
    /// Total: never throws. If <paramref name="lo"/> is greater than <paramref name="hi"/> the two comparisons
    /// below still run in order, so the result is always exactly <paramref name="lo"/> or <paramref name="hi"/>
    /// (never a value strictly between them, and never the unclamped <paramref name="value"/>): anything less
    /// than <paramref name="lo"/> clamps to <paramref name="lo"/>, everything else clamps to <paramref name="hi"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// Numeric.clamp 0 10 15   // 10
    /// Numeric.clamp 0 10 -3   // 0
    /// Numeric.clamp 0 10 4    // 4
    /// Numeric.clamp 10 1 5    // 10 — degenerate lo > hi, see remarks
    /// </code>
    /// </example>
    /// <param name="lo">Inclusive lower bound.</param>
    /// <param name="hi">Inclusive upper bound.</param>
    /// <param name="value">Value to clamp.</param>
    let inline clamp (lo: 'a) (hi: 'a) (value: 'a) : 'a =
        if value < lo then lo
        elif value > hi then hi
        else value

    /// <summary>
    /// Linearly interpolates between <paramref name="a"/> and <paramref name="b"/> by fraction <paramref name="t"/>.
    /// </summary>
    /// <remarks>
    /// Unclamped: <paramref name="t"/> is not restricted to [0, 1]. <c>t = 0</c> returns <paramref name="a"/>,
    /// <c>t = 1</c> returns <paramref name="b"/>, and values outside [0, 1] extrapolate linearly past
    /// <paramref name="a"/> or <paramref name="b"/> instead of clamping. Pipe <paramref name="t"/> through
    /// <see cref="clamp"/> first if clamped interpolation is what you want.
    /// </remarks>
    /// <example>
    /// <code>
    /// Numeric.lerp 0.0 10.0 0.0    // 0.0
    /// Numeric.lerp 0.0 10.0 1.0    // 10.0
    /// Numeric.lerp 0.0 10.0 0.5    // 5.0
    /// Numeric.lerp 0.0 10.0 2.0    // 20.0 — extrapolates past b
    /// </code>
    /// </example>
    /// <param name="a">Value at <c>t = 0</c>.</param>
    /// <param name="b">Value at <c>t = 1</c>.</param>
    /// <param name="t">Interpolation fraction.</param>
    let inline lerp (a: 'a) (b: 'a) (t: 'a) : 'a =
        a + (b - a) * t

    /// <summary>
    /// Inverse of <see cref="lerp"/>: finds the fraction <c>t</c> at which <paramref name="value"/> occurs
    /// between <paramref name="a"/> and <paramref name="b"/>.
    /// </summary>
    /// <remarks>
    /// Total for floating-point types: when <paramref name="a"/> equals <paramref name="b"/> the divisor is
    /// zero and the result is <c>NaN</c> or <c>±Infinity</c> per IEEE 754, never an exception. For integral
    /// types, a zero divisor throws <see cref="System.DivideByZeroException"/>, matching that type's own
    /// division semantics — this function adds no special-casing on top of it.
    /// </remarks>
    /// <example>
    /// <code>
    /// Numeric.inverseLerp 0.0 10.0 5.0    // 0.5
    /// Numeric.inverseLerp 0.0 10.0 15.0   // 1.5 — outside [0, 1], value is past b
    /// </code>
    /// </example>
    /// <param name="a">Value at <c>t = 0</c>.</param>
    /// <param name="b">Value at <c>t = 1</c>.</param>
    /// <param name="value">Value to locate between <paramref name="a"/> and <paramref name="b"/>.</param>
    let inline inverseLerp (a: 'a) (b: 'a) (value: 'a) : 'a =
        (value - a) / (b - a)

    /// <summary>
    /// Returns <c>true</c> if <paramref name="value"/> lies within [<paramref name="lo"/>, <paramref name="hi"/>], inclusive on both ends.
    /// </summary>
    /// <remarks>
    /// If <paramref name="lo"/> is greater than <paramref name="hi"/> the range is empty, so this always returns <c>false</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// Numeric.isBetween 0 10 0     // true
    /// Numeric.isBetween 0 10 10    // true
    /// Numeric.isBetween 0 10 11    // false
    /// </code>
    /// </example>
    /// <param name="lo">Inclusive lower bound.</param>
    /// <param name="hi">Inclusive upper bound.</param>
    /// <param name="value">Value to test.</param>
    let inline isBetween (lo: 'a) (hi: 'a) (value: 'a) : bool =
        value >= lo && value <= hi

    /// <summary>
    /// The additive identity for numeric type <c>'a</c>, via <see cref="Microsoft.FSharp.Core.LanguagePrimitives.GenericZero"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// Numeric.zero&lt;int&gt;      // 0
    /// Numeric.zero&lt;float&gt;    // 0.0
    /// </code>
    /// </example>
    let inline zero< ^a when ^a: (static member Zero: ^a)> : ^a =
        LanguagePrimitives.GenericZero< ^a>

    /// <summary>
    /// The multiplicative identity for numeric type <c>'a</c>, via <see cref="Microsoft.FSharp.Core.LanguagePrimitives.GenericOne"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// Numeric.one&lt;int&gt;      // 1
    /// Numeric.one&lt;float&gt;    // 1.0
    /// </code>
    /// </example>
    let inline one< ^a when ^a: (static member One: ^a)> : ^a =
        LanguagePrimitives.GenericOne< ^a>

/// <summary>
/// Structural shape for a flags-style enum: a struct <see cref="System.Enum"/> whose underlying type
/// supports the bitwise <c>|||</c>, <c>^^^</c> and <c>&amp;&amp;&amp;</c> operators. In practice this is every
/// .NET enum type, since the F# compiler resolves those three operators for any enum intrinsically —
/// this abbreviation exists purely so <see cref="Enum"/>'s functions don't each repeat the same three-line
/// constraint clause.
/// </summary>
type EnumShape<'enum when 'enum: struct
                      and 'enum :> Enum
                      and 'enum: (static member (|||): 'enum -> 'enum -> 'enum)
                      and 'enum: (static member (^^^): 'enum -> 'enum -> 'enum)
                      and 'enum: (static member (&&&): 'enum -> 'enum -> 'enum)> = 'enum

/// Bitwise flag helpers for <c>[&lt;Flags&gt;]</c> enums, generic over any enum type via <see cref="EnumShape{T}"/>.
[<RequireQualifiedAccess>]
module Enum =

    /// <summary>
    /// Returns <c>true</c> if <paramref name="value"/> has every bit of <paramref name="flag"/> set.
    /// </summary>
    /// <example>
    /// <code>
    /// [&lt;Flags&gt;]
    /// type Permissions = None = 0 | Read = 1 | Write = 2 | Execute = 4
    ///
    /// Enum.hasFlag Permissions.Read (Permissions.Read ||| Permissions.Write)      // true
    /// Enum.hasFlag Permissions.Execute (Permissions.Read ||| Permissions.Write)   // false
    /// </code>
    /// </example>
    /// <param name="flag">The flag(s) to test for.</param>
    /// <param name="value">The value to test.</param>
    let inline hasFlag (flag: EnumShape<_>) (value: EnumShape<_>) = value &&& flag = flag

    /// <summary>
    /// Returns <paramref name="value"/> with every bit of <paramref name="flag"/> set.
    /// </summary>
    /// <example>
    /// <code>
    /// Enum.addFlag Permissions.Write Permissions.Read   // Read ||| Write
    /// </code>
    /// </example>
    /// <param name="flag">The flag(s) to add.</param>
    /// <param name="value">The value to add the flag(s) to.</param>
    let inline addFlag (flag: EnumShape<_>) (value: EnumShape<_>) = flag ||| value

    /// <summary>
    /// Adds <paramref name="flag"/> to <paramref name="value"/> only when <paramref name="condition"/> is
    /// <c>true</c>; otherwise returns <paramref name="value"/> unchanged.
    /// </summary>
    /// <example>
    /// <code>
    /// Enum.addFlagWhen isAdmin Permissions.Write Permissions.Read
    /// </code>
    /// </example>
    /// <param name="condition">Whether to add the flag.</param>
    /// <param name="flag">The flag(s) to add.</param>
    /// <param name="value">The value to conditionally add the flag(s) to.</param>
    let inline addFlagWhen condition (flag: EnumShape<_>) (value: EnumShape<_>) =
        if condition then flag ||| value else value

    /// <summary>
    /// Returns <paramref name="value"/> with every bit of <paramref name="flag"/> cleared.
    /// </summary>
    /// <example>
    /// <code>
    /// Enum.removeFlag Permissions.Write (Permissions.Read ||| Permissions.Write)   // Read
    /// </code>
    /// </example>
    /// <param name="flag">The flag(s) to remove.</param>
    /// <param name="value">The value to remove the flag(s) from.</param>
    let inline removeFlag (flag: EnumShape<_>) (value: EnumShape<_>) = (flag ||| value) ^^^ flag

    /// <summary>
    /// Removes <paramref name="flag"/> from <paramref name="value"/> only when <paramref name="condition"/> is
    /// <c>true</c>; otherwise returns <paramref name="value"/> unchanged.
    /// </summary>
    /// <example>
    /// <code>
    /// Enum.removeFlagWhen isBanned Permissions.Write (Permissions.Read ||| Permissions.Write)
    /// </code>
    /// </example>
    /// <param name="condition">Whether to remove the flag.</param>
    /// <param name="flag">The flag(s) to remove.</param>
    /// <param name="value">The value to conditionally remove the flag(s) from.</param>
    let inline removeFlagWhen condition (flag: EnumShape<_>) (value: EnumShape<_>) =
        if condition then (flag ||| value) ^^^ flag else value
