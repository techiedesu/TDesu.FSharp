namespace TDesu.FSharp.Tests

open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Tasks

[<TestFixture>]
type TaskTests() =

    [<Test>]
    member _.``catch captures exception``() =
        match task { return 42 } |> Task.catch |> Task.getResult with
        | Ok v -> equals v 42
        | Error _ -> Assert.Fail("Expected Ok")

        match task { return failwith "boom" } |> Task.catch |> Task.getResult with
        | Error e -> equals e.Message "boom"
        | Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``runSynchronously handles null task``() =
        Task.runSynchronously (null: Task)  // should not throw
