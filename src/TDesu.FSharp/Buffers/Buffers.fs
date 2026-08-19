namespace TDesu.FSharp.Buffers

open System
open System.Buffers

/// <namespacedoc>
///   <summary>Buffer utilities: Bytes (xor, concat, constantTimeEquals), ArrayPool helpers.</summary>
/// </namespacedoc>
[<RequireQualifiedAccess>]
module Bytes =

    /// XORs two byte arrays at given offsets into a destination array.
    /// <param name="a">The first source byte array.</param>
    /// <param name="aOff">The offset into the first source array.</param>
    /// <param name="b">The second source byte array.</param>
    /// <param name="bOff">The offset into the second source array.</param>
    /// <param name="dst">The destination byte array.</param>
    /// <param name="dstOff">The offset into the destination array.</param>
    /// <param name="len">The number of bytes to XOR.</param>
    let inline xorBlock (a: byte[]) (aOff: int) (b: byte[]) (bOff: int) (dst: byte[]) (dstOff: int) (len: int) =
        for i = 0 to len - 1 do
            dst[dstOff + i] <- a[aOff + i] ^^^ b[bOff + i]

    /// XORs two byte arrays element-wise, returning a new array.
    /// <param name="a">The first byte array.</param>
    /// <param name="b">The second byte array.</param>
    let xor (a: byte[]) (b: byte[]) =
        let len = min a.Length b.Length
        let result = Array.zeroCreate len

        for i = 0 to len - 1 do
            result[i] <- a[i] ^^^ b[i]

        result

    /// XORs b into a in-place (mutates a).
    /// <param name="a">The target byte array to mutate.</param>
    /// <param name="aOff">The offset into the target array.</param>
    /// <param name="b">The source byte array to XOR from.</param>
    /// <param name="bOff">The offset into the source array.</param>
    /// <param name="len">The number of bytes to XOR.</param>
    let xorInPlace (a: byte[]) (aOff: int) (b: byte[]) (bOff: int) (len: int) =
        for i = 0 to len - 1 do
            a[aOff + i] <- a[aOff + i] ^^^ b[bOff + i]

    /// Concatenates two byte arrays using BlockCopy (faster than Array.append for bytes).
    /// <param name="a">The first byte array.</param>
    /// <param name="b">The second byte array.</param>
    let concat2 (a: byte[]) (b: byte[]) : byte[] =
        let al = a.Length
        let bl = b.Length
        let result: byte[] = Array.zeroCreate (al + bl)

        if al > 0 then
            Buffer.BlockCopy(a, 0, result, 0, al)

        if bl > 0 then
            Buffer.BlockCopy(b, 0, result, al, bl)

        result

    /// Concatenates three byte arrays without intermediate allocations.
    /// <param name="a">The first byte array.</param>
    /// <param name="b">The second byte array.</param>
    /// <param name="c">The third byte array.</param>
    let concat3 (a: byte[]) (b: byte[]) (c: byte[]) =
        let result = Array.zeroCreate (a.Length + b.Length + c.Length)
        Buffer.BlockCopy(a, 0, result, 0, a.Length)
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length)
        Buffer.BlockCopy(c, 0, result, a.Length + b.Length, c.Length)
        result

    /// Concatenates four byte arrays without intermediate allocations.
    /// <param name="a">The first byte array.</param>
    /// <param name="b">The second byte array.</param>
    /// <param name="c">The third byte array.</param>
    /// <param name="d">The fourth byte array.</param>
    let concat4 (a: byte[]) (b: byte[]) (c: byte[]) (d: byte[]) =
        let total = a.Length + b.Length + c.Length + d.Length
        let result = Array.zeroCreate total
        let mutable off = 0
        Buffer.BlockCopy(a, 0, result, off, a.Length)
        off <- off + a.Length
        Buffer.BlockCopy(b, 0, result, off, b.Length)
        off <- off + b.Length
        Buffer.BlockCopy(c, 0, result, off, c.Length)
        off <- off + c.Length
        Buffer.BlockCopy(d, 0, result, off, d.Length)
        result

    /// Copies a slice of a byte array using BlockCopy.
    /// <param name="source">The source byte array to slice from.</param>
    /// <param name="offset">The starting offset in the source array.</param>
    /// <param name="length">The number of bytes to copy.</param>
    let slice (source: byte[]) (offset: int) (length: int) =
        let result = Array.zeroCreate length
        Buffer.BlockCopy(source, offset, result, 0, length)
        result

    /// Copies source bytes into destination at the given offset.
    /// <param name="src">The source byte array.</param>
    /// <param name="srcOff">The offset into the source array.</param>
    /// <param name="dst">The destination byte array.</param>
    /// <param name="dstOff">The offset into the destination array.</param>
    /// <param name="len">The number of bytes to copy.</param>
    let inline copyTo (src: byte[]) (srcOff: int) (dst: byte[]) (dstOff: int) (len: int) =
        Buffer.BlockCopy(src, srcOff, dst, dstOff, len)

    /// Fills a byte array region with a constant value.
    /// <param name="value">The byte value to fill with.</param>
    /// <param name="offset">The starting offset in the array.</param>
    /// <param name="length">The number of bytes to fill.</param>
    /// <param name="arr">The target byte array.</param>
    let inline fill (value: byte) (offset: int) (length: int) (arr: byte[]) = Array.Fill(arr, value, offset, length)

    /// Returns true if two byte array regions are equal.
    /// <param name="a">The first byte array.</param>
    /// <param name="aOff">The offset into the first array.</param>
    /// <param name="b">The second byte array.</param>
    /// <param name="bOff">The offset into the second array.</param>
    /// <param name="len">The number of bytes to compare.</param>
    let regionEquals (a: byte[]) (aOff: int) (b: byte[]) (bOff: int) (len: int) =
        let mutable i = 0
        let mutable equal = true

        while equal && i < len do
            if a[aOff + i] <> b[bOff + i] then
                equal <- false

            i <- i + 1

        equal

    /// Constant-time byte array comparison (timing-safe for crypto).
    /// Both arrays must be the same length; returns false immediately if lengths differ.
    /// Safe for HMAC verification where output lengths are fixed and known.
    /// <param name="a">The first byte array.</param>
    /// <param name="b">The second byte array.</param>
    let constantTimeEquals (a: byte[]) (b: byte[]) =
        if a.Length <> b.Length then
            false
        else
            let mutable diff = 0

            for i = 0 to a.Length - 1 do
                diff <- diff ||| int (a[i] ^^^ b[i])

            diff = 0

