namespace TDesu.FSharp

open TDesu.FSharp.Operators

[<RequireQualifiedAccess>]
module ValueOption =
    /// Converts a C# TryXxx <c>(bool * value)</c> tuple to a <see cref="ValueOption{T}"/>.
    /// <param name="status">The success flag from the TryXxx method.</param>
    /// <param name="value">The output value from the TryXxx method.</param>
    let inline ofCSharpTryPattern (status, value) = if status then ValueSome value else ValueNone

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
    /// <param name="value">The option to act on.</param>
    let inline iterIgnore ([<InlineIfLambda>] f: 'T -> 'TResult) (value: 'T option) =
        match value with
        | None -> ()
        | Some v -> %f v

    /// Returns true if the option contains true; false otherwise.
    /// <param name="v">The boolean option to check.</param>
    let inline isTrue (v: bool option) =
        Option.defaultValue false v

    /// <summary>
    /// Returns <c>None</c> if the string is null/empty/whitespace, otherwise <c>Some(s)</c>.
    /// </summary>
    /// <param name="s">The string to convert.</param>
    let inline ofString (s: string | null) =
        if System.String.IsNullOrWhiteSpace(s) then None
        else Some s

    /// <summary>
    /// Converts <c>Some</c> to <c>Ok</c>, <c>None</c> to <c>Error</c> with the given error value.
    /// </summary>
    /// <param name="error">The error value to use when the option is None.</param>
    /// <param name="opt">The option to convert.</param>
    let inline toResult error opt =
        match opt with
        | Some v -> Ok v
        | None -> Error error

    /// <summary>
    /// Applies a side-effect on <c>Some</c> and returns the option unchanged.
    /// </summary>
    /// <param name="f">The side-effect function to apply.</param>
    /// <param name="opt">The option to inspect.</param>
    let inline tee ([<InlineIfLambda>] f: 'T -> unit) (opt: 'T option) =
        match opt with
        | Some v -> f v; opt
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
    let inline map3 ([<InlineIfLambda>] f: 'T1 -> 'T2 -> 'T3 -> 'TResult) (o1: 'T1 option) (o2: 'T2 option) (o3: 'T3 option) =
        match o1, o2, o3 with
        | Some a, Some b, Some c -> Some(f a b c)
        | _ -> None

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

    /// Transforms the Ok value with f, passing Error through unchanged.
    /// <param name="f">The mapping function to apply to the Ok value.</param>
    /// <param name="r">The result to transform.</param>
    let inline map ([<InlineIfLambda>] f) r =
        match r with
        | Ok v -> Ok(f v)
        | Error e -> Error e

    /// Chains a Result-returning function on Ok; short-circuits on Error.
    /// <param name="f">The function returning a new Result.</param>
    /// <param name="r">The result to bind over.</param>
    let inline bind ([<InlineIfLambda>] f) r =
        match r with
        | Ok v -> f v
        | Error e -> Error e

    /// Transforms the Error value with f, passing Ok through unchanged.
    /// <param name="f">The mapping function to apply to the Error value.</param>
    /// <param name="r">The result to transform.</param>
    let inline mapError ([<InlineIfLambda>] f) r =
        match r with
        | Ok v -> Ok v
        | Error e -> Error(f e)

    /// Returns true if the result is Ok.
    /// <param name="r">The result to check.</param>
    let inline isOk r =
        match r with
        | Ok _ -> true
        | Error _ -> false

    /// Returns true if the result is Error.
    /// <param name="r">The result to check.</param>
    let inline isError r =
        match r with
        | Ok _ -> false
        | Error _ -> true

    /// Extracts the Ok value, or computes a fallback from the Error.
    /// <param name="f">The function to compute a fallback from the Error value.</param>
    /// <param name="r">The result to extract from.</param>
    let inline valueOr ([<InlineIfLambda>] f: 'TError -> 'T) (r: Result<'T, 'TError>) =
        match r with
        | Ok v -> v
        | Error e -> f e

    /// Returns the Ok value, or the given default.
    /// <param name="def">The default value to return on Error.</param>
    /// <param name="r">The result to extract from.</param>
    let inline defaultValue (def: 'T) (r: Result<'T, _>) =
        match r with
        | Ok v -> v
        | Error _ -> def

    /// Returns the Ok value, or computes a default.
    /// <param name="f">The function to compute a default value on Error.</param>
    /// <param name="r">The result to extract from.</param>
    let inline defaultWith ([<InlineIfLambda>] f: unit -> 'T) (r: Result<'T, _>) =
        match r with
        | Ok v -> v
        | Error _ -> f ()

    /// Returns the result if Ok, otherwise the given fallback result.
    /// <param name="ifError">The fallback result to use on Error.</param>
    /// <param name="r">The result to evaluate.</param>
    let inline orElse (ifError: Result<'T, 'TError>) (r: Result<'T, 'TError>) =
        match r with
        | Ok _ -> r
        | Error _ -> ifError

    /// Returns the result if Ok, otherwise computes a fallback from the Error.
    /// <param name="f">The function to compute a fallback result from the Error value.</param>
    /// <param name="r">The result to evaluate.</param>
    let inline orElseWith ([<InlineIfLambda>] f: 'TError -> Result<'T, 'TError2>) (r: Result<'T, 'TError>) =
        match r with
        | Ok v -> Ok v
        | Error e -> f e

    /// Applies a side-effect on Ok and returns the result unchanged.
    /// <param name="f">The side-effect function to apply on Ok.</param>
    /// <param name="r">The result to inspect.</param>
    let inline tee ([<InlineIfLambda>] f: 'T -> unit) (r: Result<'T, 'TError>) =
        match r with
        | Ok v -> f v; r
        | Error _ -> r

    /// Applies a side-effect on Error and returns the result unchanged.
    /// <param name="f">The side-effect function to apply on Error.</param>
    /// <param name="r">The result to inspect.</param>
    let inline teeError ([<InlineIfLambda>] f: 'TError -> unit) (r: Result<'T, 'TError>) =
        match r with
        | Ok _ -> r
        | Error e -> f e; r

    /// Converts an Option to a Result: Some becomes Ok, None becomes Error.
    /// <param name="error">The error value to use when the option is None.</param>
    /// <param name="opt">The option to convert.</param>
    let inline ofOption (error: 'TError) (opt: 'T option) : Result<'T, 'TError> =
        match opt with
        | Some v -> Ok v
        | None -> Error error

    /// Converts Result to Option: Ok becomes Some, Error becomes None.
    /// <param name="r">The result to convert.</param>
    let inline toOption (r: Result<'T, _>) =
        match r with
        | Ok v -> Some v
        | Error _ -> None

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
    let inline requireTrue (error: 'TError) (value: bool) =
        if value then Ok() else Error error

    /// Returns Ok(()) if false, Error(error) if true.
    /// <param name="error">The error value to use when the condition is true.</param>
    /// <param name="value">The boolean condition to check.</param>
    let inline requireFalse (error: 'TError) (value: bool) =
        if not value then Ok() else Error error

    /// Returns Ok(value) if not null, Error(error) if null.
    /// <param name="error">The error value to use when the value is null.</param>
    /// <param name="value">The value to check for null.</param>
    let inline requireNotNull (error: 'TError) (value: 'T) =
        if obj.ReferenceEquals(value, null) then Error error else Ok value

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
        try Ok(f ()) with e -> Error e
