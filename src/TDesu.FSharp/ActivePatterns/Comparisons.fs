namespace TDesu.FSharp.ActivePatterns

/// <summary>
/// Curried comparison active patterns for use directly in a <c>match</c> expression.
/// </summary>
/// <remarks>
/// Marked with the auto-open attribute: opening <c>TDesu.FSharp.ActivePatterns</c> (or this module
/// directly) brings <c>Eq</c>, <c>NEq</c>, <c>Lt</c>, <c>Gt</c>, <c>LtEq</c>, <c>GtEq</c> and <c>Between</c>
/// into scope unqualified. Auto-open only makes sense here because these names are useless qualified —
/// nobody wants to write <c>Comparisons.Lt</c> inside a <c>match</c> arm. Each pattern takes its comparand(s)
/// first and the matched value last, so <c>match x with | Lt 10 -&gt; …</c> reads as "x is less than 10".
/// They rely only on F#'s built-in structural equality/comparison constraints, so they work for any
/// comparable type — numbers, strings, <see cref="System.DateTime"/>, tuples, comparable records — not just numbers.
///
/// Each pattern is <c>[&lt;return: Struct&gt;]</c>, so a match arm costs no allocation: a plain partial
/// active pattern would allocate an <c>FSharpOption</c> every time the arm is evaluated, which for a
/// one-comparison pattern is the whole cost of the match. The use site is unchanged either way.
/// </remarks>
[<AutoOpen>]
module Comparisons =

    /// <summary>
    /// Matches when the matched value is structurally equal to <paramref name="comparand"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// let describe x =
    ///     match x with
    ///     | Eq 0 -> "zero"
    ///     | _ -> "nonzero"
    /// </code>
    /// </example>
    /// <param name="comparand">The value to compare against.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|Eq|_|) (comparand: 'a) (value: 'a) =
        value = comparand

    /// <summary>
    /// Matches when the matched value is not structurally equal to <paramref name="comparand"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// match status with
    /// | NEq "ok" -> escalate ()
    /// | _ -> ()
    /// </code>
    /// </example>
    /// <param name="comparand">The value to compare against.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|NEq|_|) (comparand: 'a) (value: 'a) =
        value <> comparand

    /// <summary>
    /// Matches when the matched value is strictly greater than <paramref name="comparand"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// match age with
    /// | Gt 17 -> "adult"
    /// | _ -> "minor"
    /// </code>
    /// </example>
    /// <param name="comparand">The value to compare against.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|Gt|_|) (comparand: 'a) (value: 'a) =
        value > comparand

    /// <summary>
    /// Matches when the matched value is strictly less than <paramref name="comparand"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// match temperature with
    /// | Lt 0.0 -> "freezing"
    /// | _ -> "above freezing"
    /// </code>
    /// </example>
    /// <param name="comparand">The value to compare against.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|Lt|_|) (comparand: 'a) (value: 'a) =
        value < comparand

    /// <summary>
    /// Matches when the matched value is less than or equal to <paramref name="comparand"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// match retries with
    /// | LtEq 3 -> "still retrying"
    /// | _ -> "gave up"
    /// </code>
    /// </example>
    /// <param name="comparand">The value to compare against.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|LtEq|_|) (comparand: 'a) (value: 'a) =
        value <= comparand

    /// <summary>
    /// Matches when the matched value is greater than or equal to <paramref name="comparand"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// match score with
    /// | GtEq 60 -> "pass"
    /// | _ -> "fail"
    /// </code>
    /// </example>
    /// <param name="comparand">The value to compare against.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|GtEq|_|) (comparand: 'a) (value: 'a) =
        value >= comparand

    /// <summary>
    /// Matches when the matched value lies within [<paramref name="lo"/>, <paramref name="hi"/>], inclusive on both ends.
    /// </summary>
    /// <remarks>
    /// If <paramref name="lo"/> is greater than <paramref name="hi"/> the range is empty, so the pattern never matches.
    /// </remarks>
    /// <example>
    /// <code>
    /// match httpStatus with
    /// | Between 200 299 -> "success"
    /// | Between 400 499 -> "client error"
    /// | _ -> "other"
    /// </code>
    /// </example>
    /// <param name="lo">Inclusive lower bound.</param>
    /// <param name="hi">Inclusive upper bound.</param>
    /// <param name="value">The value being matched.</param>
    let inline (|Between|_|) (lo: 'a) (hi: 'a) (value: 'a) =
        value >= lo && value <= hi
