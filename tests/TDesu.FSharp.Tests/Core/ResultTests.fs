namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators

[<TestFixture>]
type ResultTests() =

    [<Test>]
    member _.``get throws on Error``() =
        % Assert.Throws<InvalidOperationException>(fun () -> % Result.get (Error "fail"))

    [<Test>]
    member _.``catch wraps exceptions``() =
        match Result.catch (fun () -> 42) with
        | Ok v -> equals v 42
        | Error _ -> Assert.Fail("Expected Ok")
        match Result.catch (fun () -> failwith "boom") with
        | Error e -> equals e.Message "boom"
        | Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``zip returns first Error``() =
        isErrorWith "a" (Result.zip (Error "a") (Ok 2))
        isErrorWith "b" (Result.zip (Ok 1) (Error "b"))
