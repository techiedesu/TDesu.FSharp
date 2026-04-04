namespace TDesu.FSharp.ActivePatterns

open System
open TDesu.FSharp

/// <namespacedoc>
///   <summary>Active patterns for parsing and string matching: Parse.Int/Double/Guid/Bool, String.NullOrWhiteSpace/Empty.</summary>
/// </namespacedoc>
module String =

    /// Matches when the string has zero length. Does not match null.
    /// <param name="str">The string to test.</param>
    let (|Empty|_|) (str: string) =
        if not (isNull str) && str.Length = 0 then Some() else None

    /// Matches when the string is exactly a single whitespace character. Does not match null.
    /// <param name="str">The string to test.</param>
    let (|WhiteSpace|_|) (str: string) =
        if not (isNull str) && str.Length = 1 && Char.IsWhiteSpace str[0] then Some() else None

    /// Matches when the string contains only whitespace characters and has length > 1. Does not match null.
    /// <param name="str">The string to test.</param>
    let (|WhiteSpaces|_|) (str: string) =
        if isNull str || str.Length <= 1 then None
        else
            let mutable i = 0
            let mutable allWhite = true
            while allWhite && i < str.Length do
                if not (Char.IsWhiteSpace str[i]) then allWhite <- false
                i <- i + 1
            if allWhite then Some() else None

    /// Matches when the string is empty or contains only whitespace. Does not match null.
    /// <param name="str">The string to test.</param>
    let (|EmptyOrWhiteSpace|_|) (str: string) =
        if not (isNull str) && System.String.IsNullOrWhiteSpace str then Some() else None

    /// Matches when the string is null, empty, or whitespace.
    /// <param name="str">The string to test.</param>
    let (|NullOrWhiteSpace|_|) (str: string) =
        if String.isNullOrWhiteSpace str then Some() else None

    /// Matches when the string starts with any of the given values.
    /// <param name="values">The prefixes to check against.</param>
    /// <param name="str">The string to test.</param>
    let (|StartsWithAny|_|) (values: string[]) (str: string) =
        if String.startsWithAny values str then Some() else None

/// Parse active patterns — match and extract parsed values from strings.
module Parse =

    /// Matches a string that parses as an int32, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|Int|_|) (str: string) =
        match Int32.TryParse(str) with
        | true, v -> Some v
        | _ -> None

    /// Matches a string that parses as an int64, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|Int64|_|) (str: string) =
        match Int64.TryParse(str) with
        | true, v -> Some v
        | _ -> None

    /// Matches a string that parses as a double, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|Double|_|) (str: string) =
        match Double.TryParse(str) with
        | true, v -> Some v
        | _ -> None

    /// Matches a string that parses as a decimal, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|Decimal|_|) (str: string) =
        match Decimal.TryParse(str) with
        | true, v -> Some v
        | _ -> None

    /// Matches a string that parses as a bool, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|Bool|_|) (str: string) =
        match Boolean.TryParse(str) with
        | true, v -> Some v
        | _ -> None

    /// Matches a string that parses as a Guid, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|Guid|_|) (str: string) =
        match Guid.TryParse(str) with
        | true, v -> Some v
        | _ -> None

    /// Matches a string that parses as a DateTimeOffset, extracting the value.
    /// <param name="str">The string to parse.</param>
    let (|DateTimeOffset|_|) (str: string) =
        match DateTimeOffset.TryParse(str) with
        | true, v -> Some v
        | _ -> None

module Ref =

    /// Matches when the value is a null reference.
    /// <param name="obj">The value to test for null.</param>
    let (|Null|_|) obj =
        if Object.ReferenceEquals(obj, null) then Some() else None
