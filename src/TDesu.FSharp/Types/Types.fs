namespace TDesu.FSharp.Types

/// Standard API response wrapper. Bridges HTTP responses and F# Result.
[<RequireQualifiedAccess>]
module ApiResponse =
    /// API response with data on success, error on failure.
    type T<'TData, 'TError> = {
        Success: bool
        Data: 'TData option
        Error: 'TError option
    }

    /// Creates a success response.
    /// <param name="data">The data payload for the success response.</param>
    let inline ok (data: 'TData) : T<'TData, 'TError> =
        { Success = true; Data = Some data; Error = None }

    /// Creates an error response.
    /// <param name="err">The error value for the failure response.</param>
    let inline error (err: 'TError) : T<'TData, 'TError> =
        { Success = false; Data = None; Error = Some err }

    /// Converts a Result to an API response.
    /// <param name="result">The Result value to convert.</param>
    let ofResult (result: Result<'TData, 'TError>) : T<'TData, 'TError> =
        match result with
        | Ok data -> ok data
        | Error err -> error err

    /// <summary>
    /// Converts an API response to a <c>Result</c>.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">When <c>Success=true</c> but <c>Data</c> is <c>None</c>.</exception>
    /// <param name="response">The API response to convert.</param>
    let toResult (response: T<'TData, 'TError>) : Result<'TData, 'TError> =
        if response.Success then
            match response.Data with
            | Some d -> Ok d
            | None -> invalidOp "API success=true but data is missing"
        else
            match response.Error with
            | Some e -> Error e
            | None -> invalidOp "API success=false but error is missing"

/// A string guaranteed to be non-null and non-empty/whitespace.
type NonEmptyString = private NonEmptyString of string

/// Error cases for NonEmptyString creation.
[<RequireQualifiedAccess>]
type NonEmptyStringError =
    | Null
    | Empty

/// Operations on NonEmptyString.
[<RequireQualifiedAccess>]
module NonEmptyString =

    /// Create a NonEmptyString from a raw string. Returns Error if null or whitespace.
    /// <param name="s">The raw string to validate and wrap.</param>
    let create (s: string) : Result<NonEmptyString, NonEmptyStringError> =
        if isNull s then
            Error NonEmptyStringError.Null
        elif System.String.IsNullOrWhiteSpace s then
            Error NonEmptyStringError.Empty
        else
            Ok (NonEmptyString s)

    /// Create a NonEmptyString, throwing if invalid.
    /// <param name="s">The raw string to validate and wrap.</param>
    let createOrFail (s: string) : NonEmptyString =
        match create s with
        | Ok v -> v
        | Error NonEmptyStringError.Null -> nullArg (nameof s)
        | Error NonEmptyStringError.Empty -> invalidArg (nameof s) "String must not be empty or whitespace"

    /// Extract the underlying string value.
    /// <param name="s">The NonEmptyString to unwrap.</param>
    let value (NonEmptyString s) = s

    /// String length.
    /// <param name="s">The NonEmptyString to measure.</param>
    let length (NonEmptyString s) = s.Length
