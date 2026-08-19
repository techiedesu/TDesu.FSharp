module TDesu.FSharp.Benchmarks.Program

open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Toolchains.InProcess.Emit

[<EntryPoint>]
let main args =
    let config =
        ManualConfig
            .Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance))

    BenchmarkSwitcher.FromAssembly(typeof<BytesConcatBenchmark>.Assembly).Run(args, config)
    |> ignore

    0
