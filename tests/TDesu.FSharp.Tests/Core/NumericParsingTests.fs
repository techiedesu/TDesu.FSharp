namespace TDesu.FSharp.Tests

open System
open System.Globalization
open NUnit.Framework
open TDesu.FSharp

/// Distinct from the enum in `NumericsTests.fs`: both files share this namespace, so the name has to
/// be unique even though the type is private to each.
type private Colour =
    | Red = 0
    | Green = 1

[<TestFixture>]
type NumericParsingTests() =

    /// Runs a body with the ambient culture forced to one whose decimal separator is a comma and whose
    /// date format is neither ISO nor US. Restores the previous culture even if the body throws, so a
    /// failure here cannot leak into unrelated fixtures.
    let withCulture (name: string) (body: unit -> unit) =
        let previous = CultureInfo.CurrentCulture

        try
            CultureInfo.CurrentCulture <- CultureInfo(name)
            body ()
        finally
            CultureInfo.CurrentCulture <- previous

    // ── Integers ──

    [<Test>]
    member _.``Int32 tryParse reads a plain integer``() = isSome 42 (Int32.tryParse "42")

    [<Test>]
    member _.``Int32 tryParse reads a negative integer``() = isSome -42 (Int32.tryParse "-42")

    [<Test>]
    member _.``Int32 tryParse tolerates surrounding whitespace``() = isSome 7 (Int32.tryParse "  7  ")

    [<Test>]
    member _.``Int32 tryParse rejects a non-number``() = isNone (Int32.tryParse "x")

    [<Test>]
    member _.``Int32 tryParse rejects the empty string``() = isNone (Int32.tryParse "")

    [<Test>]
    member _.``Int32 tryParse rejects a value past the type's range``() = isNone (Int32.tryParse "2147483648")

    [<Test>]
    member _.``Int64 tryParse reads the type's extremes``() =
        isSome Int64.MaxValue (Int64.tryParse "9223372036854775807")
        isSome Int64.MinValue (Int64.tryParse "-9223372036854775808")

    [<Test>]
    member _.``UInt64 tryParse reads a value beyond Int64``() =
        isSome UInt64.MaxValue (UInt64.tryParse "18446744073709551615")

    [<Test>]
    member _.``UInt32 tryParse rejects a negative number``() = isNone (UInt32.tryParse "-1")

    [<Test>]
    member _.``SByte tryParse covers its range and rejects past it``() =
        isSome -128y (SByte.tryParse "-128")
        isSome 127y (SByte.tryParse "127")
        isNone (SByte.tryParse "128")

    [<Test>]
    member _.``UInt16 tryParse covers its range and rejects past it``() =
        isSome 65535us (UInt16.tryParse "65535")
        isNone (UInt16.tryParse "65536")

    [<Test>]
    member _.``Int16 tryParse covers its range and rejects past it``() =
        isSome -32768s (Int16.tryParse "-32768")
        isNone (Int16.tryParse "32768")

    [<Test>]
    member _.``Byte tryParse covers its range and rejects past it``() =
        isSome 255uy (Byte.tryParse "255")
        isNone (Byte.tryParse "256")

    // ── voption variants ──

    [<Test>]
    member _.``tryParseV yields ValueSome on success``() =
        equals (Int64.tryParseV "42") (ValueSome 42L)

    [<Test>]
    member _.``tryParseV yields ValueNone on failure``() = equals (Int64.tryParseV "x") ValueNone

    [<Test>]
    member _.``tryParseV agrees with tryParse across the numeric modules``() =
        equals (Int32.tryParseV "7") (ValueSome 7)
        equals (UInt32.tryParseV "7") (ValueSome 7u)
        equals (Double.tryParseV "1.5") (ValueSome 1.5)
        equals (Boolean.tryParseV "true") (ValueSome true)
        equals (Int16.tryParseV "bad") ValueNone
        equals (Double.tryParseV "bad") ValueNone

    // ── Floating point ──

    [<Test>]
    member _.``Double tryParse reads a decimal point``() = isSome 1.5 (Double.tryParse "1.5")

    [<Test>]
    member _.``Double tryParse reads exponent notation``() = isSome 1000.0 (Double.tryParse "1e3")

    [<Test>]
    member _.``Double tryParse reads a negative fraction``() = isSome -0.25 (Double.tryParse "-0.25")

    /// The comma is the invariant group separator, so permitting group separators would read the
    /// European spelling of one-and-a-half as fifteen. Rejecting it keeps a mis-formatted number a
    /// parse failure the caller can see instead of a value ten times too large.
    [<Test>]
    member _.``Double tryParse rejects a comma rather than reading it as a group separator``() =
        isNone (Double.tryParse "1,5")
        isNone (Double.tryParse "1,234.5")

    [<Test>]
    member _.``Single tryParse reads a decimal point and rejects a comma``() =
        isSome 2.5f (Single.tryParse "2.5")
        isNone (Single.tryParse "2,5")

    [<Test>]
    member _.``Decimal tryParse reads a decimal point and rejects a comma``() =
        isSome 1.5m (Decimal.tryParse "1.5")
        isNone (Decimal.tryParse "1,5")

    // ── Culture independence ──

    /// The whole point of parsing against the invariant culture: the same string must produce the same
    /// value on a developer's machine and on a server whose locale writes numbers differently. Under
    /// the ambient culture these four assertions fail on a comma-decimal locale.
    [<Test>]
    member _.``float parsing does not depend on the ambient culture``() =
        withCulture
            "ru-RU"
            (fun () ->
                isSome 1.5 (Double.tryParse "1.5")
                isSome 2.5f (Single.tryParse "2.5")
                isSome 1.5m (Decimal.tryParse "1.5")
                isNone (Double.tryParse "1,5")
            )

    [<Test>]
    member _.``date parsing does not depend on the ambient culture``() =
        withCulture
            "ru-RU"
            (fun () ->
                isSome (DateTime(2026, 8, 29)) (DateTime.tryParse "2026-08-29")
                isSome (TimeSpan.FromMinutes 5.0) (TimeSpan.tryParse "00:05:00")
            )

    // ── Dates and times ──

    [<Test>]
    member _.``DateTime tryParse reads an ISO date and time``() =
        isSome (DateTime(2026, 8, 29, 14, 3, 0)) (DateTime.tryParse "2026-08-29 14:03:00")

    [<Test>]
    member _.``DateTime tryParse rejects a non-date``() = isNone (DateTime.tryParse "not a date")

    [<Test>]
    member _.``DateTimeOffset tryParse keeps the stated offset``() =
        let parsed = DateTimeOffset.tryParse "2026-08-29T14:03:00+03:00"
        equals (parsed |> Option.map _.Offset) (Some(TimeSpan.FromHours 3.0))

    [<Test>]
    member _.``TimeSpan tryParse reads days and time``() =
        isSome (TimeSpan(1, 2, 3, 4)) (TimeSpan.tryParse "1.02:03:04")

    [<Test>]
    member _.``TimeSpan tryParse rejects a non-duration``() = isNone (TimeSpan.tryParse "later")

    // ── Remaining modules ──

    [<Test>]
    member _.``Boolean tryParse reads both spellings and ignores case``() =
        isSome true (Boolean.tryParse "true")
        isSome false (Boolean.tryParse "FALSE")
        isNone (Boolean.tryParse "1")

    [<Test>]
    member _.``Guid tryParse reads a canonical guid and rejects a malformed one``() =
        let expected = Guid("d94f9c4e-1f4a-4b1f-9c3e-2b7d5a6e8f01")
        isSome expected (Guid.tryParse "d94f9c4e-1f4a-4b1f-9c3e-2b7d5a6e8f01")
        isNone (Guid.tryParse "d94f9c4e-1f4a-4b1f")

    // ── Composition, which is why `option` is the default shape ──

    [<Test>]
    member _.``tryParse composes with List choose to drop unparseable entries``() =
        equals ([ "1"; "x"; "3" ] |> List.choose Int32.tryParse) [ 1; 3 ]

    // ── Char, Version, Enum ──

    [<Test>]
    member _.``Char tryParse reads a single character``() = isSome 'x' (Char.tryParse "x")

    [<Test>]
    member _.``Char tryParse rejects a longer string rather than truncating it``() =
        isNone (Char.tryParse "xy")
        isNone (Char.tryParse "")

    [<Test>]
    member _.``Version tryParse reads a dotted version``() =
        isSome (Version(1, 2, 3)) (Version.tryParse "1.2.3")

    [<Test>]
    member _.``Version tryParse rejects a non-version``() = isNone (Version.tryParse "1.x")

    [<Test>]
    member _.``Enum tryParse reads a case by name``() =
        isSome Colour.Green (Enum.tryParse<Colour> "Green")

    [<Test>]
    member _.``Enum tryParse is case-sensitive but tryParseIgnoreCase is not``() =
        isNone (Enum.tryParse<Colour> "green")
        isSome Colour.Green (Enum.tryParseIgnoreCase<Colour> "green")

    [<Test>]
    member _.``Enum tryParse rejects a name that is not a case``() = isNone (Enum.tryParse<Colour> "Mauve")

    [<Test>]
    member _.``Enum tryParseV yields ValueSome and ValueNone``() =
        equals (Enum.tryParseV<Colour> "Red") (ValueSome Colour.Red)
        equals (Enum.tryParseV<Colour> "Mauve") ValueNone
        equals (Enum.tryParseVIgnoreCase<Colour> "RED") (ValueSome Colour.Red)
