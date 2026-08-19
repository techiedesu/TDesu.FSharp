namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp

[<TestFixture>]
type ValidationTests() =

    [<Test>]
    member _.``ok creates valid value``() =
        let v = Validation.ok 42

        match v with
        | Validation.Ok x -> equals x 42
        | Validation.Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``error creates single-error``() =
        let v: Validation<int, string> = Validation.error "bad"

        match v with
        | Validation.Error errs -> equals errs [ "bad" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``errors creates multi-error``() =
        let v: Validation<int, string> = Validation.errors [ "a"; "b" ]

        match v with
        | Validation.Error errs -> equals errs [ "a"; "b" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``map transforms Ok value``() =
        let v = Validation.ok 5 |> Validation.map (fun x -> x * 2)

        match v with
        | Validation.Ok x -> equals x 10
        | Validation.Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``map preserves Error``() =
        let v: Validation<int, string> =
            Validation.error "err" |> Validation.map (fun x -> x * 2)

        match v with
        | Validation.Error errs -> equals errs [ "err" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``mapError transforms errors``() =
        let v = Validation.error "x" |> Validation.mapError (fun s -> s + "!")

        match v with
        | Validation.Error errs -> equals errs [ "x!" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``bind short-circuits on error``() =
        let v: Validation<int, string> =
            Validation.error "first" |> Validation.bind (fun _ -> Validation.error "second")

        match v with
        | Validation.Error errs -> equals errs [ "first" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``bind chains on Ok``() =
        let v = Validation.ok 5 |> Validation.bind (fun x -> Validation.ok (x + 1))

        match v with
        | Validation.Ok x -> equals x 6
        | Validation.Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``apply combines errors from both sides``() =
        let fV: Validation<int -> string, string> = Validation.error "f-err"
        let xV: Validation<int, string> = Validation.error "x-err"
        let result = Validation.apply fV xV

        match result with
        | Validation.Error errs -> equals errs [ "f-err"; "x-err" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``apply applies function on both Ok``() =
        let fV = Validation.ok (fun x -> x * 2)
        let xV = Validation.ok 5
        let result = Validation.apply fV xV

        match result with
        | Validation.Ok x -> equals x 10
        | Validation.Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``ofResult converts Ok``() =
        let v = Validation.ofResult (Ok 42)

        match v with
        | Validation.Ok x -> equals x 42
        | Validation.Error _ -> Assert.Fail("Expected Ok")

    [<Test>]
    member _.``ofResult converts Error``() =
        let v = Validation.ofResult (Error "bad")

        match v with
        | Validation.Error errs -> equals errs [ "bad" ]
        | Validation.Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``toResult roundtrips``() =
        let okResult = Validation.ok 42 |> Validation.toResult
        isOk 42 okResult

        let errResult: Result<int, string list> =
            Validation.errors [ "a"; "b" ] |> Validation.toResult

        isErrorWith [ "a"; "b" ] errResult

    [<Test>]
    member _.``isOk and isError``() =
        isTrue (Validation.isOk (Validation.ok 1))
        isFalse (Validation.isError (Validation.ok 1))
        isTrue (Validation.isError (Validation.error "e": Validation<int, string>))
        isFalse (Validation.isOk (Validation.error "e": Validation<int, string>))

    [<Test>]
    member _.``valueOrFail returns value on Ok``() =
        let v = Validation.ok 42 |> Validation.valueOrFail
        equals v 42

    [<Test>]
    member _.``valueOrFail throws on Error``() =
        Assert.Throws<InvalidOperationException>(fun () -> Validation.error "bad" |> Validation.valueOrFail |> ignore)
        |> ignore

    [<Test>]
    member _.``defaultValue returns value on Ok``() =
        let v = Validation.ok 42 |> Validation.defaultValue 0
        equals v 42

    [<Test>]
    member _.``defaultValue returns default on Error``() =
        let v: int = Validation.error "bad" |> Validation.defaultValue 99
        equals v 99

    [<Test>]
    member _.``validation CE collects errors with and!``() =
        let validateName (s: string) =
            if String.IsNullOrWhiteSpace s then
                Validation.error "Name required"
            else
                Validation.ok s

        let validateAge (age: int) =
            if age < 0 || age > 150 then
                Validation.error "Invalid age"
            else
                Validation.ok age

        let result =
            validation {
                let! name = validateName ""
                and! age = validateAge -1
                return (name, age)
            }

        match result with
        | Validation.Error errs ->
            equals (List.length errs) 2
            isTrue (List.contains "Name required" errs)
            isTrue (List.contains "Invalid age" errs)
        | Validation.Ok _ -> Assert.Fail("Expected Error with 2 errors")

    [<Test>]
    member _.``validation CE succeeds when all valid``() =
        let result =
            validation {
                let! a = Validation.ok 1
                and! b = Validation.ok 2
                return a + b
            }

        match result with
        | Validation.Ok x -> equals x 3
        | Validation.Error _ -> Assert.Fail("Expected Ok")
