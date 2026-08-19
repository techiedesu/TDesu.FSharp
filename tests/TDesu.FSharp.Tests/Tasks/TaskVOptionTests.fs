namespace TDesu.FSharp.Tests

open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Tasks

[<TestFixture>]
type TaskVOptionTests() =

    [<Test>]
    member _.``taskBind applies on ValueSome``() =
        let result =
            TaskVOption.taskBind (fun x -> task { return ValueSome(x * 2) }) (task { return ValueSome 21 })
            |> Task.getResult

        equals result (ValueSome 42)

    [<Test>]
    member _.``taskBind returns ValueNone on ValueNone``() =
        let result =
            TaskVOption.taskBind (fun x -> task { return ValueSome(x * 2) }) (task { return ValueNone })
            |> Task.getResult

        equals result ValueNone
