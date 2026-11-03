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

    [<Test>]
    member _.``withCopy returns the buffer to the pool even when the copy itself throws``() =
        // ARRANGE: prime the pool's bucket for this size so we know exactly which instance
        // withCopy's internal Rent will receive.
        let primed = ArrayPool<byte>.Shared.Rent(1498)
        ArrayPool<byte>.Shared.Return(primed)
        let data = [| 1uy; 2uy |]

        // ACT: offset/length run past the 2-byte source, so Buffer.BlockCopy itself throws
        // before the callback ever runs.
        %Assert.Throws<ArgumentException>(fun () -> ArrayPool.withCopy data 0 1498 (fun buf -> buf.Length) |> ignore)

        // ASSERT: re-renting the same size gets the exact primed instance back, which is only
        // possible if withCopy returned it to the pool despite the copy throwing.
        let reRented = ArrayPool<byte>.Shared.Rent(1498)
        isTrue (obj.ReferenceEquals(primed, reRented))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``useBytesClearing zeroes the buffer before returning it to the pool``() =
        // ARRANGE / ACT
        let mutable captured: byte[] = null

        ArrayPool.useBytesClearing
            2048
            (fun buf ->
                captured <- buf
                Array.fill buf 0 buf.Length 0xAAuy
            )

        // ASSERT: the immediate re-rent is the exact same instance, and it now reads as zero.
        let reRented = ArrayPool<byte>.Shared.Rent(2048)
        isTrue (obj.ReferenceEquals(captured, reRented))
        isTrue (reRented |> Array.forall ((=) 0uy))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``useBytes (non-clearing) leaves the pooled buffer's previous contents intact``() =
        // ARRANGE / ACT
        let mutable captured: byte[] = null

        ArrayPool.useBytes
            2049
            (fun buf ->
                captured <- buf
                Array.fill buf 0 buf.Length 0xAAuy
            )

        // ASSERT
        let reRented = ArrayPool<byte>.Shared.Rent(2049)
        isTrue (obj.ReferenceEquals(captured, reRented))
        isTrue (reRented |> Array.forall ((=) 0xAAuy))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``usePooledClearing zeroes the buffer before returning it to the pool``() =
        // ARRANGE / ACT
        let mutable captured: int[] = null

        ArrayPool.usePooledClearing<int, unit>
            222
            (fun buf ->
                captured <- buf
                Array.fill buf 0 buf.Length 777
            )

        // ASSERT
        let reRented = ArrayPool<int>.Shared.Rent(222)
        isTrue (obj.ReferenceEquals(captured, reRented))
        isTrue (reRented |> Array.forall ((=) 0))
        ArrayPool<int>.Shared.Return(reRented)

    [<Test>]
    member _.``returnBytesClearing zeroes the buffer before returning it to the pool``() =
        // ARRANGE
        let arr = ArrayPool.rentBytes 1333
        Array.fill arr 0 arr.Length 0xBBuy

        // ACT
        ArrayPool.returnBytesClearing arr

        // ASSERT
        let reRented = ArrayPool<byte>.Shared.Rent(1333)
        isTrue (obj.ReferenceEquals(arr, reRented))
        isTrue (reRented |> Array.forall ((=) 0uy))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``withCopyClearing zeroes the buffer before returning it to the pool``() =
        // ARRANGE / ACT
        let data = Array.create 1444 1uy
        let mutable captured: byte[] = null
        ArrayPool.withCopyClearing data 0 1444 (fun buf -> captured <- buf)

        // ASSERT
        let reRented = ArrayPool<byte>.Shared.Rent(1444)
        isTrue (obj.ReferenceEquals(captured, reRented))
        isTrue (reRented |> Array.forall ((=) 0uy))
        ArrayPool<byte>.Shared.Return(reRented)

    [<Test>]
    member _.``withCopyClearing returns the buffer to the pool even when the copy itself throws``() =
        // ARRANGE
        let primed = ArrayPool<byte>.Shared.Rent(1497)
        ArrayPool<byte>.Shared.Return(primed)
        let data = [| 1uy; 2uy |]

        // ACT
        %Assert.Throws<ArgumentException>(fun () ->
            ArrayPool.withCopyClearing data 0 1497 (fun buf -> buf.Length) |> ignore
        )

        // ASSERT
        let reRented = ArrayPool<byte>.Shared.Rent(1497)
        isTrue (obj.ReferenceEquals(primed, reRented))
        ArrayPool<byte>.Shared.Return(reRented)
