namespace TDesu.FSharp.Tests

open NUnit.Framework

[<AutoOpen>]
module TestHelpers =

    let inline equals<'T> (actual: 'T) (expected: 'T) =
        Assert.That(actual, Is.EqualTo(expected))

    let inline notEquals<'T> (actual: 'T) (expected: 'T) =
        Assert.That(actual, Is.Not.EqualTo(expected))

    let inline isTrue (v: bool) = Assert.That(v, Is.True)

    let inline isFalse (v: bool) = Assert.That(v, Is.False)

    let inline isNone (v: 'a option) = Assert.That(v, Is.EqualTo(None))

    let inline isSome (expected: 'a) (v: 'a option) =
        Assert.That(v, Is.EqualTo(Some expected))

    let inline isOk (expected: 'a) (v: Result<'a, _>) =
        match v with
        | Ok actual -> Assert.That(actual :> obj, Is.EqualTo(expected :> obj))
        | Error e -> Assert.Fail($"Expected Ok({expected}) but got Error({e})")

    let inline isError (v: Result<_, _>) =
        match v with
        | Error _ -> ()
        | Ok v -> Assert.Fail($"Expected Error but got Ok({v})")

    let inline isErrorWith (expected: 'e) (v: Result<_, 'e>) =
        match v with
        | Error actual -> Assert.That(actual :> obj, Is.EqualTo(expected :> obj))
        | Ok v -> Assert.Fail($"Expected Error({expected}) but got Ok({v})")
