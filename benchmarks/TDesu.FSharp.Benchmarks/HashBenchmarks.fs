namespace TDesu.FSharp.Benchmarks

open System.Security.Cryptography
open System.Text
open BenchmarkDotNet.Attributes
open TDesu.FSharp.Hashing

[<MemoryDiagnoser; RankColumn>]
type ContentHashBenchmark() =
    let data: byte[] = Encoding.UTF8.GetBytes(String.replicate 100 "hello world ")

    [<Benchmark(Baseline = true, Description = "Raw SHA256.Create+ComputeHash")>]
    member _.RawSha256() =
        use alg = SHA256.Create()
        alg.ComputeHash(data)

    [<Benchmark(Description = "ContentHash.sha256")>]
    member _.ContentHashSha256() =
        ContentHash.sha256 data

    [<Benchmark(Description = "ContentHash.sha256HexBytes")>]
    member _.ContentHashHex() =
        ContentHash.sha256HexBytes data

[<MemoryDiagnoser; RankColumn>]
type HashCombineBenchmark() =
    let items = Array.init 100 id

    [<Benchmark(Baseline = true, Description = "Array.fold hash (x31)")>]
    member _.FoldHash() =
        items |> Array.fold (fun h x -> h * 31 + x) 0

    [<Benchmark(Description = "Hash.ofArray (System.HashCode)")>]
    member _.HashOfArray() : int =
        Hash.ofArray items
