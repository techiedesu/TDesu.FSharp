#if FABLE_COMPILER
module internal TDesu.FSharp.Hashing_NotAvailable
#else
namespace TDesu.FSharp.Hashing

open System
open System.Collections.Generic
open System.Security.Cryptography
open System.Text

/// Pipeline-friendly hash combining using System.HashCode.
[<RequireQualifiedAccess>]
module Hash =
    /// Combines two values into a single hash code.
    /// <param name="a">The first value to combine.</param>
    /// <param name="b">The second value to combine.</param>
    let inline combine2 a b = HashCode.Combine(a, b)

    /// Combines three values into a single hash code.
    /// <param name="a">The first value to combine.</param>
    /// <param name="b">The second value to combine.</param>
    /// <param name="c">The third value to combine.</param>
    let inline combine3 a b c = HashCode.Combine(a, b, c)

    /// Combines four values into a single hash code.
    /// <param name="a">The first value to combine.</param>
    /// <param name="b">The second value to combine.</param>
    /// <param name="c">The third value to combine.</param>
    /// <param name="d">The fourth value to combine.</param>
    let inline combine4 a b c d = HashCode.Combine(a, b, c, d)

    /// Hashes all elements of a sequence. Useful for using collections as dictionary keys.
    /// <param name="xs">The sequence of elements to hash.</param>
    let ofSeq (xs: 'a seq) =
        let mutable hc = HashCode()
        for x in xs do hc.Add(x)
        hc.ToHashCode()

    /// Hashes all elements of an array.
    /// <param name="xs">The array of elements to hash.</param>
    let inline ofArray (xs: 'a[]) = ofSeq xs

    /// Hashes all elements of a list.
    /// <param name="xs">The list of elements to hash.</param>
    let inline ofList (xs: 'a list) = ofSeq xs

/// Content hashing helpers (SHA256, SHA1, MD5). Netstandard2.1 compatible.
[<RequireQualifiedAccess>]
module ContentHash =
    let private hexChars = "0123456789abcdef"
    let private toHex (bytes: byte[]) =
        let chars = Array.zeroCreate (bytes.Length * 2)
        for i = 0 to bytes.Length - 1 do
            let b = int bytes[i]
            chars[i * 2] <- hexChars[b >>> 4]
            chars[i * 2 + 1] <- hexChars[b &&& 0xF]
        String(chars)

    /// SHA256 hash of byte array.
    /// <param name="data">The byte array to hash.</param>
    let sha256 (data: byte[]) : byte[] =
        use alg = SHA256.Create()
        alg.ComputeHash(data)

    /// SHA256 hash of a string (UTF-8).
    /// <param name="s">The string to hash.</param>
    let sha256String (s: string) : byte[] =
        sha256 (Encoding.UTF8.GetBytes s)

    /// SHA256 hash as lowercase hex string.
    /// <param name="s">The string to hash.</param>
    let sha256Hex (s: string) : string =
        sha256String s |> toHex

    /// SHA256 hash of bytes as lowercase hex string.
    /// <param name="data">The byte array to hash.</param>
    let sha256HexBytes (data: byte[]) : string =
        sha256 data |> toHex

    /// SHA1 hash of byte array.
    /// <param name="data">The byte array to hash.</param>
    let sha1 (data: byte[]) : byte[] =
        use alg = SHA1.Create()
        alg.ComputeHash(data)

    /// SHA1 hash as lowercase hex string.
    /// <param name="data">The byte array to hash.</param>
    let sha1Hex (data: byte[]) : string =
        sha1 data |> toHex

    /// MD5 hash of a string (UTF-8) as lowercase hex string.
    /// <param name="s">The string to hash.</param>
    let md5Hex (s: string) : string =
        use alg = MD5.Create()
        alg.ComputeHash(Encoding.UTF8.GetBytes s) |> toHex

    /// MD5 hash of bytes as lowercase hex string.
    /// <param name="data">The byte array to hash.</param>
    let md5HexBytes (data: byte[]) : string =
        use alg = MD5.Create()
        alg.ComputeHash(data) |> toHex

    /// Generic: hash bytes to lowercase hex using any HashAlgorithm.
    /// <param name="alg">The hash algorithm instance to use.</param>
    /// <param name="data">The byte array to hash.</param>
    let hashHex (alg: HashAlgorithm) (data: byte[]) : string =
        alg.ComputeHash(data) |> toHex

/// IEqualityComparer implementations for using collections as dictionary keys.
[<RequireQualifiedAccess>]
module CollectionComparer =
    /// Equality comparer for byte arrays (structural, not reference).
    let forByteArray () =
        { new IEqualityComparer<byte[]> with
            member _.Equals(a, b) =
                if obj.ReferenceEquals(a, b) then
                    true
                elif isNull a || isNull b then
                    false
                elif a.Length <> b.Length then
                    false
                else
                    let mutable i = 0
                    let mutable eq = true
                    while eq && i < a.Length do
                        if a[i] <> b[i] then
                            eq <- false
                        i <- i + 1
                    eq

            member _.GetHashCode(arr) =
                Hash.ofArray arr
        }

    /// Equality comparer for generic arrays (structural).
    let forArray<'a when 'a: equality> () =
        { new IEqualityComparer<'a[]> with
            member _.Equals(a, b) =
                if obj.ReferenceEquals(a, b) then
                    true
                elif isNull a || isNull b then
                    false
                elif a.Length <> b.Length then
                    false
                else
                    Array.forall2 (=) a b

            member _.GetHashCode(arr) =
                Hash.ofArray arr
        }

    /// Equality comparer for lists (structural — F# lists already have structural equality but not IEqualityComparer).
    let forList<'a when 'a: equality> () =
        { new IEqualityComparer<'a list> with
            member _.Equals(a, b) = a = b
            member _.GetHashCode(lst) = Hash.ofList lst }
#endif
