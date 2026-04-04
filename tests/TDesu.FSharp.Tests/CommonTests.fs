namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Tasks
open TDesu.FSharp.Concurrency

[<TestFixture>]
type CancellationTokenTests() =

    [<Test>]
    member _.``linked cancels when parent cancels``() =
        use parentCts = new CancellationTokenSource()
        let linkedCts, cleanup = CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token
        use _ = cleanup
        isFalse linkedCts.Token.IsCancellationRequested
        parentCts.Cancel()
        isTrue linkedCts.Token.IsCancellationRequested

    [<Test>]
    member _.``linked cancels on timeout``() = task {
        use parentCts = new CancellationTokenSource()
        let linkedCts, cleanup = CancellationToken.linked (TimeSpan.FromMilliseconds 50.) parentCts.Token
        use _ = cleanup
        let! expired = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> linkedCts.Token.IsCancellationRequested)
        isTrue expired
    }

[<TestFixture>]
type TaskVOptionTests() =

    [<Test>]
    member _.``taskBind applies on ValueSome``() =
        let result =
            TaskVOption.taskBind (fun x -> task { return ValueSome (x * 2) }) (task { return ValueSome 21 })
            |> Task.getResult
        equals result (ValueSome 42)

    [<Test>]
    member _.``taskBind returns ValueNone on ValueNone``() =
        let result =
            TaskVOption.taskBind (fun x -> task { return ValueSome (x * 2) }) (task { return ValueNone })
            |> Task.getResult
        equals result ValueNone
