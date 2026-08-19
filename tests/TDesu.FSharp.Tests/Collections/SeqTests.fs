namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp.Collections

[<TestFixture>]
type SeqTests() =

    [<Test>]
    member _.``toResizeArray on a null sequence returns an empty ResizeArray``() =
        // ARRANGE
        let source: int seq = null
        // ACT
        let result = Seq.toResizeArray source
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``toResizeArray on an empty sequence returns an empty ResizeArray``() =
        // ARRANGE
        let source = Seq.empty<int>
        // ACT
        let result = Seq.toResizeArray source
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``toResizeArray preserves element order``() =
        // ARRANGE
        let source =
            seq {
                1
                2
                3
                4
                5
            }
        // ACT
        let result = Seq.toResizeArray source
        // ASSERT
        equals (result |> Seq.toList) [ 1; 2; 3; 4; 5 ]

    [<Test>]
    member _.``toResizeArray pre-sizes from an ICollection source so capacity exactly matches count``() =
        // ARRANGE
        // Arrays implement ICollection<'T>, so the constructor path can pre-size via Count + CopyTo
        // instead of growing-and-copying through repeated Add calls.
        let source: int[] = [| 1; 2; 3; 4; 5; 6; 7 |]
        // ACT
        let result = Seq.toResizeArray source
        // ASSERT
        equals result.Count 7
        equals result.Capacity 7

    [<Test>]
    member _.``toResizeArray enumerates a non-ICollection source exactly once``() =
        // ARRANGE
        let mutable enumerations = 0

        let source =
            seq {
                for x in [ 1; 2; 3 ] do
                    enumerations <- enumerations + 1
                    yield x
            }
        // ACT
        let result = Seq.toResizeArray source
        // ASSERT
        equals (result |> Seq.toList) [ 1; 2; 3 ]
        equals enumerations 3

    [<Test>]
    member _.``toResizeArray returns a mutable, independent copy``() =
        // ARRANGE
        let source = [ 1; 2; 3 ]
        // ACT
        let result = Seq.toResizeArray source
        result.Add 4
        // ASSERT
        equals (result |> Seq.toList) [ 1; 2; 3; 4 ]
        equals source [ 1; 2; 3 ]
