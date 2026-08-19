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
