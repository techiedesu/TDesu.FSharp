namespace TDesu.FSharp.Tests

open System
open System.Buffers
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Buffers

[<TestFixture>]
type ArrayPoolTests() =

    [<Test>]
    member _.``useBytes gives the callback a buffer at least as long as requested``() =
        // ACT
        let observedLength = ArrayPool.useBytes 123 (fun buf -> buf.Length)

        // ASSERT
        isTrue (observedLength >= 123)

    [<Test>]
    member _.``useBytes returns the callback's result``() =
        // ACT
        let result = ArrayPool.useBytes 16 (fun buf -> buf.Length * 2)

        // ASSERT
        isTrue (result >= 32)

    [<Test>]
    member _.``useBytes returns the buffer to the pool even when the callback throws``() =
        // ARRANGE
        let mutable captured: byte[] = null

        // ACT
        %Assert.Throws<InvalidOperationException>(fun () ->
            ArrayPool.useBytes
                4096
                (fun buf ->
                    captured <- buf
                    raise (InvalidOperationException "boom")
                )
        )

        // ASSERT: an immediate re-rent of the same size gets back the exact instance,
        // which is only possible if the buffer was actually returned to the shared pool.
        let reRented = ArrayPool<byte>.Shared.Rent(4096)
        isTrue (obj.ReferenceEquals(captured, reRented))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``usePooled gives the callback a typed buffer at least as long as requested``() =
        // ACT
        let observedLength = ArrayPool.usePooled<int, _> 50 (fun buf -> buf.Length)

        // ASSERT
        isTrue (observedLength >= 50)

    [<Test>]
    member _.``usePooled returns the buffer to the pool even when the callback throws``() =
        // ARRANGE
        let mutable captured: int[] = null

        // ACT
        %Assert.Throws<InvalidOperationException>(fun () ->
            ArrayPool.usePooled<int, unit>
                777
                (fun buf ->
                    captured <- buf
                    raise (InvalidOperationException "boom")
                )
        )

        // ASSERT
        let reRented = ArrayPool<int>.Shared.Rent(777)
        isTrue (obj.ReferenceEquals(captured, reRented))
        ArrayPool<int>.Shared.Return(reRented)

    [<Test>]
    member _.``rentBytes gives a buffer at least as long as requested``() =
        // ACT
        let arr = ArrayPool.rentBytes 200

        // ASSERT
        isTrue (arr.Length >= 200)
        ArrayPool.returnBytes arr

    [<Test>]
    member _.``returnBytes puts the buffer back in the shared pool for reuse``() =
        // ARRANGE
        let arr = ArrayPool.rentBytes 999

        // ACT
        ArrayPool.returnBytes arr

        // ASSERT
        let reRented = ArrayPool<byte>.Shared.Rent(999)
        isTrue (obj.ReferenceEquals(arr, reRented))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``withCopy copies the requested slice into the rented buffer``() =
        // ARRANGE
        let data = [| 10uy; 11uy; 12uy; 13uy; 14uy; 15uy |]

        // ACT
        let copied = ArrayPool.withCopy data 2 3 (fun buf -> buf[0..2])

        // ASSERT
        equals copied [| 12uy; 13uy; 14uy |]

    [<Test>]
    member _.``withCopy returns the buffer to the pool even when the callback throws``() =
        // ARRANGE
        let data = Array.create 555 1uy
        let mutable captured: byte[] = null

        // ACT
        %Assert.Throws<InvalidOperationException>(fun () ->
            ArrayPool.withCopy
                data
                0
                555
                (fun buf ->
                    captured <- buf
                    raise (InvalidOperationException "boom")
                )
        )

        // ASSERT
        let reRented = ArrayPool<byte>.Shared.Rent(555)
        isTrue (obj.ReferenceEquals(captured, reRented))
        ArrayPool<byte>.Shared.Return(reRented)
