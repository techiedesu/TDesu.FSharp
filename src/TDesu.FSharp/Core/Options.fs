namespace TDesu.FSharp

open TDesu.FSharp.Operators

[<RequireQualifiedAccess>]
module ValueOption =
    /// Converts a C# TryXxx <c>(bool * value)</c> tuple to a <see cref="ValueOption{T}"/>.
    /// <param name="status">The success flag from the TryXxx method.</param>
    /// <param name="value">The output value from the TryXxx method.</param>
    let inline ofCSharpTryPattern (status, value) =
        if status then ValueSome value else ValueNone

    /// <summary>
    /// Type-guard cast: returns <c>ValueSome(value :?> 'T)</c> if <paramref name="value"/> is of type
    /// <c>'T</c>, otherwise <c>ValueNone</c>. A <c>null</c> input returns <c>ValueNone</c> rather than
    /// throwing, since a CLR type test against null never succeeds for any <c>'T</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// let boxed: obj = box "hello"
    /// ValueOption.tryCast&lt;string&gt; boxed // ValueSome "hello"
    /// ValueOption.tryCast&lt;int&gt; boxed    // ValueNone
    /// ValueOption.tryCast&lt;string&gt; null  // ValueNone
    /// </code>
    /// </example>
    /// <param name="value">The boxed value to type-test.</param>
    let inline tryCast<'T> (value: obj) : 'T voption =
        match value with
        | :? 'T as v -> ValueSome v
        | _ -> ValueNone

    /// <summary>
    /// Returns <c>ValueSome(value)</c> if <paramref name="predicate"/> returns <c>true</c> for it, otherwise
    /// <c>ValueNone</c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="predicate"/> is a function argument: per the null policy a null or throwing predicate
    /// is a programmer error, so it is called unguarded and any exception (including on a null delegate)
    /// propagates unchanged rather than being swallowed.
    /// </remarks>
    /// <example>
    /// <code>
    /// ValueOption.ofPredicate (fun x -> x > 0) 5   // ValueSome 5
    /// ValueOption.ofPredicate (fun x -> x > 0) -5  // ValueNone
    /// </code>
    /// </example>
    /// <param name="predicate">The predicate to test the value with.</param>
    /// <param name="value">The value to conditionally wrap.</param>
    let inline ofPredicate ([<InlineIfLambda>] predicate: 'T -> bool) (value: 'T) : 'T voption =
        if predicate value then ValueSome value else ValueNone

