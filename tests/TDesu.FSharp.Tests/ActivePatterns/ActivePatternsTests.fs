namespace TDesu.FSharp.Tests

open System.Globalization
open System.Threading
open NUnit.Framework
open TDesu.FSharp.ActivePatterns

[<TestFixture>]
type ParsePatternTests() =

    [<Test>]
    member _.``Int matches and extracts an int32``() =
        match "42" with
        | Parse.Int v -> equals v 42
        | _ -> Assert.Fail("expected a match")

    [<Test>]
    member _.``Int does not match a non-integer string``() =
        match "abc" with
        | Parse.Int _ -> Assert.Fail("expected no match")
        | _ -> ()

    [<Test>]
    member _.``Int64 matches and extracts an int64``() =
        match "9223372036854775807" with
        | Parse.Int64 v -> equals v System.Int64.MaxValue
        | _ -> Assert.Fail("expected a match")

    [<Test>]
    member _.``Bool matches and extracts a bool, culture-invariant since Boolean.TryParse takes none``() =
        match "true" with
        | Parse.Bool v -> isTrue v
        | _ -> Assert.Fail("expected a match")

    [<Test>]
    member _.``Guid matches and extracts a Guid, culture-invariant since Guid.TryParse takes none``() =
        let g = System.Guid.NewGuid()

        match g.ToString() with
        | Parse.Guid v -> equals v g
        | _ -> Assert.Fail("expected a match")

    [<Test>]
    member _.``DateTimeInv matches an invariant-culture date regardless of the ambient culture``() =
        let original = Thread.CurrentThread.CurrentCulture

        try
            Thread.CurrentThread.CurrentCulture <- CultureInfo.GetCultureInfo("ru-RU")

            match "2026-01-02" with
            | Parse.DateTimeInv v ->
                equals v.Year 2026
                equals v.Month 1
                equals v.Day 2
            | _ -> Assert.Fail("expected a match")
        finally
            Thread.CurrentThread.CurrentCulture <- original

    [<Test>]
    member _.``Double parses against the ambient culture; a comma-decimal locale rejects a dot-separated literal``() =
        let original = Thread.CurrentThread.CurrentCulture

        try
            Thread.CurrentThread.CurrentCulture <- CultureInfo.GetCultureInfo("ru-RU")

            let matched =
                match "3.14" with
                | Parse.Double _ -> true
                | _ -> false

            isFalse matched
        finally
            Thread.CurrentThread.CurrentCulture <- original

    [<Test>]
    member _.``DoubleInv parses a dot-separated literal regardless of the ambient culture``() =
        let original = Thread.CurrentThread.CurrentCulture

        try
            Thread.CurrentThread.CurrentCulture <- CultureInfo.GetCultureInfo("ru-RU")

            match "3.14" with
            | Parse.DoubleInv v -> equals v 3.14
            | _ -> Assert.Fail("expected a match")
        finally
            Thread.CurrentThread.CurrentCulture <- original

    [<Test>]
    member _.``DecimalInv parses a dot-separated literal regardless of the ambient culture``() =
        let original = Thread.CurrentThread.CurrentCulture

        try
            Thread.CurrentThread.CurrentCulture <- CultureInfo.GetCultureInfo("ru-RU")

            match "3.14" with
            | Parse.DecimalInv v -> equals v 3.14M
            | _ -> Assert.Fail("expected a match")
        finally
            Thread.CurrentThread.CurrentCulture <- original

    [<Test>]
    member _.``SingleInv parses a dot-separated literal regardless of the ambient culture``() =
        let original = Thread.CurrentThread.CurrentCulture

        try
            Thread.CurrentThread.CurrentCulture <- CultureInfo.GetCultureInfo("ru-RU")

            match "3.5" with
            | Parse.SingleInv v -> equals v 3.5f
            | _ -> Assert.Fail("expected a match")
        finally
            Thread.CurrentThread.CurrentCulture <- original

    [<Test>]
    member _.``DateTimeOffset matches and extracts a value using the ambient culture``() =
        match "2026-01-02T03:04:05+00:00" with
        | Parse.DateTimeOffset v -> equals v.Year 2026
        | _ -> Assert.Fail("expected a match")

    [<Test>]
    member _.``DateTimeOffsetInv parses regardless of the ambient culture``() =
        let original = Thread.CurrentThread.CurrentCulture

        try
            Thread.CurrentThread.CurrentCulture <- CultureInfo.GetCultureInfo("ru-RU")

            match "2026-01-02T03:04:05+00:00" with
            | Parse.DateTimeOffsetInv v ->
                equals v.Year 2026
                equals v.Month 1
                equals v.Day 2
            | _ -> Assert.Fail("expected a match")
        finally
            Thread.CurrentThread.CurrentCulture <- original

[<TestFixture>]
type StringPatternTests() =

    [<Test>]
    member _.``Empty matches an empty string but not null``() =
        let isEmpty s =
            match s with
            | String.Empty -> true
            | _ -> false

        isTrue (isEmpty "")
        isFalse (isEmpty (null: string))
        isFalse (isEmpty "x")

    [<Test>]
    member _.``WhiteSpace matches exactly one whitespace character``() =
        let isSingleWhite s =
            match s with
            | String.WhiteSpace -> true
            | _ -> false

        isTrue (isSingleWhite " ")
        isFalse (isSingleWhite "  ")
        isFalse (isSingleWhite "")

    [<Test>]
    member _.``WhiteSpaces matches two or more whitespace characters only``() =
        let isMultiWhite s =
            match s with
            | String.WhiteSpaces -> true
            | _ -> false

        isTrue (isMultiWhite "   ")
        isFalse (isMultiWhite " ")
        isFalse (isMultiWhite "a ")

    [<Test>]
    member _.``EmptyOrWhiteSpace matches empty and whitespace-only strings but not null``() =
        let matches s =
            match s with
            | String.EmptyOrWhiteSpace -> true
            | _ -> false

        isTrue (matches "")
        isTrue (matches "   ")
        isFalse (matches (null: string))
        isFalse (matches "a")

    [<Test>]
    member _.``NullOrWhiteSpace matches null, empty, and whitespace-only strings``() =
        let matches s =
            match s with
            | String.NullOrWhiteSpace -> true
            | _ -> false

        isTrue (matches (null: string))
        isTrue (matches "")
        isTrue (matches "  ")
        isFalse (matches "a")

    [<Test>]
    member _.``StartsWithAny matches when the string starts with one of the given prefixes``() =
        let matches s =
            match s with
            | String.StartsWithAny [| "foo"; "bar" |] -> true
            | _ -> false

        isTrue (matches "foobar")
        isTrue (matches "barfoo")
        isFalse (matches "baz")

[<TestFixture>]
type RefPatternTests() =

    [<Test>]
    member _.``Null matches a null reference``() =
        let matches (o: obj) =
            match o with
            | Ref.Null -> true
            | _ -> false

        isTrue (matches null)
        isFalse (matches (box 1))

[<TestFixture>]
type StartsWithAnyPatternTests() =

    [<Test>]
    member _.``StartsWithAny is an ordinal prefix test: a zero-width joiner before the prefix is not ignored``() =
        match "\u200dconfig.json" with
        | String.StartsWithAny [| "config" |] -> Assert.Fail("a culture-aware comparison matched")
        | _ -> ()

    [<Test>]
    member _.``StartsWithAny matches an ordinary prefix``() =
        match "config.json" with
        | String.StartsWithAny [| "settings"; "config" |] -> ()
        | _ -> Assert.Fail("expected a match")
