namespace TDesu.FSharp.Benchmarks

open System
open BenchmarkDotNet.Attributes
open TDesu.FSharp

[<MemoryDiagnoser; RankColumn>]
type UnixTimeBenchmark() =

    [<Benchmark(Baseline = true, Description = "DateTimeOffset.UtcNow.ToUnixTimeSeconds()")>]
    member _.SystemCall() : int64 =
        DateTimeOffset.UtcNow.ToUnixTimeSeconds()

    [<Benchmark(Description = "UnixTime.seconds() (cached Stopwatch)")>]
    member _.CachedStopwatch() : int64 =
        UnixTime.seconds ()

    [<Benchmark(Description = "UnixTime.seconds32()")>]
    member _.CachedInt32() : int32 =
        UnixTime.seconds32 ()

    [<Benchmark(Description = "DateTimeOffset.UtcNow (full object)")>]
    member _.FullDateTimeOffset() : DateTimeOffset =
        DateTimeOffset.UtcNow
