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

    [<Test>]
    member _.``known vectors for sha256Hex, sha1Hex, md5Hex and sha256HexBytes``() =
        equals (ContentHash.sha256Hex "hello") "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824"
        equals (ContentHash.sha1Hex (Text.Encoding.UTF8.GetBytes "hello")) "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d"
        equals (ContentHash.md5Hex "hello") "5d41402abc4b2a76b9719d911017c592"
        equals (ContentHash.sha256HexBytes [||]) "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"

    [<Test>]
    member _.``hashHex matches the algorithm-specific helper for the same algorithm``() =
        use alg = Security.Cryptography.SHA256.Create()
        equals (ContentHash.hashHex alg (Text.Encoding.UTF8.GetBytes "hello")) (ContentHash.sha256Hex "hello")

[<TestFixture>]
type HashTests() =

    [<Test>]
    member _.``ofSeq of a null sequence is 0``() =
        let xs: int seq = null
        equals (Hash.ofSeq xs) 0

    [<Test>]
    member _.``ofArray of a null array is 0``() =
        let xs: int[] = null
        equals (Hash.ofArray xs) 0

    [<Test>]
    member _.``ofList of a null list is 0``() =
        let xs: int list = Unchecked.defaultof<_>
        equals (Hash.ofList xs) 0

    [<Test>]
    member _.``ofArray agrees with ofSeq over the same elements``() =
        let xs = [| 1; 2; 3; 4 |]
        equals (Hash.ofArray xs) (Hash.ofSeq (xs :> int seq))

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

    [<Test>]
    member _.``forByteArray.GetHashCode(null) does not throw and agrees with Equals(null, null)``() =
        let comparer = CollectionComparer.forByteArray ()
        isTrue (comparer.Equals(null, null))
        equals (comparer.GetHashCode(null)) 0

    [<Test>]
    member _.``forArray enables dictionary with array keys and distinguishes unequal arrays``() =
        let dict = Dictionary<int[], string>(CollectionComparer.forArray<int> ())
        dict[[| 1; 2; 3 |]] <- "a"
        dict[[| 4; 5 |]] <- "b"
        equals dict[[| 1; 2; 3 |]] "a"
        isFalse (dict.ContainsKey [| 1; 2 |])

    [<Test>]
    member _.``forArray.GetHashCode(null) does not throw and agrees with Equals(null, null)``() =
        let comparer = CollectionComparer.forArray<int> ()
        let nullArray: int[] = null
        isTrue (comparer.Equals(nullArray, nullArray))
        equals (comparer.GetHashCode(nullArray)) 0

    [<Test>]
    member _.``forList enables dictionary with list keys``() =
        let dict = Dictionary<int list, string>(CollectionComparer.forList<int> ())
        dict[[ 1; 2; 3 ]] <- "a"
        equals dict[[ 1; 2; 3 ]] "a"
        isFalse (dict.ContainsKey [ 1; 2 ])

    [<Test>]
    member _.``forList.GetHashCode(null) does not throw``() =
        let comparer = CollectionComparer.forList<int> ()
        let nullList: int list = Unchecked.defaultof<_>
        equals (comparer.GetHashCode(nullList)) 0

    [<Test>]
    member _.``forArray.Equals and forByteArray.GetHashCode do not allocate per call``() =
        let arrayComparer = CollectionComparer.forArray<int> ()
        let a = [| for i in 1..64 -> i |]
        let b = [| for i in 1..64 -> i |]
        arrayComparer.Equals(a, b) |> ignore // warm up / JIT

        let byteComparer = CollectionComparer.forByteArray ()
        let bytes = [| for i in 1..64 -> byte i |]
        byteComparer.GetHashCode(bytes) |> ignore // warm up / JIT

        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let before = GC.GetAllocatedBytesForCurrentThread()

        for _ in 1..10_000 do
            arrayComparer.Equals(a, b) |> ignore
            byteComparer.GetHashCode(bytes) |> ignore

        let after = GC.GetAllocatedBytesForCurrentThread()
        let bytesPerIteration = float (after - before) / 10_000.

        isTrue (bytesPerIteration < 16.)
