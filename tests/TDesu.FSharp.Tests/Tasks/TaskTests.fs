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
    member _.``runSynchronously handles null task``() = Task.runSynchronously (null: Task) // should not throw

    // Task.loop exists so a recursive task { } can be written without `return! step`, which is
    // not a tail call: that shape keeps one state machine per iteration alive and nests real
    // stack frames when the awaited work completes synchronously. A million synchronous
    // iterations is far past where the recursive form overflowed (between 1,000 and 1,500 on a
    // 1 MB stack), so finishing at all is the point of the first test.

    [<Test>]
    member _.``loop runs a million synchronous steps in constant stack``() =
        let total =
            Task.loop
                (fun (i, acc) ->
                    Task.FromResult(if i = 1_000_000 then Loop.Stop acc else Loop.Continue(i + 1, acc + int64 i)))
                (0, 0L)
            |> Task.getResult

        equals total 499_999_500_000L

    [<Test>]
    member _.``loop threads the state and returns the stop result``() =
        let words =
            Task.loop
                (fun (n: int, acc: string list) ->
                    task {
                        do! Task.Yield()
                        return if n = 0 then Loop.Stop(List.rev acc) else Loop.Continue(n - 1, string n :: acc)
                    })
                (3, [])
            |> Task.getResult

        equals words [ "3"; "2"; "1" ]

    [<Test>]
    member _.``loop faults when a step throws and does not run further steps``() =
        let mutable steps = 0

        let t =
            Task.loop
                (fun (n: int) ->
                    task {
                        steps <- steps + 1
                        if n = 2 then failwith "boom"
                        return Loop.Continue(n + 1)
                    })
                0

        let ex = Assert.Throws<System.AggregateException>(fun () -> t.Wait())
        equals ex.InnerException.Message "boom"
        equals steps 3
