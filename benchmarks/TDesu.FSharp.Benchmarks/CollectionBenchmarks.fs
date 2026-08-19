namespace TDesu.FSharp.Benchmarks

open System.Collections.Generic
open BenchmarkDotNet.Attributes
open TDesu.FSharp.Collections
open TDesu.FSharp.Concurrency

[<MemoryDiagnoser; RankColumn>]
type BoundedDictBenchmark() =

    [<Benchmark(Baseline = true, Description = "Dictionary (no bound)")>]
    member _.PlainDict() : int =
        let d = Dictionary<int, int>()

        for i in 0..999 do
            d[i] <- i * 2

        d.Count

    [<Benchmark(Description = "BoundedDict (cap=500)")>]
    member _.BoundedDict500() : int =
        let d = BoundedDict<int, int>(500)

        for i in 0..999 do
            d.Set(i, i * 2)

        d.Count

[<MemoryDiagnoser; RankColumn>]
type ResizeArrayVsListBenchmark() =
    let items: int list = [ 1..1000 ]

    [<Benchmark(Baseline = true, Description = "List.map + List.filter")>]
    member _.FSharpList() : int =
        items |> List.map ((*) 2) |> List.filter (fun x -> x > 500) |> List.length

    [<Benchmark(Description = "ResizeArray pipeline")>]
    member _.ResizeArrayPipeline() : int =
        ResizeArray.ofList items
        |> ResizeArray.map ((*) 2)
        |> ResizeArray.filter (fun x -> x > 500)
        |> ResizeArray.count
