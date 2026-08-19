namespace TDesu.FSharp.Benchmarks

open BenchmarkDotNet.Attributes
open TDesu.FSharp

[<MemoryDiagnoser; RankColumn>]
type StringBenchmark() =

    let longString = System.String('a', 10_000)
    let searchString = "hello world hello world hello world"

    [<Benchmark(Description = "String.truncate 100 on 10K chars")>]
    member _.Truncate() : string = String.truncate 100 longString

    [<Benchmark(Description = "String.truncate — no-op (short string)")>]
    member _.TruncateNoop() : string = String.truncate 100 "short"

    [<Benchmark(Description = "String.countOccurrences")>]
    member _.GetCountOfOccurrences() : int =
        searchString |> String.countOccurrences "hello"

    [<Benchmark(Baseline = true, Description = "Manual count via Split")>]
    member _.ManualCount() : int = searchString.Split("hello").Length - 1
