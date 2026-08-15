namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp

[<TestFixture>]
type ByrefTests() =

    [<Test>]
    member _.``inc increments the byref value in place, visible to the caller``() =
        // ARRANGE
        let mutable counter = 5
        // ACT
        Byref.inc &counter
        // ASSERT
        equals counter 6

    [<Test>]
    member _.``dec decrements the byref value in place, visible to the caller``() =
        // ARRANGE
        let mutable counter = 5
        // ACT
        Byref.dec &counter
        // ASSERT
        equals counter 4

    [<Test>]
    member _.``setv overwrites the byref value in place, visible to the caller``() =
        // ARRANGE
        let mutable value = 1
        // ACT
        Byref.setv &value 42
        // ASSERT
        equals value 42

    [<Test>]
    member _.``add accumulates into the byref value in place, visible to the caller``() =
        // ARRANGE
        let mutable total = 10
        // ACT
        Byref.add &total 5
        // ASSERT
        equals total 15

    [<Test>]
    member _.``sub reduces the byref value in place, visible to the caller``() =
        // ARRANGE
        let mutable total = 10
        // ACT
        Byref.sub &total 4
        // ASSERT
        equals total 6

    [<Test>]
    member _.``mul scales the byref value up in place, visible to the caller``() =
        // ARRANGE
        let mutable total = 3
        // ACT
        Byref.mul &total 4
        // ASSERT
        equals total 12

    [<Test>]
    member _.``div scales the byref value down in place, visible to the caller``() =
        // ARRANGE
        let mutable total = 20
        // ACT
        Byref.div &total 4
        // ASSERT
        equals total 5

    [<Test>]
    member _.``inc accumulates correctly across repeated calls against the same mutable cell``() =
        // ARRANGE
        let mutable counter = 0
        // ACT
        for _ in 1 .. 100 do
            Byref.inc &counter
        // ASSERT
        equals counter 100

    [<Test>]
    member _.``add mutation accumulates and is visible to the caller across repeated calls``() =
        // ARRANGE
        let mutable counter = 0
        // ACT
        Byref.add &counter 3
        Byref.add &counter 3
        // ASSERT
        equals counter 6
