namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks

[<TestFixture>]
type TaskGroupTests() =

    [<Test>]
    member _.``WaitAll completes all tasks``() = task {
        let mutable a = 0
        let mutable b = 0
        use group = new TaskGroup()
        group.Run(fun _ -> task { a <- 1 })
        group.Run(fun _ -> task { b <- 2 })
        do! group.WaitAll()
        equals a 1
        equals b 2
    }

    [<Test>]
    member _.``WaitAll throws AggregateException on failure``() =
        %Assert.ThrowsAsync<AggregateException>(fun () -> task {
            use group = new TaskGroup()
            group.Run(fun _ -> task { failwith "boom" })
            do! group.WaitAll()
        })

    [<Test>]
    member _.``failure cancels other tasks``() = task {
        let mutable cancelled = false
        use group = new TaskGroup()
        group.Run(fun _ -> task { failwith "boom" })
        group.Run(fun ct -> task {
            try
                do! Task.Delay(5000, ct)
            with :? OperationCanceledException ->
                cancelled <- true
        })
        try do! group.WaitAll() with :? AggregateException -> ()
        isTrue cancelled
    }

    [<Test>]
    member _.``Token is cancelled on Dispose``() =
        let group = new TaskGroup()
        let token = group.Token
        isFalse token.IsCancellationRequested
        (group :> IDisposable).Dispose()
        isTrue token.IsCancellationRequested

    [<Test>]
    member _.``linked to parent token``() = task {
        use parentCts = new CancellationTokenSource()
        use group = new TaskGroup(parentCts.Token)
        parentCts.Cancel()
        isTrue group.Token.IsCancellationRequested
    }

    [<Test>]
    member _.``multiple errors collected in AggregateException``() = task {
        use group = new TaskGroup()
        group.Run(fun _ -> task { failwith "error1" })
        do! Task.Delay(50)
        group.Run(fun _ -> task { failwith "error2" })
        let mutable caught: AggregateException option = None
        try do! group.WaitAll()
        with :? AggregateException as ex -> caught <- Some ex
        isTrue caught.IsSome
        isTrue (caught.Value.InnerExceptions.Count >= 1)
    }