[<RequireQualifiedAccess>]
module ArrayPool =

    /// Rents a byte buffer, applies f, then returns the buffer to the pool.
    /// <param name="minLength">The minimum length of the rented buffer.</param>
    /// <param name="f">The function to apply to the rented buffer.</param>
    let inline useBytes (minLength: int) ([<InlineIfLambda>] f: byte[] -> 'R) : 'R =
        let arr = ArrayPool<byte>.Shared.Rent(minLength)

        try
            f arr
        finally
            ArrayPool<byte>.Shared.Return(arr)

    /// Rents a typed buffer from the shared pool, applies f, then returns the buffer.
    /// <param name="minLength">The minimum length of the rented buffer.</param>
    /// <param name="f">The function to apply to the rented buffer.</param>
    let inline usePooled<'T, 'R> (minLength: int) ([<InlineIfLambda>] f: 'T[] -> 'R) : 'R =
        let arr = ArrayPool<'T>.Shared.Rent(minLength)

        try
            f arr
        finally
            ArrayPool<'T>.Shared.Return(arr)

    /// Rents a byte buffer from the shared pool.
    /// <param name="minLength">The minimum length of the rented buffer.</param>
    let inline rentBytes (minLength: int) = ArrayPool<byte>.Shared.Rent(minLength)

    /// Returns a byte buffer to the shared pool.
    /// <param name="arr">The byte array to return to the pool.</param>
    let inline returnBytes (arr: byte[]) = ArrayPool<byte>.Shared.Return(arr)

    /// Rents a buffer, copies data into it, applies f, then returns the buffer.
    /// <param name="data">The source byte array to copy from.</param>
    /// <param name="offset">The starting offset in the source array.</param>
    /// <param name="length">The number of bytes to copy.</param>
    /// <param name="f">The function to apply to the copied buffer.</param>
    let inline withCopy (data: byte[]) (offset: int) (length: int) ([<InlineIfLambda>] f: byte[] -> 'R) : 'R =
        let arr = ArrayPool<byte>.Shared.Rent(length)
        Buffer.BlockCopy(data, offset, arr, 0, length)

        try
            f arr
        finally
            ArrayPool<byte>.Shared.Return(arr)
