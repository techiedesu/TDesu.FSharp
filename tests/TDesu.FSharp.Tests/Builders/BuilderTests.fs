namespace TDesu.FSharp.Tests

open System
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Builders

[<TestFixture>]
type MaybeBuilderTests() =

    [<Test>]
    member _.``option CE returns Some on happy path``() =
        let result =
            option {
                let! x = Some 1
                let! y = Some 2
                return x + y
            }

        isSome 3 result

    [<Test>]
    member _.``option CE returns None on first None``() =
        let result =
            option {
                let! x = Some 1
                let! _ = None
                return x + 99
            }

        isNone result

    [<Test>]
    member _.``option CE handles ValueOption bind``() =
        let result =
            option {
                let! x = ValueSome 42
                return x
            }

        isSome 42 result

    [<Test>]
    member _.``option CE TryWith catches exception``() =
        let result =
            option {
                try
                    return failwith "boom"
                with _ ->
                    return 0
            }

        isSome 0 result

    [<Test>]
    member _.``option CE Using disposes resource``() =
        let mutable disposed = false

        let resource =
            { new IDisposable with
                member _.Dispose() = disposed <- true
            }

        let result =
            option {
                use _ = resource
                return 42
            }

        isSome 42 result
        isTrue disposed

    [<Test>]
    member _.``option CE return () is Some (), not None``() =
        let result: unit option = option { return () }
        isSome () result

    [<Test>]
    member _.``option CE return null does not collapse to None``() =
        let result =
            option {
                let! _ = Some 1
                return (null: string)
            }

        isSome (null: string) result

    [<Test>]
    member _.``option CE do! None inside an if with nothing after: zero path is Some (), failure path is None``() =
        let run (n: int) : unit option =
            option {
                if n < 0 then
                    do! None
            }

        isSome () (run 5)
        isNone (run -5)

    [<Test>]
    member _.``option CE do! None inside an if no longer swallows the failure before a later return``() =
        let parseAge (n: int) : int option =
            option {
                if n < 0 then
                    do! None

                return n
            }

        isSome 30 (parseAge 30)
        isNone (parseAge -3)

    [<Test>]
    member _.``option CE do! ValueNone inside an if no longer swallows the failure before a later return``() =
        let q (n: int) : int option =
            option {
                if n < 0 then
                    do! ValueNone

                return n
            }

        isSome 30 (q 30)
        isNone (q -5)

    [<Test>]
    member _.``option CE For short-circuits on the first None``() =
        let run (xs: int list) : unit option =
            option {
                for x in xs do
                    if x < 0 then
                        do! None
            }

        isSome () (run [ 1; 2; 3 ])
        isNone (run [ 1; -2; 3 ])

    [<Test>]
    member _.``option CE While short-circuits on the first None``() =
        let run () : unit option =
            option {
                let mutable i = 0

                while i < 5 do
                    if i = 3 then
                        do! None

                    i <- i + 1
            }

        isNone (run ())

[<TestFixture>]
type ResultBuilderTests() =

    [<Test>]
    member _.``result returns Ok on happy path``() =
        let r =
            result {
                let! x = Ok 1
                let! y = Ok 2
                return x + y
            }

        isOk 3 r

    [<Test>]
    member _.``result short-circuits on Error``() =
        let r =
            result {
                let! x = Ok 1
                let! _ = Error "fail"
                return x + 99
            }

        equals r (Error "fail")

    [<Test>]
    member _.``result TryWith catches exception``() =
        let r =
            result {
                try
                    return failwith "boom"
                with _ ->
                    return 0
            }

        match r with
        | Ok v -> equals v 0
        | Error(_: string) -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``result Using disposes resource``() =
        let mutable disposed = false

        let resource =
            { new IDisposable with
                member _.Dispose() = disposed <- true
            }

        let r =
            result {
                use _ = resource
                return 42
            }

        match r with
        | Ok v -> equals v 42
        | Error(_: string) -> Assert.Fail("Expected Ok")

        isTrue disposed

    [<Test>]
    member _.``result CE For short-circuits on the first Error``() =
        let run (xs: int list) : Result<unit, string> =
            result {
                for x in xs do
                    if x < 0 then
                        return! Error "negative"
            }

        isOk () (run [ 1; 2; 3 ])
        equals (run [ 1; -2; 3 ]) (Error "negative")

    [<Test>]
    member _.``result CE While short-circuits on the first Error``() =
        let run () : Result<unit, string> =
            result {
                let mutable i = 0

                while i < 5 do
                    if i = 3 then
                        return! Error "hit 3"

                    i <- i + 1
            }

        equals (run ()) (Error "hit 3")

[<TestFixture>]
type TaskResultBuilderTests() =

    let getResult (t: Task<_>) =
        t.ConfigureAwait(false).GetAwaiter().GetResult()

    [<Test>]
    member _.``taskResult returns Ok on happy path``() =
        let t: Task<Result<int, string>> =
            taskResult {
                let! x = Ok 10
                let! y = Task.FromResult(Ok 20)
                return x + y
            }

        isOk 30 (getResult t)

    [<Test>]
    member _.``taskResult short-circuits on Error``() =
        let t: Task<Result<int, string>> =
            taskResult {
                let! _ = Ok 1
                let! _ = Error "stop"
                return 999
            }

        isErrorWith "stop" (getResult t)

    [<Test>]
    member _.``taskResult binds plain Task``() =
        let t: Task<Result<int, string>> =
            taskResult {
                let! v = Task.FromResult(42)
                return v
            }

        isOk 42 (getResult t)

    [<Test>]
    member _.``taskResult TryWith catches``() =
        let t: Task<Result<int, string>> =
            taskResult {
                try
                    return failwith "boom"
                with _ ->
                    return 0
            }

        isOk 0 (getResult t)

    [<Test>]
    member _.``taskResult TryFinally runs compensation``() =
        let mutable ran = false

        let t: Task<Result<int, string>> =
            taskResult {
                try
                    return 42
                finally
                    ran <- true
            }

        isOk 42 (getResult t)
        isTrue ran

    [<Test>]
    member _.``taskResult returns a faulted Task instead of throwing synchronously when the synchronous prefix throws``
        ()
        =
        let t: Task<Result<int, string>> = taskResult { return (failwith "boom": int) }
        isTrue t.IsFaulted

    [<Test>]
    member _.``taskResult returns a faulted Task when an exception is thrown after a synchronous let!``() =
        let t: Task<Result<int, string>> =
            taskResult {
                let! x = Ok 1
                failwith "boom2" |> ignore
                return x
            }

        isTrue t.IsFaulted

    [<Test>]
    member _.``taskResult CE For short-circuits on the first Error``() =
        let run (xs: int list) : Task<Result<unit, string>> =
            taskResult {
                for x in xs do
                    if x < 0 then
                        do! (Error "negative": Result<unit, string>)
            }

        isOk () (getResult (run [ 1; 2; 3 ]))
        isErrorWith "negative" (getResult (run [ 1; -2; 3 ]))

    [<Test>]
    member _.``taskResult CE While short-circuits on the first Error``() =
        let run () : Task<Result<unit, string>> =
            taskResult {
                let mutable i = 0

                while i < 5 do
                    if i = 3 then
                        do! (Error "hit 3": Result<unit, string>)

                    i <- i + 1
            }

        isErrorWith "hit 3" (getResult (run ()))
