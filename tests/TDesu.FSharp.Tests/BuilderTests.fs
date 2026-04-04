namespace TDesu.FSharp.Tests

open System
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Builders

[<TestFixture>]
type MaybeBuilderTests() =

    [<Test>]
    member _.``option CE returns Some on happy path``() =
        let result = option {
            let! x = Some 1
            let! y = Some 2
            return x + y
        }
        isSome 3 result

    [<Test>]
    member _.``option CE returns None on first None``() =
        let result = option {
            let! x = Some 1
            let! _ = None
            return x + 99
        }
        isNone result

    [<Test>]
    member _.``option CE handles ValueOption bind``() =
        let result = option {
            let! x = ValueSome 42
            return x
        }
        isSome 42 result

    [<Test>]
    member _.``option CE TryWith catches exception``() =
        let result = option {
            try
                return failwith "boom"
            with _ ->
                return 0
        }
        isSome 0 result

    [<Test>]
    member _.``option CE Using disposes resource``() =
        let mutable disposed = false
        let resource = { new IDisposable with member _.Dispose() = disposed <- true }
        let result = option {
            use _ = resource
            return 42
        }
        isSome 42 result
        isTrue disposed

[<TestFixture>]
type ResultBuilderTests() =

    [<Test>]
    member _.``result returns Ok on happy path``() =
        let r = result {
            let! x = Ok 1
            let! y = Ok 2
            return x + y
        }
        isOk 3 r

    [<Test>]
    member _.``result short-circuits on Error``() =
        let r = result {
            let! x = Ok 1
            let! _ = Error "fail"
            return x + 99
        }
        equals r (Error "fail")

    [<Test>]
    member _.``result TryWith catches exception``() =
        let r = result {
            try
                return failwith "boom"
            with _ ->
                return 0
        }
        match r with Ok v -> equals v 0 | Error (_: string) -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``result Using disposes resource``() =
        let mutable disposed = false
        let resource = { new IDisposable with member _.Dispose() = disposed <- true }
        let r = result {
            use _ = resource
            return 42
        }
        match r with Ok v -> equals v 42 | Error (_: string) -> Assert.Fail("Expected Ok")
        isTrue disposed

[<TestFixture>]
type TaskResultBuilderTests() =

    let getResult (t: Task<_>) = t.ConfigureAwait(false).GetAwaiter().GetResult()

    [<Test>]
    member _.``taskResult returns Ok on happy path``() =
        let t : Task<Result<int, string>> = taskResult {
            let! x = Ok 10
            let! y = Task.FromResult(Ok 20)
            return x + y
        }
        isOk 30 (getResult t)

    [<Test>]
    member _.``taskResult short-circuits on Error``() =
        let t : Task<Result<int, string>> = taskResult {
            let! _ = Ok 1
            let! _ = Error "stop"
            return 999
        }
        isErrorWith "stop" (getResult t)

    [<Test>]
    member _.``taskResult binds plain Task``() =
        let t : Task<Result<int, string>> = taskResult {
            let! v = Task.FromResult(42)
            return v
        }
        isOk 42 (getResult t)

    [<Test>]
    member _.``taskResult TryWith catches``() =
        let t : Task<Result<int, string>> = taskResult {
            try
                return failwith "boom"
            with _ ->
                return 0
        }
        isOk 0 (getResult t)

    [<Test>]
    member _.``taskResult TryFinally runs compensation``() =
        let mutable ran = false
        let t : Task<Result<int, string>> = taskResult {
            try
                return 42
            finally
                ran <- true
        }
        isOk 42 (getResult t)
        isTrue ran
