namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp

[<TestFixture>]
type OptionTests() =

    [<Test>]
    member _.``zip combines two options``() =
        isSome (1, "a") (Option.zip (Some 1) (Some "a"))
        isNone (Option.zip (Some 1) None)
        isNone (Option.zip None (Some "a"))
