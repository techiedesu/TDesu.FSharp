namespace TDesu.FSharp

open System
open System.Globalization

// Every module here offers the same pair: `tryParse` returning an `option` and `tryParseV` returning a
// `voption`. The pairing is not a preference, it is the convention this library already keeps elsewhere
// — `Tasks/TaskOption.fs` sits beside `Tasks/TaskVOption.fs` — and the parsers were the one place that
// offered only half of it.
//
// Which one to reach for: `option` composes with `List.choose`, `Option.map` and the rest of FSharp.Core,
// so it is the right default and the one a caller should use unless it has a reason. `tryParseV` avoids
// the allocation of a `Some` box, which is worth having in a loop that parses millions of fields and is
// noise anywhere else — and it does not compose with `List.choose`, so converting it back at a boundary
// gives the allocation straight back.
//
// On culture. Integer parsing is culture-insensitive for the input these functions actually receive:
// the default `NumberStyles.Integer` permits a sign and surrounding whitespace, and nothing else that a
// locale can redefine. Floating point and the date types are a different matter — the decimal separator
// and the date format are exactly what a locale changes — so those parse against
// `CultureInfo.InvariantCulture` here rather than the ambient one.
//
// That is a deliberate change from parsing with the current culture, and the reason is that a parser in
// a general-purpose library overwhelmingly receives machine-generated text: a database column, a JSON
// number, a protocol field, a config file. Such text is invariant by construction, so a current-culture
// default means the same string parses on the developer's machine and returns `None` on a server whose
// locale writes `1,5` — a failure that depends on the host and appears as missing data rather than as an
// error. A caller that genuinely wants a human's locale wants `Double.TryParse` with an explicit
// provider, and should say so at the call site rather than inherit it by accident.

[<RequireQualifiedAccess>]
module SByte =
    /// Parses a string as sbyte, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        SByte.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as sbyte, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        SByte.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Byte =
    /// Parses a string as byte, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Byte.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as byte, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Byte.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Int16 =
    /// Parses a string as int16, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Int16.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as int16, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Int16.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module UInt16 =
    /// Parses a string as uint16, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        UInt16.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as uint16, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        UInt16.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Int32 =
    /// Parses a string as int32, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Int32.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as int32, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Int32.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module UInt32 =
    /// Parses a string as uint32, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        UInt32.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as uint32, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        UInt32.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Int64 =
    /// Parses a string as int64, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Int64.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as int64, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Int64.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module UInt64 =
    /// Parses a string as uint64, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        UInt64.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as uint64, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        UInt64.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Single =
    // No `AllowThousands`: the invariant group separator is a comma, so allowing it would parse the
    // European spelling "1,5" as 15.0 — silently ten times the intended value. Machine-generated
    // numbers carry no group separators, so the styles that would corrupt them are simply refused.
    [<Literal>]
    let private styles = NumberStyles.Float

    /// Parses a string as float32 against the invariant culture, returning <c>Some(value)</c> or
    /// <c>None</c>. See the note at the top of this file on why the culture is not the ambient one.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Single.TryParse(str, styles, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as float32 against the invariant culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Single.TryParse(str, styles, CultureInfo.InvariantCulture)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Double =
    [<Literal>]
    let private styles = NumberStyles.Float

    /// Parses a string as double against the invariant culture, returning <c>Some(value)</c> or
    /// <c>None</c>. See the note at the top of this file on why the culture is not the ambient one.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Double.TryParse(str, styles, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as double against the invariant culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Double.TryParse(str, styles, CultureInfo.InvariantCulture)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Decimal =
    [<Literal>]
    let private styles = NumberStyles.Float

    /// Parses a string as decimal against the invariant culture, returning <c>Some(value)</c> or
    /// <c>None</c>. See the note at the top of this file on why the culture is not the ambient one.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Decimal.TryParse(str, styles, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as decimal against the invariant culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Decimal.TryParse(str, styles, CultureInfo.InvariantCulture)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Boolean =
    /// Parses a string as bool, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Boolean.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as bool, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Boolean.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Guid =
    /// Parses a string as Guid, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Guid.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as Guid, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Guid.TryParse(str) |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module DateTime =
    /// Parses a string as DateTime against the invariant culture, returning <c>Some(value)</c> or
    /// <c>None</c>. See the note at the top of this file on why the culture is not the ambient one.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> Option.ofCSharpTryPattern

    /// Parses a string as DateTime against the invariant culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module DateTimeOffset =
    /// Parses a string as DateTimeOffset against the invariant culture, returning <c>Some(value)</c> or
    /// <c>None</c>. See the note at the top of this file on why the culture is not the ambient one.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> Option.ofCSharpTryPattern

    /// Parses a string as DateTimeOffset against the invariant culture, returning
    /// <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module TimeSpan =
    /// Parses a string as TimeSpan against the invariant culture, returning <c>Some(value)</c> or
    /// <c>None</c>. See the note at the top of this file on why the culture is not the ambient one.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        TimeSpan.TryParse(str, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as TimeSpan against the invariant culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        TimeSpan.TryParse(str, CultureInfo.InvariantCulture)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Version =
    /// Parses a dotted version string, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Version.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a dotted version string, returning <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Version.TryParse(str) |> ValueOption.ofCSharpTryPattern
