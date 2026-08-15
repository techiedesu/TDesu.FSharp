# Examples

Runnable, self-checking `.fsx` scripts live under
[`examples/`](https://github.com/techiedesu/TDesu.FSharp/tree/master/examples) in the repository.
Each one is a small, standalone walkthrough of one corner of the API that ends in assertions --
a wrong result is a non-zero exit, not a silent `printfn` nobody reads.

They also double as the FSI consumption test: unlike the NUnit suite, which links the library's
projects directly, these scripts exercise the library the way a consumer actually would --
`#r`'d into F# Interactive. `dotnet fsi manage.fsx examples` runs every script against the built
DLL; `dotnet fsi manage.fsx examples --nupkg` packs the library and runs them again against the
packed nupkg, which catches problems (a missing dependency, the wrong target framework) that a
direct DLL reference can't see.

| Script | Covers |
|--------|--------|
| [`01-core.fsx`](https://github.com/techiedesu/TDesu.FSharp/blob/master/examples/01-core.fsx) | `Operators` (`^`, `%`, `tee`, `swap`, `always`), `String`, `Option`/`ValueOption`/`Result` combinators |
| [`02-patterns.fsx`](https://github.com/techiedesu/TDesu.FSharp/blob/master/examples/02-patterns.fsx) | `ActivePatterns`: `Parse.Int`/`Guid`/`Bool`, the `String` shape patterns, and `Comparisons` (`Lt`, `Between`, ...) in a real `match` |
| [`03-numerics.fsx`](https://github.com/techiedesu/TDesu.FSharp/blob/master/examples/03-numerics.fsx) | `Numeric.clamp`/`lerp`/`isBetween` and the `Enum` flag helpers over a `[<Flags>]` enum |
| [`04-collections-and-tasks.fsx`](https://github.com/techiedesu/TDesu.FSharp/blob/master/examples/04-collections-and-tasks.fsx) | `Collections` (`Dictionary`, `Seq`, `ResizeArray`, `Stack`) and `Tasks` (`Task.map`, `TaskGroup`) |
| [`05-resilience.fsx`](https://github.com/techiedesu/TDesu.FSharp/blob/master/examples/05-resilience.fsx) | `Retry.withBackoff`, `CircuitBreaker`, `Memoize` (plain and with a TTL) |

Run them yourself from the repository root:

```
dotnet fsi manage.fsx examples          # against the built DLL
dotnet fsi manage.fsx examples --nupkg  # against a freshly packed nupkg
```

Or run a single script directly once the DLL is built:

```
dotnet fsi examples/01-core.fsx
```
