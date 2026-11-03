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
    member _.``truncate backs off one position instead of cutting a surrogate pair in half``() =
        // "ab\U0001F600cd": a(0) b(1) high-surrogate(2) low-surrogate(3) c(4) d(5) in UTF-16 units.
        equals (String.truncate 3 "ab\U0001F600cd") "ab"
        equals (String.truncate 4 "ab\U0001F600cd") "ab\U0001F600"
        equals (String.truncate 2 "ab\U0001F600cd") "ab"

    [<Test>]
    member _.``toOption returns None for blank strings``() =
        isNone (String.toOption null)
        isNone (String.toOption "")
        isNone (String.toOption "  ")
        isSome "hello" (String.toOption "hello")
