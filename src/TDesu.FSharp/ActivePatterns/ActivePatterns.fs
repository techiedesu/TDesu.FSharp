namespace TDesu.FSharp.ActivePatterns

open System
open TDesu.FSharp
open TDesu.FSharp.Operators

/// <namespacedoc>
///   <summary>Active patterns for parsing and string matching: Parse.Int/Double/Guid/Bool, String.NullOrWhiteSpace/Empty.</summary>
/// </namespacedoc>
///
/// Every partial pattern here returns a struct <c>ValueOption</c> via
/// <c>[&lt;return: Struct&gt;]</c>. A plain partial pattern allocates one
/// <c>FSharpOption</c> per evaluated match arm, which for patterns this small —
/// used in hot parse and validation loops — is the entire cost of the match.
/// The attribute is invisible at the use site: the syntax of a <c>match</c> arm
/// is identical either way.
module String =

    /// Matches when the string has zero length. Does not match null.
    /// <param name="str">The string to test.</param>
    let inline (|Empty|_|) (str: string) = isNotNullRef str && str.Length = 0

    /// Matches when the string is exactly a single whitespace character. Does not match null.
    /// <param name="str">The string to test.</param>
    let inline (|WhiteSpace|_|) (str: string) =
        isNotNullRef str && str.Length = 1 && Char.IsWhiteSpace str[0]

    /// Matches when the string contains only whitespace characters and has length > 1. Does not match null.
    /// <param name="str">The string to test.</param>
    let inline (|WhiteSpaces|_|) (str: string) =
        if isNull str || str.Length <= 1 then
            false
        else
            let mutable i = 0
            let mutable allWhite = true

            while allWhite && i < str.Length do
                if not (Char.IsWhiteSpace str[i]) then
                    allWhite <- false

                i <- i + 1

            allWhite

    /// Matches when the string is empty or contains only whitespace. Does not match null.
    /// <param name="str">The string to test.</param>
    let inline (|EmptyOrWhiteSpace|_|) (str: string) =
        isNotNullRef str && String.IsNullOrWhiteSpace str

    /// Matches when the string is null, empty, or whitespace.
    /// <param name="str">The string to test.</param>
    let inline (|NullOrWhiteSpace|_|) (str: string) = String.isNullOrWhiteSpace str

    /// Matches when the string starts with any of the given values.
    /// <param name="values">The prefixes to check against.</param>
    /// <param name="str">The string to test.</param>
    let inline (|StartsWithAny|_|) (values: string[]) (str: string) = String.startsWithAny values str

/// Parse active patterns — match and extract parsed values from strings.
module Parse =

    /// Matches a string that parses as an int32, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Int|_|) (str: string) =
        Int32.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as an int64, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Int64|_|) (str: string) =
        Int64.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a double, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Double|_|) (str: string) =
        Double.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a decimal, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Decimal|_|) (str: string) =
        Decimal.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a bool, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Bool|_|) (str: string) =
        Boolean.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a Guid, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Guid|_|) (str: string) =
        Guid.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a DateTimeOffset, extracting the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|DateTimeOffset|_|) (str: string) =
        DateTimeOffset.TryParse(str) |> ValueOption.ofCSharpTryPattern

module Ref =

    /// Matches when the value is a null reference.
    /// <param name="obj">The value to test for null.</param>
    let inline (|Null|_|) obj = Object.ReferenceEquals(obj, null)
