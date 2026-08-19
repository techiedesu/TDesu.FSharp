namespace TDesu.FSharp.Benchmarks

open System
open System.Text
open FSharp.NativeInterop
open BenchmarkDotNet.Attributes
open TDesu.FSharp.Buffers

#nowarn "9" // NativePtr.stackalloc — used once per benchmark call, directly in the calling
// method (never in a loop, never returned) — see ValueStringBuilder's XML docs.

[<AutoOpen>]
module private ValueStringBuilderBenchmarkHelpers =
    /// Deterministic, readable payload character for position i ('a'..'z' repeating).
    let inline charAt (i: int) : char = char (int 'a' + (i % 26))

/// <summary>
/// Decides the ValueStringBuilder question empirically: compares it against
/// <see cref="System.Text.StringBuilder"/> and against plain <c>+</c> (<c>String.Concat</c>)
/// accumulation, at output sizes that sweep across and beyond the 256-char initial stack
/// buffer used here — so the crossover between "fits on the stack" and "needs the shared
/// pool" is visible directly in the summary table instead of asserted.
/// </summary>
[<MemoryDiagnoser; RankColumn>]
type ValueStringBuilderBenchmark() =

    [<Params(16, 64, 256, 1024, 8192)>]
    member val Size = 0 with get, set

    [<Benchmark(Baseline = true, Description = "System.Text.StringBuilder, char-by-char")>]
    member this.StringBuilderAppend() : string =
        let sb = StringBuilder()

        for i = 0 to this.Size - 1 do
            sb.Append(charAt i) |> ignore

        sb.ToString()

    [<Benchmark(Description = "ValueStringBuilder (256-char stackalloc), char-by-char")>]
    member this.ValueStringBuilderAppend() : string =
        let buffer = NativePtr.stackalloc<char> 256
        let mutable sb = ValueStringBuilder(Span<char>(NativePtr.toVoidPtr buffer, 256))

        try
            for i = 0 to this.Size - 1 do
                sb.Append(charAt i)

            sb.ToString()
        finally
            sb.Dispose()

    [<Benchmark(Description = "Plain String.Concat (+) accumulation, char-by-char")>]
    member this.PlainConcat() : string =
        let mutable result = ""

        for i = 0 to this.Size - 1 do
            result <- result + string (charAt i)

        result

/// <summary>
/// Follow-up to <see cref="ValueStringBuilderBenchmark"/>: the background for this type
/// claims plain <see cref="System.Text.StringBuilder"/> "wins back" for very large strings.
/// <c>Plain String.Concat</c> is dropped here — its O(n^2) cost already proven catastrophic
/// above and it would dominate wall-clock time for no new information — so this hunts
/// specifically for a StringBuilder/ValueStringBuilder crossover at sizes far beyond the
/// 256-char stack buffer, instead of assuming one exists.
/// </summary>
[<MemoryDiagnoser; RankColumn>]
type ValueStringBuilderLargeOutputBenchmark() =

    [<Params(65536, 262144, 1048576)>]
    member val Size = 0 with get, set

    [<Benchmark(Baseline = true, Description = "System.Text.StringBuilder, char-by-char")>]
    member this.StringBuilderAppend() : string =
        let sb = StringBuilder()

        for i = 0 to this.Size - 1 do
            sb.Append(charAt i) |> ignore

        sb.ToString()

    [<Benchmark(Description = "ValueStringBuilder (256-char stackalloc), char-by-char")>]
    member this.ValueStringBuilderAppend() : string =
        let buffer = NativePtr.stackalloc<char> 256
        let mutable sb = ValueStringBuilder(Span<char>(NativePtr.toVoidPtr buffer, 256))

        try
            for i = 0 to this.Size - 1 do
                sb.Append(charAt i)

            sb.ToString()
        finally
            sb.Dispose()
