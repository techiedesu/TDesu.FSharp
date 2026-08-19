namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp.IO

[<TestFixture>]
type EnvTests() =

    [<Test>]
    member _.``getVar returns Some for set variable``() =
        Environment.SetEnvironmentVariable("TDESU_TEST_VAR", "hello")

        try
            let result = Env.getVar "TDESU_TEST_VAR"
            isSome "hello" result
        finally
            Environment.SetEnvironmentVariable("TDESU_TEST_VAR", null)

    [<Test>]
    member _.``getVar returns None for unset variable``() =
        let result = Env.getVar "TDESU_DEFINITELY_NOT_SET_12345"
        isNone result

    [<Test>]
    member _.``getVar returns None for empty variable``() =
        Environment.SetEnvironmentVariable("TDESU_TEST_EMPTY", "")

        try
            let result = Env.getVar "TDESU_TEST_EMPTY"
            isNone result
        finally
            Environment.SetEnvironmentVariable("TDESU_TEST_EMPTY", null)

    [<Test>]
    member _.``requireVar returns value when set``() =
        Environment.SetEnvironmentVariable("TDESU_TEST_REQ", "value123")

        try
            let result = Env.requireVar "TDESU_TEST_REQ"
            equals result "value123"
        finally
            Environment.SetEnvironmentVariable("TDESU_TEST_REQ", null)

    [<Test>]
    member _.``requireVar throws when not set``() =
        Assert.Throws<InvalidOperationException>(fun () -> Env.requireVar "TDESU_DEFINITELY_NOT_SET_12345" |> ignore)
        |> ignore

    [<Test>]
    member _.``getVarOr returns value when set``() =
        Environment.SetEnvironmentVariable("TDESU_TEST_OR", "real")

        try
            let result = Env.getVarOr "default" "TDESU_TEST_OR"
            equals result "real"
        finally
            Environment.SetEnvironmentVariable("TDESU_TEST_OR", null)

    [<Test>]
    member _.``getVarOr returns default when not set``() =
        let result = Env.getVarOr "fallback" "TDESU_DEFINITELY_NOT_SET_12345"
        equals result "fallback"
