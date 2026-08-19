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

    [<Test>]
    member _.``map2 maps over two options``() =
        isSome 3 (Option.map2 (+) (Some 1) (Some 2))
        isNone (Option.map2 (+) (Some 1) None)

    [<Test>]
    member _.``map3 maps over three options``() =
        let f a b c = a + b + c
        isSome 6 (Option.map3 f (Some 1) (Some 2) (Some 3))
        isNone (Option.map3 f (Some 1) None (Some 3))
