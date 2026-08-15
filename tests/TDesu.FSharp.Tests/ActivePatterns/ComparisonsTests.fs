namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp.ActivePatterns

[<TestFixture>]
type ComparisonsTests() =

    [<Test>]
    member _.``Eq matches an equal value``() =
        equals (match 3 with Eq 3 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``Eq does not match a different value``() =
        equals (match 3 with Eq 4 -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``NEq matches a different value``() =
        equals (match "abc" with NEq "xyz" -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``NEq does not match an equal value``() =
        equals (match "abc" with NEq "abc" -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``Lt matches a lesser value``() =
        equals (match 3 with Lt 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``Lt does not match an equal or greater value``() =
        equals (match 10 with Lt 10 -> "matched" | _ -> "no match") "no match"
        equals (match 11 with Lt 10 -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``Gt matches a greater value``() =
        equals (match 11 with Gt 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``Gt does not match an equal or lesser value``() =
        equals (match 10 with Gt 10 -> "matched" | _ -> "no match") "no match"
        equals (match 9 with Gt 10 -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``LtEq matches lesser and equal values``() =
        equals (match 9 with LtEq 10 -> "matched" | _ -> "no match") "matched"
        equals (match 10 with LtEq 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``LtEq does not match a greater value``() =
        equals (match 11 with LtEq 10 -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``GtEq matches greater and equal values``() =
        equals (match 11 with GtEq 10 -> "matched" | _ -> "no match") "matched"
        equals (match 10 with GtEq 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``GtEq does not match a lesser value``() =
        equals (match 9 with GtEq 10 -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``Between matches the inclusive lower boundary``() =
        equals (match 1 with Between 1 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``Between matches the inclusive upper boundary``() =
        equals (match 10 with Between 1 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``Between matches an interior value``() =
        equals (match 5 with Between 1 10 -> "matched" | _ -> "no match") "matched"

    [<Test>]
    member _.``Between does not match values outside the bounds``() =
        equals (match 0 with Between 1 10 -> "matched" | _ -> "no match") "no match"
        equals (match 11 with Between 1 10 -> "matched" | _ -> "no match") "no match"

    [<Test>]
    member _.``Between with lo greater than hi never matches``() =
        equals (match 5 with Between 10 1 -> "matched" | _ -> "no match") "no match"
