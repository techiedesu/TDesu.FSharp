namespace TDesu.FSharp.Benchmarks

open BenchmarkDotNet.Attributes
open TDesu.FSharp.Buffers

[<MemoryDiagnoser; RankColumn>]
type BytesConcatBenchmark() =
    let a: byte[] = Array.init 64 (fun i -> byte i)
    let b: byte[] = Array.init 64 (fun i -> byte (i + 64))
    let c: byte[] = Array.init 64 (fun i -> byte (i + 128))

    [<Benchmark(Baseline = true, Description = "Array.append (stdlib)")>]
    member _.ArrayAppend() : byte[] = Array.append a b

    [<Benchmark(Description = "Bytes.concat2 (BlockCopy)")>]
    member _.BytesConcat2() : byte[] = Bytes.concat2 a b

    [<Benchmark(Description = "Array.concat 3 (stdlib)")>]
    member _.ArrayConcat3() : byte[] = Array.concat [| a; b; c |]

    [<Benchmark(Description = "Bytes.concat3 (BlockCopy)")>]
    member _.BytesConcat3() : byte[] = Bytes.concat3 a b c

[<MemoryDiagnoser; RankColumn>]
type BytesXorBenchmark() =
    let a: byte[] = Array.init 1024 (fun i -> byte i)
    let b: byte[] = Array.init 1024 (fun i -> byte (255 - i))
    let mutable dst: byte[] = Array.zeroCreate 1024

    [<Benchmark(Baseline = true, Description = "Manual XOR loop")>]
    member _.ManualXor() : byte[] =
        for i = 0 to a.Length - 1 do
            dst[i] <- a[i] ^^^ b[i]
        dst

    [<Benchmark(Description = "Bytes.xorBlock")>]
    member _.BytesXorBlock() : byte[] =
        Bytes.xorBlock a 0 b 0 dst 0 1024
        dst