[<RequireQualifiedAccess>]
module Option =
    /// Cached <c>Some(())</c> to avoid allocations.
    let someUnit = Some()

    /// Returns <c>Some(())</c> if true, <c>None</c> if false.
    /// <param name="v">The boolean value to convert.</param>
    let inline ofBool (v: bool) =
        match v with
        | false -> None
        | true -> someUnit

    /// Converts a C# TryXxx <c>(bool * value)</c> tuple to an <see cref="Option{T}"/>.
    /// <param name="status">The success flag from the TryXxx method.</param>
    /// <param name="value">The output value from the TryXxx method.</param>
    let inline ofCSharpTryPattern (status, value) = if status then Some value else None

    /// Applies an action to the Some value (discarding its return value), or does nothing for None.
    /// <param name="f">The function to apply to the contained value.</param>
    let inline iterIgnore ([<InlineIfLambda>] f: 'T -> 'TResult) =
        function
        | None -> ()
        | Some v -> %f v

    /// Returns true if the option contains true; false otherwise.
    /// <param name="v">The boolean option to check.</param>
    let inline isTrue (v: bool option) = Option.defaultValue false v

    /// <summary>
    /// Returns <c>None</c> if the string is null/empty/whitespace, otherwise <c>Some(s)</c>.
    /// </summary>
    /// <param name="s">The string to convert.</param>
    let inline ofString (s: string | null) =
        if System.String.IsNullOrWhiteSpace(s) then None else Some s

    /// <summary>
    /// Type-guard cast: returns <c>Some(value :?> 'T)</c> if <paramref name="value"/> is of type <c>'T</c>,
    /// otherwise <c>None</c>. A <c>null</c> input returns <c>None</c> rather than throwing, since a CLR type
    /// test against null never succeeds for any <c>'T</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// let boxed: obj = box "hello"
    /// Option.tryCast&lt;string&gt; boxed // Some "hello"
    /// Option.tryCast&lt;int&gt; boxed    // None
    /// Option.tryCast&lt;string&gt; null  // None
    /// </code>
    /// </example>
    /// <param name="value">The boxed value to type-test.</param>
    let inline tryCast<'T> (value: obj) : 'T option =
        match value with
        | :? 'T as v -> Some v
        | _ -> None

    /// <summary>
    /// Returns <c>Some(value)</c> if <paramref name="predicate"/> returns <c>true</c> for it, otherwise
    /// <c>None</c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="predicate"/> is a function argument: per the null policy a null or throwing predicate
    /// is a programmer error, so it is called unguarded and any exception (including on a null delegate)
    /// propagates unchanged rather than being swallowed.
    /// </remarks>
    /// <example>
    /// <code>
    /// Option.ofPredicate (fun x -> x > 0) 5   // Some 5
    /// Option.ofPredicate (fun x -> x > 0) -5  // None
    /// </code>
    /// </example>
    /// <param name="predicate">The predicate to test the value with.</param>
    /// <param name="value">The value to conditionally wrap.</param>
    let inline ofPredicate ([<InlineIfLambda>] predicate: 'T -> bool) (value: 'T) : 'T option =
        if predicate value then Some value else None

    /// <summary>
    /// Converts <c>Some</c> to <c>Ok</c>, <c>None</c> to <c>Error</c> with the given error value.
    /// </summary>
    /// <param name="error">The error value to use when the option is None.</param>
    let inline toResult error =
        function
        | Some v -> Ok v
        | None -> Error error

    /// <summary>
    /// Applies a side-effect on <c>Some</c> and returns the option unchanged.
    /// </summary>
    /// <param name="f">The side-effect function to apply.</param>
    /// <param name="opt">The option to inspect.</param>
    let inline tee ([<InlineIfLambda>] f: 'T -> unit) (opt: 'T option) =
        match opt with
        | Some v ->
            f v
            opt
        | None -> None

    /// <summary>
    /// Combines two options into a tuple option. <c>None</c> if either is <c>None</c>.
    /// </summary>
    /// <param name="o1">The first option.</param>
    /// <param name="o2">The second option.</param>
    let inline zip (o1: 'T1 option) (o2: 'T2 option) =
        match o1, o2 with
        | Some a, Some b -> Some(a, b)
        | _ -> None

    /// Maps a function over two option values. None if either is None.
    /// <param name="f">The mapping function.</param>
    /// <param name="o1">The first option.</param>
    /// <param name="o2">The second option.</param>
    let inline map2 ([<InlineIfLambda>] f: 'T1 -> 'T2 -> 'TResult) (o1: 'T1 option) (o2: 'T2 option) =
        match o1, o2 with
        | Some a, Some b -> Some(f a b)
        | _ -> None

    /// Maps a function over three option values. None if any is None.
    /// <param name="f">The mapping function.</param>
    /// <param name="o1">The first option.</param>
    /// <param name="o2">The second option.</param>
    /// <param name="o3">The third option.</param>
    let inline map3
        ([<InlineIfLambda>] f: 'T1 -> 'T2 -> 'T3 -> 'TResult)
        (o1: 'T1 option)
        (o2: 'T2 option)
        (o3: 'T3 option)
        =
        match o1, o2, o3 with
        | Some a, Some b, Some c -> Some(f a b c)
        | _ -> None

/// <summary>
/// What FSharp.Core's <c>Result</c> module does not have. <c>map</c>, <c>bind</c>, <c>mapError</c>,
/// <c>isOk</c>, <c>isError</c>, <c>defaultValue</c>, <c>defaultWith</c> and <c>toOption</c> lived
/// here too until 1.6.0; FSharp.Core 9 ships all of them with the same names and shapes, and F#
/// resolves <c>Result.map</c> across every module of that name in scope, so callers did not change.
/// </summary>
[<RequireQualifiedAccess>]
module Result =
    /// <summary>
    /// Extracts the <c>Ok</c> value or throws <see cref="System.InvalidOperationException"/> on <c>Error</c>.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">When the result is <c>Error</c>.</exception>
    /// <param name="r">The result to unwrap.</param>
    let inline get (r: Result<_, _>) =
        match r with
        | Error err -> invalidOpf "Result contained Error: %O" err
        | Ok r -> r

    /// Extracts the Ok value, or computes a fallback from the Error.
    /// <param name="f">The function to compute a fallback from the Error value.</param>
    let inline valueOr ([<InlineIfLambda>] f: 'TError -> 'T) =
        function
        | Ok v -> v
        | Error e -> f e

    /// Returns the result if Ok, otherwise the given fallback result.
    /// <param name="ifError">The fallback result to use on Error.</param>
    /// <param name="r">The result to evaluate.</param>
    let inline orElse (ifError: Result<'T, 'TError>) (r: Result<'T, 'TError>) =
        match r with
        | Ok _ -> r
        | Error _ -> ifError

    /// Returns the result if Ok, otherwise computes a fallback from the Error.
    /// <param name="f">The function to compute a fallback result from the Error value.</param>
    let inline orElseWith ([<InlineIfLambda>] f: 'TError -> Result<'T, 'TError2>) =
        function
        | Ok v -> Ok v
        | Error e -> f e

    /// Applies a side-effect on Ok and returns the result unchanged.
    /// <param name="f">The side-effect function to apply on Ok.</param>
    /// <param name="r">The result to inspect.</param>
    let inline tee ([<InlineIfLambda>] f: 'T -> unit) (r: Result<'T, 'TError>) =
        match r with
        | Ok v ->
            f v
            r
        | Error _ -> r

    /// Applies a side-effect on Error and returns the result unchanged.
    /// <param name="f">The side-effect function to apply on Error.</param>
    /// <param name="r">The result to inspect.</param>
    let inline teeError ([<InlineIfLambda>] f: 'TError -> unit) (r: Result<'T, 'TError>) =
        match r with
        | Ok _ -> r
        | Error e ->
            f e
            r

    /// Converts an Option to a Result: Some becomes Ok, None becomes Error.
    /// <param name="error">The error value to use when the option is None.</param>
    let inline ofOption (error: 'TError) (opt: 'T option) : Result<'T, 'TError> =
        match opt with
        | Some v -> Ok v
        | None -> Error error

    /// <summary>
    /// Combines two results into a tuple. Returns the first <c>Error</c> if any.
    /// </summary>
    /// <param name="r1">The first result.</param>
    /// <param name="r2">The second result.</param>
    let inline zip (r1: Result<'T1, 'TError>) (r2: Result<'T2, 'TError>) =
        match r1, r2 with
        | Ok a, Ok b -> Ok(a, b)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    /// Discards the Ok value, preserving the Error.
    /// <param name="r">The result whose Ok value is discarded.</param>
    let inline ignore (r: Result<_, 'TError>) : Result<unit, 'TError> =
        match r with
        | Ok _ -> Ok()
        | Error e -> Error e

    /// Returns Ok(()) if true, Error(error) if false.
    /// <param name="error">The error value to use when the condition is false.</param>
    /// <param name="value">The boolean condition to check.</param>
    let inline requireTrue (error: 'TError) (value: bool) = if value then Ok() else Error error

    /// Returns Ok(()) if false, Error(error) if true.
    /// <param name="error">The error value to use when the condition is true.</param>
    /// <param name="value">The boolean condition to check.</param>
    let inline requireFalse (error: 'TError) (value: bool) = if not value then Ok() else Error error

    /// Returns Ok(value) if not null, Error(error) if null.
    /// <param name="error">The error value to use when the value is null.</param>
    /// <param name="value">The value to check for null.</param>
    let inline requireNotNull (error: 'TError) (value: 'T) =
        if obj.ReferenceEquals(value, null) then
            Error error
        else
            Ok value

    /// <summary>
    /// Wraps a function call in try/catch, returning <c>Ok</c> on success or <c>Error(exn)</c> on exception.
    /// </summary>
    /// <example>
    /// <code>
    /// Result.catch (fun () -> int "42")   // Ok 42
    /// Result.catch (fun () -> int "bad")  // Error (FormatException ...)
    /// </code>
    /// </example>
    /// <param name="f">The function to execute inside a try/catch.</param>
    let inline catch ([<InlineIfLambda>] f: unit -> 'T) : Result<'T, exn> =
        try
            Ok(f ())
        with e ->
            Error e
