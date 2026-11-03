namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Buffers

[<TestFixture>]
type BytesTests() =

    [<Test>]
    member _.``xor two arrays element-wise``() =
        let a = [| 0xFFuy; 0x00uy; 0xAAuy |]
        let b = [| 0x0Fuy; 0xF0uy; 0x55uy |]
        let result = Bytes.xor a b
        equals result [| 0xF0uy; 0xF0uy; 0xFFuy |]

    [<Test>]
    member _.``constantTimeEquals detects equality``() =
        isTrue (Bytes.constantTimeEquals [| 1uy; 2uy; 3uy |] [| 1uy; 2uy; 3uy |])
        isFalse (Bytes.constantTimeEquals [| 1uy; 2uy; 3uy |] [| 1uy; 2uy; 4uy |])
        isFalse (Bytes.constantTimeEquals [| 1uy |] [| 1uy; 2uy |])

    [<Test>]
    member _.``xor throws ArgumentException when lengths differ, instead of silently truncating``() =
        let a = [| 1uy; 2uy; 3uy; 4uy |]
        let b = [| 1uy; 2uy |]
        %Assert.Throws<ArgumentException>(fun () -> Bytes.xor a b |> ignore)

    [<Test>]
    member _.``xorBlock XORs the requested region at the given offsets``() =
        let a = [| 0uy; 0xFFuy; 0x00uy; 0uy |]
        let b = [| 0uy; 0x0Fuy; 0xF0uy; 0uy |]
        let dst = Array.zeroCreate<byte> 4
        Bytes.xorBlock a 1 b 1 dst 1 2
        equals dst [| 0uy; 0xF0uy; 0xF0uy; 0uy |]

    [<Test>]
    member _.``xorInPlace XORs b into a at the given offsets``() =
        let a = [| 0uy; 0xFFuy; 0x00uy; 0uy |]
        let b = [| 0uy; 0x0Fuy; 0xF0uy; 0uy |]
        Bytes.xorInPlace a 1 b 1 2
        equals a [| 0uy; 0xF0uy; 0xF0uy; 0uy |]

    [<Test>]
    member _.``concat2 joins two arrays in order``() =
        equals (Bytes.concat2 [| 1uy; 2uy |] [| 3uy; 4uy; 5uy |]) [| 1uy; 2uy; 3uy; 4uy; 5uy |]

    [<Test>]
    member _.``concat2 handles an empty operand on either side``() =
        equals (Bytes.concat2 [||] [| 1uy; 2uy |]) [| 1uy; 2uy |]
        equals (Bytes.concat2 [| 1uy; 2uy |] [||]) [| 1uy; 2uy |]

    [<Test>]
    member _.``concat3 joins three arrays in order``() =
        equals (Bytes.concat3 [| 1uy |] [| 2uy; 3uy |] [| 4uy |]) [| 1uy; 2uy; 3uy; 4uy |]

    [<Test>]
    member _.``concat4 joins four arrays in order``() =
        equals (Bytes.concat4 [| 1uy |] [| 2uy |] [| 3uy |] [| 4uy |]) [| 1uy; 2uy; 3uy; 4uy |]

    [<Test>]
    member _.``slice copies the requested region``() =
        equals (Bytes.slice [| 10uy; 11uy; 12uy; 13uy; 14uy |] 1 3) [| 11uy; 12uy; 13uy |]

    [<Test>]
    member _.``copyTo copies source bytes into destination at the given offset``() =
        let dst = Array.zeroCreate<byte> 5
        Bytes.copyTo [| 1uy; 2uy; 3uy |] 0 dst 2 3
        equals dst [| 0uy; 0uy; 1uy; 2uy; 3uy |]

    [<Test>]
    member _.``fill sets a region to a constant value``() =
        let arr = Array.zeroCreate<byte> 5
        Bytes.fill 9uy 1 3 arr
        equals arr [| 0uy; 9uy; 9uy; 9uy; 0uy |]

    [<Test>]
    member _.``regionEquals detects equal and unequal regions``() =
        isTrue (Bytes.regionEquals [| 1uy; 2uy; 3uy |] 0 [| 9uy; 1uy; 2uy; 3uy |] 1 3)
        isFalse (Bytes.regionEquals [| 1uy; 2uy; 3uy |] 0 [| 1uy; 2uy; 9uy |] 0 3)
