namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp.Types

[<TestFixture>]
type NonEmptyStringTests() =

    [<Test>]
    member _.``createOrFail throws for null``() =
        Assert.Throws<ArgumentNullException>(fun () -> NonEmptyString.createOrFail null |> ignore)
        |> ignore

    [<Test>]
    member _.``createOrFail throws for empty``() =
        Assert.Throws<ArgumentException>(fun () -> NonEmptyString.createOrFail "" |> ignore)
        |> ignore

    [<Test>]
    member _.``createOrFail throws for whitespace``() =
        Assert.Throws<ArgumentException>(fun () -> NonEmptyString.createOrFail "   " |> ignore)
        |> ignore

    [<Test>]
    member _.``create returns Error Null for a null string``() =
        match NonEmptyString.create null with
        | Error NonEmptyStringError.Null -> ()
        | other -> Assert.Fail($"expected Error Null, got %A{other}")

    [<Test>]
    member _.``create returns Error Empty for an empty or whitespace-only string``() =
        match NonEmptyString.create "" with
        | Error NonEmptyStringError.Empty -> ()
        | other -> Assert.Fail($"expected Error Empty, got %A{other}")

        match NonEmptyString.create "   " with
        | Error NonEmptyStringError.Empty -> ()
        | other -> Assert.Fail($"expected Error Empty, got %A{other}")

    [<Test>]
    member _.``create returns Ok for a non-blank string``() =
        match NonEmptyString.create "hello" with
        | Ok s -> Assert.That(NonEmptyString.value s, Is.EqualTo("hello"))
        | Error e -> Assert.Fail($"expected Ok, got Error %A{e}")

    [<Test>]
    member _.``value unwraps the original string``() =
        let s = NonEmptyString.createOrFail "hello"
        Assert.That(NonEmptyString.value s, Is.EqualTo("hello"))

    [<Test>]
    member _.``length returns the underlying string's length``() =
        let s = NonEmptyString.createOrFail "hello"
        Assert.That(NonEmptyString.length s, Is.EqualTo(5))

[<TestFixture>]
type ApiResponseTests() =

    [<Test>]
    member _.``toResult throws when Success=true but Data=None``() =
        let response: ApiResponse.T<int, string> = {
            Success = true
            Data = None
            Error = None
        }

        Assert.Throws<InvalidOperationException>(fun () -> ApiResponse.toResult response |> ignore)
        |> ignore

    [<Test>]
    member _.``toResult returns Ok when Success=true and Data=Some``() =
        let response = ApiResponse.ok 42

        match ApiResponse.toResult response with
        | Ok v -> Assert.That(v, Is.EqualTo(42))
        | Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``toResult throws when Success=false but Error=None``() =
        let response: ApiResponse.T<int, string> = {
            Success = false
            Data = None
            Error = None
        }

        Assert.Throws<InvalidOperationException>(fun () -> ApiResponse.toResult response |> ignore)
        |> ignore

    [<Test>]
    member _.``toResult returns Error when Success=false and Error=Some``() =
        let response = ApiResponse.error "boom"

        match ApiResponse.toResult response with
        | Error e -> Assert.That(e, Is.EqualTo("boom"))
        | Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``toResult reads only the field matching Success when a response sets both Data and Error``() =
        let bothSetSuccess: ApiResponse.T<int, string> = {
            Success = true
            Data = Some 1
            Error = Some "ignored"
        }

        match ApiResponse.toResult bothSetSuccess with
        | Ok v -> Assert.That(v, Is.EqualTo(1))
        | Error _ -> Assert.Fail("Expected Ok, Error field should be discarded when Success=true")

        let bothSetFailure: ApiResponse.T<int, string> = {
            Success = false
            Data = Some 1
            Error = Some "boom"
        }

        match ApiResponse.toResult bothSetFailure with
        | Error e -> Assert.That(e, Is.EqualTo("boom"))
        | Ok _ -> Assert.Fail("Expected Error, Data field should be discarded when Success=false")

    [<Test>]
    member _.``ofResult round-trips Ok and Error through toResult``() =
        let okResponse = ApiResponse.ofResult (Ok 7: Result<int, string>)
        Assert.That(ApiResponse.toResult okResponse, Is.EqualTo(Ok 7: Result<int, string>))

        let errorResponse = ApiResponse.ofResult (Error "nope": Result<int, string>)
        Assert.That(ApiResponse.toResult errorResponse, Is.EqualTo(Error "nope": Result<int, string>))
