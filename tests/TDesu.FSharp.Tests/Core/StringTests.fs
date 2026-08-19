namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp

[<TestFixture>]
type StringTests() =

    [<Test>]
    member _.``countOccurrences counts substrings``() =
        equals ("abcabc" |> String.countOccurrences "abc") 2
        equals ("hello" |> String.countOccurrences "xyz") 0
        equals ("" |> String.countOccurrences "a") 0

    [<Test>]
    member _.``startsWithAny matches any prefix``() =
        isTrue (String.startsWithAny [| "foo"; "bar" |] "foobar")
        isTrue (String.startsWithAny [| "foo"; "bar" |] "baz" |> not)

    [<Test>]
    member _.``truncate limits string length``() =
        equals (String.truncate 5 "hello world") "hello"
        equals (String.truncate 100 "short") "short"
        equals (String.truncate 5 null) null

    [<Test>]
    member _.``toOption returns None for blank strings``() =
        isNone (String.toOption null)
        isNone (String.toOption "")
        isNone (String.toOption "  ")
        isSome "hello" (String.toOption "hello")
