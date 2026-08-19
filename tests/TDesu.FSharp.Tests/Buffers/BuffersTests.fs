namespace TDesu.FSharp.Tests

open NUnit.Framework
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
