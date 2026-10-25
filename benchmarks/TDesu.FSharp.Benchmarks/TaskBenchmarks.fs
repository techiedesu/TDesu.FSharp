namespace TDesu.FSharp.Benchmarks

open System.Threading.Tasks
open BenchmarkDotNet.Attributes
open TDesu.FSharp.Tasks

/// `map`/`bind`/`ignore` short-circuit an input that already finished, which is the common
/// case in a request pipeline (a cache hit, a buffered read). The builder cases are here so a
/// regression in the short-circuit shows up as the two converging again.
[<MemoryDiagnoser; RankColumn>]
type TaskCombinatorBenchmark() =

    let completed = Task.FromResult 42

    [<Benchmark(Baseline = true, Description = "task { let! v = completed in return v + 1 }")>]
    member _.BuilderOverCompleted() : int =
        task {
            let! v = completed
            return v + 1
        }
        |> Task.getResult

    [<Benchmark(Description = "Task.map over a completed task")>]
    member _.MapOverCompleted() : int =
        completed |> Task.map (fun v -> v + 1) |> Task.getResult

    [<Benchmark(Description = "Task.bind over a completed task")>]
    member _.BindOverCompleted() : int =
        completed |> Task.bind (fun v -> Task.FromResult(v + 1)) |> Task.getResult

    [<Benchmark(Description = "Task.ignore over a completed task")>]
    member _.IgnoreOverCompleted() : unit =
        completed |> Task.ignore |> Task.getResult

    /// The pending input must keep going through the state machine; this pins that the
    /// short-circuit did not cost the asynchronous path anything.
    [<Benchmark(Description = "Task.map over a task that completes later")>]
    member _.MapOverPending() : int =
        let source =
            TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)

        let mapped = completed |> Task.bind (fun v -> Task.map (fun x -> x + v) source.Task)
        source.SetResult 1
        Task.getResult mapped
