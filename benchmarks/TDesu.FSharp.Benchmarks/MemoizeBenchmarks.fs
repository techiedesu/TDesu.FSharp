namespace TDesu.FSharp.Benchmarks

open System
open BenchmarkDotNet.Attributes
open TDesu.FSharp.Resilience

[<MemoryDiagnoser; RankColumn>]
type MemoizeBenchmark() =

    let plainFn (k: int) = k * k
    let memoized = Memoize.create plainFn
    let memoizedTtl = Memoize.withTtl (TimeSpan.FromMinutes 5.) plainFn

    [<Benchmark(Baseline = true, Description = "Plain function call")>]
    member _.Plain() : int = plainFn 42

    [<Benchmark(Description = "Memoize.create — cache hit")>]
    member _.MemoizeHit() : int = memoized 42

    [<Benchmark(Description = "Memoize.withTtl — cache hit")>]
    member _.MemoizeTtlHit() : int = memoizedTtl 42

    [<Benchmark(Description = "Memoize.create — cache miss (new key each call)")>]
    member this.MemoizeMiss() : int =
        let f = Memoize.create plainFn
        f 42

    [<Benchmark(Description = "Memoize.withTtl — cache miss (new key each call)")>]
    member this.MemoizeTtlMiss() : int =
        let f = Memoize.withTtl (TimeSpan.FromMinutes 5.) plainFn
        f 42
