namespace TDesu.FSharp.Tests

open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Tasks

[<TestFixture>]
type TaskResultTests() =

    let errTask (e: string) : Task<Result<int, string>> = Task.FromResult(Error e)

    [<Test>]
    member _.``bind short-circuits on Error``() =
        let r =
            errTask "fail"
            |> TaskResult.bind (fun v -> Task.FromResult(Ok(v * 2) : Result<int, string>))
            |> Task.getResult
        isError r
