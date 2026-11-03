namespace TDesu.FSharp.ActivePatterns

open System
open TDesu.FSharp
open TDesu.FSharp.Operators

/// <namespacedoc>
///   <summary>Active patterns for parsing and string matching: Parse.Int/Double/Guid/Bool, String.NullOrWhiteSpace/Empty.</summary>
/// </namespacedoc>
///
/// Every pattern in this module returns a plain <c>bool</c>, not an <c>option</c> or
/// <c>voption</c> — a boolean-returning partial active pattern already allocates nothing per
/// evaluated match arm, so there is no <c>[&lt;return: Struct&gt;]</c> to add here. That
/// attribute is what the <see cref="Parse"/> module below carries instead, since its patterns
/// extract a parsed value out through <c>ValueSome</c>.
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

/// <summary>
/// Parse active patterns — match and extract parsed values from strings. Every pattern here
/// returns a struct <c>ValueOption</c> via <c>[&lt;return: Struct&gt;]</c>, so a match arm
/// allocates nothing.
/// </summary>
module Parse =

    /// Matches a string that parses as an int32, extracting the value, using the ambient culture.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Int|_|) (str: string) =
        Int32.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as an int64, extracting the value, using the ambient culture.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Int64|_|) (str: string) =
        Int64.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a double, extracting the value, using the ambient
    /// culture's decimal separator — e.g. "3.14" fails to match on a comma-decimal locale. Use
    /// <see cref="DoubleInv"/> for culture-invariant (machine-generated) text.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Double|_|) (str: string) =
        Double.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a double against the invariant culture, refusing group
    /// separators — the one to use for machine-generated text. Extracts the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|DoubleInv|_|) (str: string) = Double.tryParseInvV str

    /// Matches a string that parses as a float32 against the invariant culture, refusing group
    /// separators — the one to use for machine-generated text. Extracts the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|SingleInv|_|) (str: string) = Single.tryParseInvV str

    /// Matches a string that parses as a decimal, extracting the value, using the ambient
    /// culture's decimal separator. Use <see cref="DecimalInv"/> for culture-invariant
    /// (machine-generated) text.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Decimal|_|) (str: string) =
        Decimal.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a decimal against the invariant culture, refusing group
    /// separators — the one to use for machine-generated text. Extracts the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|DecimalInv|_|) (str: string) = Decimal.tryParseInvV str

    /// Matches a string that parses as a bool, extracting the value. <c>Boolean.TryParse</c>
    /// takes no culture, so this is already culture-invariant.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Bool|_|) (str: string) =
        Boolean.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a Guid, extracting the value. <c>Guid.TryParse</c> takes
    /// no culture, so this is already culture-invariant.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|Guid|_|) (str: string) =
        Guid.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a DateTime against the invariant culture — the one to use
    /// for machine-generated text. Extracts the value. There is no ambient-culture
    /// <c>DateTime</c> pattern here; reach for <see cref="DateTimeOffset"/> when the ambient
    /// culture is genuinely what is wanted.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|DateTimeInv|_|) (str: string) = DateTime.tryParseInvV str

    /// Matches a string that parses as a DateTimeOffset, extracting the value, using the ambient
    /// culture to interpret the date. Use <see cref="DateTimeOffsetInv"/> for culture-invariant
    /// (machine-generated) text.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|DateTimeOffset|_|) (str: string) =
        DateTimeOffset.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Matches a string that parses as a DateTimeOffset against the invariant culture — the one
    /// to use for machine-generated text. Extracts the value.
    /// <param name="str">The string to parse.</param>
    [<return: Struct>]
    let inline (|DateTimeOffsetInv|_|) (str: string) = DateTimeOffset.tryParseInvV str

module Ref =

    /// Matches when the value is a null reference.
    /// <param name="obj">The value to test for null.</param>
    let inline (|Null|_|) obj = Object.ReferenceEquals(obj, null)
