namespace TDesu.FSharp

open System
open Microsoft.FSharp.Core

#nowarn "0077"
#if !FABLE_COMPILER
#nowarn "0042"
#endif

/// <namespacedoc>
///   <summary>Core utilities: operators, Guard, UnixTime, String, Option, Result, Validation, Clock, StateMachine, NumericParsing.</summary>
/// </namespacedoc>
module Operators =

    /// <summary>
    /// Reverse application operator: <c>f ^ x</c> is equivalent to <c>f x</c>.
    /// Avoids extra parentheses in nested calls.
    /// Unlike the backward pipe <c>&lt;|</c>, <c>^</c> has higher precedence
    /// (binds tighter than comparisons and logical operators).
    /// Both are right-associative: <c>f ^ g ^ x</c> = <c>f (g x)</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// let result = string ^ 42 + 1            // string (42 + 1) = "43"
    /// raise ^ Exception ^ sprintf "err: %s" s  // raise (Exception (sprintf "err: %s" s))
    ///
    /// // &lt;| conflicts with = (same precedence), requiring parentheses:
    /// if x = (f &lt;| 42) then ...   // need parens — ambiguous without them
    /// if x = f ^ 42 then ...        // ^ binds tighter than = — just works
    /// </code>
    /// </example>
    /// <param name="f">Function to apply.</param>
    /// <param name="x">Argument to pass to the function.</param>
    let inline (^) f x = f x

    /// <summary>
    /// Ignores the return value of an expression. Useful for fluent/chain APIs that return <c>this</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// %list.Add(42)  // Add returns void in C#, but this silences any return
    /// </code>
    /// </example>
    /// <param name="x">Value whose return is discarded.</param>
    let inline (~%) x = ignore x

    /// <summary>
    /// Assigns <see cref="ValueSome"/>(<paramref name="a"/>) to a byref field,
    /// or <see cref="ValueNone"/> if <paramref name="a"/> is null.
    /// </summary>
    /// <param name="field">Byref field to assign to.</param>
    /// <param name="a">Value to wrap; if null, assigns <c>ValueNone</c>.</param>
    let inline (<-?) (field: _ byref) a =
        if Object.ReferenceEquals(null, a) then
            field <- ValueNone
        else
            field <- ValueSome a

    /// <summary>
    /// Returns <c>true</c> if the reference-type value is not null.
    /// </summary>
    /// <param name="v">Reference-type value to check.</param>
    let inline isNotNull<'T when 'T: not struct> (v: 'T) = obj.ReferenceEquals(v, null) |> not

    /// <summary>
    /// Applies a side-effect action to a value, then returns the value unchanged.
    /// Useful for logging or debugging in a pipeline.
    /// </summary>
    /// <example>
    /// <code>
    /// let result = 42 |> tee (printfn "got %d") |> string
    /// </code>
    /// </example>
    /// <param name="f">Side-effect action to apply.</param>
    /// <param name="v">Value to pass through.</param>
    let inline tee ([<InlineIfLambda>] f: 'T -> unit) (v: 'T) =
        f v
        v

    /// <summary>
    /// Applies two side-effect actions to a value, then returns the value unchanged.
    /// </summary>
    /// <param name="f">First side-effect action.</param>
    /// <param name="g">Second side-effect action.</param>
    /// <param name="v">Value to pass through.</param>
    let inline tee2 ([<InlineIfLambda>] f: 'T -> unit) ([<InlineIfLambda>] g: 'T -> unit) (v: 'T) =
        f v
        g v
        v

    /// <summary>
    /// Swaps the two arguments of a function: <c>swap f a b</c> calls <c>f b a</c>.
    /// </summary>
    /// <param name="f">Function whose arguments are swapped.</param>
    /// <param name="a">Second argument passed to <paramref name="f"/>.</param>
    /// <param name="b">First argument passed to <paramref name="f"/>.</param>
    let inline swap ([<InlineIfLambda>] f: 'T2 -> 'T1 -> _) (a: 'T1) (b: 'T2) = f b a

    /// <summary>
    /// Always returns the first argument, ignoring the second.
    /// <c>always x _</c> = <c>x</c>.
    /// </summary>
    /// <param name="x">Value to always return.</param>
    let inline always x _ = x

    /// <summary>
    /// Converts a <c>snake_case</c> string to <c>CamelCase</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// snakeCaseToCamelCase "hello_world" // "HelloWorld"
    /// </code>
    /// </example>
    /// <param name="str">The snake_case string to convert.</param>
    let snakeCaseToCamelCase (str: string) =
        if str.Length = 0 then str
        else
            let sb = System.Text.StringBuilder(str.Length)
            let mutable prev = '_'
            for c in str do
                let c = if prev = '_' then Char.ToUpper c else c
                if c <> '_' then sb.Append(c) |> ignore
                prev <- c
            sb.ToString()

    /// <summary>
    /// Converts a <c>CamelCase</c> string to <c>snake_case</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// camelCaseToSnakeCase "HelloWorld" // "hello_world"
    /// </code>
    /// </example>
    /// <param name="str">The CamelCase string to convert.</param>
    let camelCaseToSnakeCase (str: string) =
        if str.Length = 0 then str
        else
            let sb = System.Text.StringBuilder(str.Length + 4)
            sb.Append(Char.ToLower str[0]) |> ignore
            for i = 1 to str.Length - 1 do
                let c = str[i]
                if Char.IsUpper c then
                    sb.Append('_').Append(Char.ToLower c) |> ignore
                else
                    sb.Append(c) |> ignore
            sb.ToString()

    /// <summary>
    /// Uncurries a 2-arg function to accept a tuple.
    /// </summary>
    /// <param name="f">Curried function to uncurry.</param>
    /// <param name="a">First element of the tuple.</param>
    /// <param name="b">Second element of the tuple.</param>
    let inline uncurry ([<InlineIfLambda>] f) (a, b) = f a b
    /// <summary>
    /// Uncurries a 3-arg function to accept a triple.
    /// </summary>
    /// <param name="f">Curried function to uncurry.</param>
    /// <param name="a">First element of the triple.</param>
    /// <param name="b">Second element of the triple.</param>
    /// <param name="c">Third element of the triple.</param>
    let inline uncurry3 ([<InlineIfLambda>] f) (a, b, c) = f a b c

    /// <summary>
    /// Returns <c>true</c> if the given <see cref="System.Type"/> is an F# <c>Option</c> type.
    /// </summary>
    /// <param name="t">Type to inspect.</param>
    let isOptionType (t: Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

    /// <summary>
    /// Returns <c>true</c> if the given <see cref="System.Type"/> is an F# <c>ValueOption</c> type.
    /// </summary>
    /// <param name="t">Type to inspect.</param>
    let isValueOptionType (t: Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<voption<_>>

    /// <summary>
    /// Increments a byref numeric value in-place by one.
    /// </summary>
    /// <param name="a">Byref value to increment.</param>
    let inline inc (a: 'a byref) = a <- a + LanguagePrimitives.GenericOne

    /// <summary>Unsafe cast like in C#.</summary>
    /// <param name="a">Value to cast.</param>
#if FABLE_COMPILER
    let inline ucast<'a, 'b> (a: 'a) : 'b = unbox a
#else
    let inline ucast<'a, 'b> (a: 'a) : 'b = (# "" a: 'b #)

    /// <summary>
    /// Implicit cast using <c>op_Implicit</c>. Equivalent to an implicit conversion in C#.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    let inline icast< ^a, ^b when (^a or ^b): (static member op_Implicit: ^a -> ^b)> (value: ^a) : ^b =
        ((^a or ^b): (static member op_Implicit: ^a -> ^b) value)

    /// <summary>
    /// Explicit cast using <c>op_Explicit</c>. Equivalent to an explicit conversion in C#.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    let inline ecast< ^a, ^b when (^a or ^b): (static member op_Explicit: ^a -> ^b)> (value: ^a) : ^b =
        ((^a or ^b): (static member op_Explicit: ^a -> ^b) value)
#endif


    // ── Exception helpers ── FSharp.Core has failwith, invalidArg, invalidOp, nullArg.
    // These cover the rest, following the same naming convention.

    /// <summary>
    /// Throws <see cref="System.NotSupportedException"/>.
    /// </summary>
    /// <exception cref="System.NotSupportedException">Always thrown.</exception>
    /// <param name="msg">Exception message.</param>
    let inline notSupported (msg: string) : 'a = raise ^ NotSupportedException(msg)

    /// <summary>
    /// Throws <see cref="System.NotImplementedException"/>.
    /// </summary>
    /// <exception cref="System.NotImplementedException">Always thrown.</exception>
    /// <param name="msg">Exception message.</param>
    let inline notImpl (msg: string) : 'a = raise ^ NotImplementedException(msg)

    /// <summary>
    /// Throws <see cref="System.ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">Always thrown.</exception>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="msg">Exception message.</param>
    let inline argRange (paramName: string) (msg: string) : 'a =
        raise ^ ArgumentOutOfRangeException(paramName, msg)

    /// <summary>
    /// Throws <see cref="System.ArgumentOutOfRangeException"/> including the actual value.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">Always thrown.</exception>
    /// <param name="paramName">Name of the parameter.</param>
    /// <param name="actualValue">The out-of-range value.</param>
    /// <param name="msg">Exception message.</param>
    let inline argRangeVal (paramName: string) (actualValue: obj) (msg: string) : 'a =
        raise ^ ArgumentOutOfRangeException(paramName, actualValue, msg)

    /// <summary>
    /// Throws <see cref="System.AggregateException"/> wrapping multiple errors.
    /// </summary>
    /// <exception cref="System.AggregateException">Always thrown.</exception>
    /// <param name="errors">Inner exceptions to aggregate.</param>
    let inline aggregate (errors: exn seq) : 'a =
        raise ^ AggregateException(errors)

    /// <summary>
    /// Throws <see cref="System.ObjectDisposedException"/>.
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">Always thrown.</exception>
    /// <param name="objectName">Name of the disposed object.</param>
    let inline disposed (objectName: string) : 'a =
        raise ^ ObjectDisposedException(objectName)

    /// <summary>
    /// Throws <see cref="System.TimeoutException"/>.
    /// </summary>
    /// <exception cref="System.TimeoutException">Always thrown.</exception>
    /// <param name="msg">Exception message.</param>
    let inline timedOut (msg: string) : 'a = raise ^ TimeoutException(msg)

    /// <summary>
    /// Throws <see cref="System.InvalidCastException"/>.
    /// </summary>
    /// <exception cref="System.InvalidCastException">Always thrown.</exception>
    /// <param name="msg">Exception message.</param>
    let inline invalidCast (msg: string) : 'a = raise ^ InvalidCastException(msg)

    // ── Printf variants ── type-safe formatted messages via %d, %s, %A, etc.

    /// <summary>Throws <see cref="System.InvalidOperationException"/> with a formatted message.</summary>
    /// <param name="fmt">Printf format string.</param>
    let inline invalidOpf fmt = Printf.ksprintf invalidOp fmt

    /// <summary>Throws <see cref="System.NotSupportedException"/> with a formatted message.</summary>
    /// <param name="fmt">Printf format string.</param>
    let inline notSupportedf fmt = Printf.ksprintf notSupported fmt

    /// <summary>Throws <see cref="System.NotImplementedException"/> with a formatted message.</summary>
    /// <param name="fmt">Printf format string.</param>
    let inline notImplf fmt = Printf.ksprintf notImpl fmt

    /// <summary>Throws <see cref="System.TimeoutException"/> with a formatted message.</summary>
    /// <param name="fmt">Printf format string.</param>
    let inline timedOutf fmt = Printf.ksprintf timedOut fmt

    /// <summary>Throws <see cref="System.InvalidCastException"/> with a formatted message.</summary>
    /// <param name="fmt">Printf format string.</param>
    let inline invalidCastf fmt = Printf.ksprintf invalidCast fmt

    /// Wraps an F# function as a <see cref="System.Action{T}"/> with 1 parameter.
    /// <param name="f">Function to wrap.</param>
    let inline toAction f = Action<'a> f
    /// Wraps an F# function as a <see cref="System.Action{T1,T2}"/> with 2 parameters.
    /// <param name="f">Function to wrap.</param>
    let inline toAction2 f = Action<'a, 'b> f
    /// Wraps an F# function as a <see cref="System.Action{T1,T2,T3}"/> with 3 parameters.
    /// <param name="f">Function to wrap.</param>
    let inline toAction3 f = Action<'a, 'b, 'c> f

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
        if obj.ReferenceEquals(value, null) then nullArg paramName

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

/// <summary>
/// Fast cached Unix timestamp — avoids repeated syscalls.
/// Updated lazily, accurate to ~15ms (system timer resolution).
/// </summary>
/// <remarks>
/// Thread-safe. The <see cref="CalData"/> reference is swapped atomically.
/// On .NET, uses <see cref="System.Diagnostics.Stopwatch"/> for high-resolution elapsed time.
/// On Fable, falls back to <see cref="System.DateTimeOffset.UtcNow"/>.
/// </remarks>
[<RequireQualifiedAccess>]
module UnixTime =
#if FABLE_COMPILER
    /// Current Unix timestamp in seconds.
    let seconds () : int64 =
        DateTimeOffset.UtcNow.ToUnixTimeSeconds()

    /// Current Unix timestamp in milliseconds.
    let milliseconds () : int64 =
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

    /// Current Unix timestamp in seconds as int32 (for protocols that use 32-bit timestamps).
    let inline seconds32 () : int32 = int32 (seconds ())
#else
    open System.Diagnostics

    /// Calibration snapshot — swapped atomically as a single reference.
    [<Sealed>]
    type private CalData(seconds: int64, ms: int64, ticks: int64) =
        member _.Seconds = seconds
        member _.Ms = ms
        member _.Ticks = ticks

    let private sw = Stopwatch.StartNew()
    let mutable private cal =
        let now = DateTimeOffset.UtcNow
        CalData(now.ToUnixTimeSeconds(), now.ToUnixTimeMilliseconds(), sw.ElapsedTicks)

    /// Recalibrate from system clock. Called automatically if drift > 1 minute.
    /// Thread-safe: the CalData reference is swapped atomically.
    let recalibrate () =
        let now = DateTimeOffset.UtcNow
        Threading.Volatile.Write(&cal,
            CalData(now.ToUnixTimeSeconds(), now.ToUnixTimeMilliseconds(), sw.ElapsedTicks))

    /// <summary>
    /// Current Unix timestamp in seconds (fast, cached).
    /// </summary>
    /// <returns>Unix seconds since epoch.</returns>
    let seconds () : int64 =
        let c = Threading.Volatile.Read(&cal)
        let elapsed = (sw.ElapsedTicks - c.Ticks) / Stopwatch.Frequency
        let result = c.Seconds + elapsed
        if elapsed > 60L then recalibrate ()
        result

    /// <summary>
    /// Current Unix timestamp in milliseconds (fast, cached).
    /// </summary>
    /// <returns>Unix milliseconds since epoch.</returns>
    let milliseconds () : int64 =
        let c = Threading.Volatile.Read(&cal)
        let elapsedMs = (sw.ElapsedTicks - c.Ticks) * 1000L / Stopwatch.Frequency
        let result = c.Ms + elapsedMs
        if elapsedMs > 60000L then recalibrate ()
        result

    /// Current Unix timestamp in seconds as int32 (for protocols that use 32-bit timestamps).
    let inline seconds32 () : int32 = int32 (seconds ())
#endif

module Char =
    /// Converts a character to uppercase using the current culture.
    /// <param name="c">Character to convert.</param>
    let inline toUpper (c: Char) =
        Char.ToUpper(c)

    /// Converts a character to uppercase using invariant culture.
    /// <param name="c">Character to convert.</param>
    let inline toUpperInv (c: Char) =
        Char.ToUpperInvariant(c)

module Regex =

    open System.Text.RegularExpressions

    module Match =

        /// Gets the capture group at the given index.
        /// <param name="m">The match to extract from.</param>
        /// <param name="idx">Zero-based group index.</param>
        let inline getGroup (m: Match) (idx: int) =
            m.Groups[idx]

        /// Gets the second capture group (index 1).
        /// <param name="m">The match to extract from.</param>
        let inline getSecondGroup (m: Match) =
            m.Groups[1]

    module Capture =

        /// Gets the matched string value of a capture.
        /// <param name="c">Capture to get the value from.</param>
        let inline value (c: Capture) =
            c.Value
