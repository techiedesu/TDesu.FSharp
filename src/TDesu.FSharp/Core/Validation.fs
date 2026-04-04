namespace TDesu.FSharp

/// <summary>
/// Applicative validation: collects ALL errors instead of short-circuiting.
/// Use <c>and!</c> in the <c>validation { }</c> CE to run validators in parallel and collect errors.
/// </summary>
/// <example>
/// <code>
/// let validateName name =
///     if String.IsNullOrWhiteSpace name then Validation.error "Name required"
///     else Validation.ok name
///
/// let validateAge age =
///     if age &lt; 0 || age &gt; 150 then Validation.error "Invalid age"
///     else Validation.ok age
///
/// validation {
///     let! name = validateName input.Name
///     and! age = validateAge input.Age
///     return { Name = name; Age = age }
/// }
/// // If both fail: Error ["Name required"; "Invalid age"]
/// </code>
/// </example>
[<RequireQualifiedAccess>]
type Validation<'TValue, 'TError> =
    | Ok of 'TValue
    | Error of 'TError list

/// Combinators for Validation.
[<RequireQualifiedAccess>]
module Validation =

    /// Create a valid value.
    /// <param name="value">The value to wrap.</param>
    let inline ok (value: 'TValue) : Validation<'TValue, 'TError> =
        Validation.Ok value

    /// Create a single-error validation.
    /// <param name="err">The error to wrap.</param>
    let inline error (err: 'TError) : Validation<'TValue, 'TError> =
        Validation.Error [ err ]

    /// Create a multi-error validation.
    /// <param name="errs">The list of errors.</param>
    let inline errors (errs: 'TError list) : Validation<'TValue, 'TError> =
        Validation.Error errs

    /// Map over the value, leaving errors unchanged.
    /// <param name="f">The mapping function.</param>
    /// <param name="v">The validation to map over.</param>
    let map (f: 'TValue -> 'TResult) (v: Validation<'TValue, 'TError>) : Validation<'TResult, 'TError> =
        match v with
        | Validation.Ok x -> Validation.Ok(f x)
        | Validation.Error errs -> Validation.Error errs

    /// Map over the errors, leaving the value unchanged.
    /// <param name="f">The error mapping function.</param>
    /// <param name="v">The validation to map over.</param>
    let mapError (f: 'TError -> 'TError2) (v: Validation<'TValue, 'TError>) : Validation<'TValue, 'TError2> =
        match v with
        | Validation.Ok x -> Validation.Ok x
        | Validation.Error errs -> Validation.Error(List.map f errs)

    /// Bind (monadic, short-circuits on first Error — use <c>and!</c> for applicative).
    /// <param name="f">The binding function.</param>
    /// <param name="v">The validation to bind.</param>
    let bind (f: 'TValue -> Validation<'TResult, 'TError>) (v: Validation<'TValue, 'TError>) : Validation<'TResult, 'TError> =
        match v with
        | Validation.Ok x -> f x
        | Validation.Error errs -> Validation.Error errs

    /// Applicative apply — combines errors from both sides.
    /// <param name="fV">A validation containing a function.</param>
    /// <param name="xV">A validation containing a value.</param>
    let apply (fV: Validation<'TValue -> 'TResult, 'TError>) (xV: Validation<'TValue, 'TError>) : Validation<'TResult, 'TError> =
        match fV, xV with
        | Validation.Ok f, Validation.Ok x -> Validation.Ok(f x)
        | Validation.Error e1, Validation.Error e2 -> Validation.Error(e1 @ e2)
        | Validation.Error e, _ | _, Validation.Error e -> Validation.Error e

    /// Convert a Result to a Validation.
    /// <param name="r">The Result to convert.</param>
    let ofResult (r: Result<'TValue, 'TError>) : Validation<'TValue, 'TError> =
        match r with
        | Result.Ok v -> Validation.Ok v
        | Result.Error e -> Validation.Error [ e ]

    /// Convert a Validation to a Result.
    /// <param name="v">The Validation to convert.</param>
    let toResult (v: Validation<'TValue, 'TError>) : Result<'TValue, 'TError list> =
        match v with
        | Validation.Ok x -> Result.Ok x
        | Validation.Error errs -> Result.Error errs

    /// Returns true if the validation is Ok.
    /// <param name="v">The validation to check.</param>
    let inline isOk (v: Validation<'TValue, 'TError>) =
        match v with Validation.Ok _ -> true | _ -> false

    /// Returns true if the validation is Error.
    /// <param name="v">The validation to check.</param>
    let inline isError (v: Validation<'TValue, 'TError>) =
        match v with Validation.Error _ -> true | _ -> false

    /// Extract the value, throwing if Error.
    /// <param name="v">The validation to unwrap.</param>
    let valueOrFail (v: Validation<'TValue, 'TError>) : 'TValue =
        match v with
        | Validation.Ok x -> x
        | Validation.Error errs -> invalidOp $"Validation failed with {List.length errs} error(s)"

    /// Extract the value or use a default.
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="v">The validation to unwrap.</param>
    let defaultValue (defaultValue: 'TValue) (v: Validation<'TValue, 'TError>) : 'TValue =
        match v with
        | Validation.Ok x -> x
        | Validation.Error _ -> defaultValue

/// <summary>
/// Computation expression builder for Validation workflows.
/// Use <c>let!</c> for monadic (short-circuit) and <c>and!</c> for applicative (collect errors).
/// </summary>
[<Sealed>]
type ValidationBuilder() =
    member inline _.Return(value: 'TValue) : Validation<'TValue, 'TError> =
        Validation.Ok value

    member inline _.ReturnFrom(v: Validation<'TValue, 'TError>) : Validation<'TValue, 'TError> = v

    member inline _.Bind(v: Validation<'TValue, 'TError>, f: 'TValue -> Validation<'TResult, 'TError>) : Validation<'TResult, 'TError> =
        Validation.bind f v

    member inline _.BindReturn(v: Validation<'TValue, 'TError>, [<InlineIfLambda>] f: 'TValue -> 'TResult) : Validation<'TResult, 'TError> =
        Validation.map f v

    member inline _.MergeSources(v1: Validation<'T1, 'TError>, v2: Validation<'T2, 'TError>) : Validation<'T1 * 'T2, 'TError> =
        match v1, v2 with
        | Validation.Ok a, Validation.Ok b -> Validation.Ok(a, b)
        | Validation.Error e1, Validation.Error e2 -> Validation.Error(e1 @ e2)
        | Validation.Error e, _ | _, Validation.Error e -> Validation.Error e

    member inline _.Zero() : Validation<unit, 'TError> = Validation.Ok()

    member inline _.Delay([<InlineIfLambda>] f: unit -> Validation<'TValue, 'TError>) = f

    member inline _.Run([<InlineIfLambda>] f: unit -> Validation<'TValue, 'TError>) = f()

/// Computation expression instance for Validation workflows: <c>validation { ... }</c>.
[<AutoOpen>]
module ValidationCE =
    let validation = ValidationBuilder()
