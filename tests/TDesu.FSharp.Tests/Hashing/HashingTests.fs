namespace TDesu.FSharp.Tests

open System
open System.Collections.Generic
open NUnit.Framework
open TDesu.FSharp.Hashing

[<TestFixture>]
type ContentHashTests() =

    [<Test>]
    member _.``sha256Hex returns 64-char lowercase hex``() =
        let hex = ContentHash.sha256Hex "hello"
        equals hex.Length 64
        isTrue (hex |> Seq.forall (fun c -> Char.IsDigit c || (c >= 'a' && c <= 'f')))

    [<Test>]
    member _.``sha256Hex differs for different inputs``() =
        notEquals (ContentHash.sha256Hex "a") (ContentHash.sha256Hex "b")

[<TestFixture>]
type CollectionComparerTests() =

    [<Test>]
    member _.``forByteArray enables dictionary with byte array keys``() =
        let dict = Dictionary<byte[], string>(CollectionComparer.forByteArray ())
        dict[[| 1uy; 2uy |]] <- "a"
        dict[[| 3uy; 4uy |]] <- "b"
        equals dict[[| 1uy; 2uy |]] "a"
        equals dict[[| 3uy; 4uy |]] "b"
        isTrue (dict.ContainsKey [| 1uy; 2uy |])
