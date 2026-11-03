namespace TDesu.FSharp.Tests

open System
open System.Diagnostics
open System.Globalization
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators

[<TestFixture>]
type OperatorTests() =

    [<Test>]
    member _.``snakeCaseToCamelCase converts``() =
        equals (snakeCaseToCamelCase "hello_world") "HelloWorld"
        equals (snakeCaseToCamelCase "foo") "Foo"

    [<Test>]
    member _.``camelCaseToSnakeCase converts``() =
        equals (camelCaseToSnakeCase "HelloWorld") "hello_world"
        equals (camelCaseToSnakeCase "fooBar") "foo_bar"

    /// Under tr-TR, culture-aware casing turns an ASCII `i`/`I` into the Turkish dotted/dotless
    /// forms; these convert identifiers, so the casing must stay invariant regardless of host locale.
    [<Test>]
    member _.``snakeCaseToCamelCase and camelCaseToSnakeCase use invariant casing, not the current culture``() =
        let previous = CultureInfo.CurrentCulture

        try
            CultureInfo.CurrentCulture <- CultureInfo("tr-TR")
            equals (snakeCaseToCamelCase "id_value") "IdValue"
            equals (camelCaseToSnakeCase "HasId") "has_id"
        finally
            CultureInfo.CurrentCulture <- previous

    [<Test>]
    member _.``UnixTime.seconds returns reasonable value``() =
        let now = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        let fast = UnixTime.seconds ()
        isTrue (abs (now - fast) < 2L)

    [<Test>]
    member _.``UnixTime.milliseconds returns reasonable value``() =
        let now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        let fast = UnixTime.milliseconds ()
        isTrue (abs (now - fast) < 100L)

    /// Forces a calibration whose millisecond remainder sits right at a second boundary, then lets a
    /// few milliseconds of real time cross it. The old formula truncated the calibration's elapsed
    /// seconds and its own sub-second remainder separately (`floor(T) + floor(e)`) instead of
    /// together (`floor(T + e)`), so it stayed one second behind whenever a read landed just past
    /// such a boundary without having triggered a recalibration yet.
    [<Test>]
    member _.``UnixTime.seconds agrees with UnixTime.milliseconds / 1000 across a second boundary``() =
        let mutable remainder = -1L
        let search = Stopwatch.StartNew()

        while remainder < 990L && search.Elapsed < TimeSpan.FromSeconds 5.0 do
            UnixTime.recalibrate ()
            remainder <- UnixTime.milliseconds () % 1000L

        isTrue (remainder >= 990L)

        let spin = Stopwatch.StartNew()

        while spin.ElapsedMilliseconds < 25L do
            ()

        equals (UnixTime.seconds ()) (UnixTime.milliseconds () / 1000L)
