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
// locale can redefine, so those modules offer one parser and no variants. Floating point and the date
// types are a different matter — the decimal separator and the date format are exactly what a locale
// redefines — so each of those offers both, named the way this library already names the distinction in
// `Char.toUpper`/`toUpperInv` and `String.toLower`/`toLowerInv`: the bare name follows the ambient
// culture, the `Inv` suffix pins the invariant one.
//
// Which to reach for is not a toss-up. Text that came from a machine — a database column, a JSON
// number, a protocol field, a config value — is invariant by construction, and parsing it with
// `tryParse` makes the result depend on the host: `Double.tryParse "1.5"` is `None` on every locale
// whose decimal separator is a comma, which is most of Europe, and the failure surfaces as missing data
// rather than as an error. Such a caller wants `tryParseInv`. `tryParse` is for text a person typed in
// their own locale, which is the only case where the ambient culture is the question being asked.
//
// The `Inv` parsers also refuse group separators, which the ambient ones inherit from the BCL default.
// Under the invariant culture the group separator is a comma, so allowing it would read `"1,5"` as
// `15.0` — ten times the intended value, silently. For machine text a group separator is not a thing
// that legitimately occurs, so refusing it costs nothing and removes the corruption.

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
    [<Literal>]
    let private invStyles = NumberStyles.Float

    /// Parses a string as float32 using the ambient culture, returning <c>Some(value)</c> or
    /// <c>None</c>. For text that came from a machine rather than from a person, use
    /// <c>tryParseInv</c> — see the note at the top of this file.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Single.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as float32 using the ambient culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Single.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Parses a string as float32 against the invariant culture, refusing group separators. This is
    /// the one to use for machine-generated text, whose meaning must not depend on the host's locale.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInv (str: string) =
        Single.TryParse(str, invStyles, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as float32 against the invariant culture, refusing group separators, returning
    /// <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInvV (str: string) =
        Single.TryParse(str, invStyles, CultureInfo.InvariantCulture)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Double =
    [<Literal>]
    let private invStyles = NumberStyles.Float

    /// Parses a string as double using the ambient culture, returning <c>Some(value)</c> or
    /// <c>None</c>. For text that came from a machine rather than from a person, use
    /// <c>tryParseInv</c> — see the note at the top of this file.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Double.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as double using the ambient culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Double.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Parses a string as double against the invariant culture, refusing group separators. This is
    /// the one to use for machine-generated text, whose meaning must not depend on the host's locale.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInv (str: string) =
        Double.TryParse(str, invStyles, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as double against the invariant culture, refusing group separators, returning
    /// <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInvV (str: string) =
        Double.TryParse(str, invStyles, CultureInfo.InvariantCulture)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Decimal =
    [<Literal>]
    let private invStyles = NumberStyles.Float

    /// Parses a string as decimal using the ambient culture, returning <c>Some(value)</c> or
    /// <c>None</c>. For text that came from a machine rather than from a person, use
    /// <c>tryParseInv</c> — see the note at the top of this file.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Decimal.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as decimal using the ambient culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        Decimal.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Parses a string as decimal against the invariant culture, refusing group separators. This is
    /// the one to use for machine-generated text, whose meaning must not depend on the host's locale.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInv (str: string) =
        Decimal.TryParse(str, invStyles, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as decimal against the invariant culture, refusing group separators, returning
    /// <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInvV (str: string) =
        Decimal.TryParse(str, invStyles, CultureInfo.InvariantCulture)
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
    /// Parses a string as DateTime using the ambient culture, returning <c>Some(value)</c> or
    /// <c>None</c>. For text that came from a machine rather than from a person, use
    /// <c>tryParseInv</c> — see the note at the top of this file.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        DateTime.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as DateTime using the ambient culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        DateTime.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Parses a string as DateTime against the invariant culture. This is the one to use for
    /// machine-generated text, whose meaning must not depend on the host's locale.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInv (str: string) =
        DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> Option.ofCSharpTryPattern

    /// Parses a string as DateTime against the invariant culture, returning <c>ValueSome(value)</c>
    /// or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInvV (str: string) =
        DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module DateTimeOffset =
    /// Parses a string as DateTimeOffset using the ambient culture, returning <c>Some(value)</c> or
    /// <c>None</c>. For text that came from a machine rather than from a person, use
    /// <c>tryParseInv</c> — see the note at the top of this file.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        DateTimeOffset.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as DateTimeOffset using the ambient culture, returning <c>ValueSome(value)</c>
    /// or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        DateTimeOffset.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Parses a string as DateTimeOffset against the invariant culture. This is the one to use for
    /// machine-generated text, whose meaning must not depend on the host's locale.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInv (str: string) =
        DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> Option.ofCSharpTryPattern

    /// Parses a string as DateTimeOffset against the invariant culture, returning
    /// <c>ValueSome(value)</c> or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInvV (str: string) =
        DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None)
        |> ValueOption.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module TimeSpan =
    /// Parses a string as TimeSpan using the ambient culture, returning <c>Some(value)</c> or
    /// <c>None</c>. For text that came from a machine rather than from a person, use
    /// <c>tryParseInv</c> — see the note at the top of this file.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        TimeSpan.TryParse(str) |> Option.ofCSharpTryPattern

    /// Parses a string as TimeSpan using the ambient culture, returning <c>ValueSome(value)</c> or
    /// <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseV (str: string) =
        TimeSpan.TryParse(str) |> ValueOption.ofCSharpTryPattern

    /// Parses a string as TimeSpan against the invariant culture. This is the one to use for
    /// machine-generated text, whose meaning must not depend on the host's locale.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInv (str: string) =
        TimeSpan.TryParse(str, CultureInfo.InvariantCulture)
        |> Option.ofCSharpTryPattern

    /// Parses a string as TimeSpan against the invariant culture, returning <c>ValueSome(value)</c>
    /// or <c>ValueNone</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParseInvV (str: string) =
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
