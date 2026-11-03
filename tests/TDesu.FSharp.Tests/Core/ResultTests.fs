namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators

[<TestFixture>]
type ResultTests() =

    [<Test>]
    member _.``get throws on Error``() =
        %Assert.Throws<InvalidOperationException>(fun () -> %Result.get(Error "fail"))

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

    [<Test>]
    member _.``requireNotNull returns Ok for a non-null reference and Error for null``() =
        isOk "value" (Result.requireNotNull "e" "value")
        isErrorWith "e" (Result.requireNotNull "e" (null: string))

    [<Test>]
    member _.``requireNotNull rejects None, which is a null reference at runtime``() =
        isErrorWith "e" (Result.requireNotNull "e" (None: int option))

    /// `requireNotNull` used to box an unconstrained `'T`, so a struct `'T` always compared a fresh
    /// box against null and could never report Error for one. The fix adds a `not struct` constraint,
    /// which an ordinary call site enforces at compile time, so the only way left to observe it from
    /// a test is to go around the compiler: the CLR itself now refuses to bind the method's `'T` to a
    /// value type.
    [<Test>]
    member _.``requireNotNull is constrained to reference types at the CLR level, so a struct cannot bind``() =
        let resultModule =
            typeof<IClock>.Assembly.GetType("TDesu.FSharp.Result").GetMethod("requireNotNull")

        let act () =
            resultModule.MakeGenericMethod(typeof<string>, typeof<int>) |> ignore

        %Assert.Throws<ArgumentException>(act)
