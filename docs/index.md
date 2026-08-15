# TDesu.FSharp

Practical F# utility library. Extends FSharp.Core with the functions you keep rewriting.

## Getting Started

```
dotnet add package TDesu.FSharp
```

```fsharp
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.Builders

// Operators
raise ^ exn "something went wrong"
%httpClient.SendAsync(req)

// String helpers
"hello world" |> String.contains "world"
"hello" |> String.toOption  // Some "hello"

// Option/Result combinators
Some 42 |> Option.toResult "missing"
Ok 42 |> Result.tee (printfn "got %d")

// Task combinators
Task.zip (getUser()) (getOrders())

// Computation expressions
let result = taskResult {
    let! user = fetchUser id
    return! fetchOrders user.Id
}
```

## Namespaces

| Namespace | Description |
|-----------|-------------|
| `TDesu.FSharp` | Core operators, String, Option, Result, Guard, UnixTime, Validation, Clock, StateMachine, NumericParsing, Numeric, Enum |
| `TDesu.FSharp.Builders` | Computation expressions: `result {}`, `option {}`, `taskResult {}` |
| `TDesu.FSharp.Tasks` | Task/TaskResult combinators, TaskGroup, parallelThrottle, fireAndForget |
| `TDesu.FSharp.Collections` | Dictionary, ResizeArray, Seq, List, Stack extensions |
| `TDesu.FSharp.Concurrency` | AtomicInt/Int64, BoundedDict, BoundedQueue, Signal, PeriodicTimer, ChannelWorker, SlidingWindowLimiter |
| `TDesu.FSharp.Resilience` | Retry, CircuitBreaker, Timeout, Memoize, Saga |
| `TDesu.FSharp.IO` | Env, File, Directory, Disposable, TemporaryFileStream, Stream |
| `TDesu.FSharp.Buffers` | Bytes (xor, concat, constantTimeEquals), ArrayPool, ValueStringBuilder |
| `TDesu.FSharp.Hashing` | ContentHash (SHA256/SHA1/MD5), Hash.combine, CollectionComparer |
| `TDesu.FSharp.ActivePatterns` | Parse.Int/Double/Guid/Bool, String patterns, comparison patterns (Eq/Lt/Gt/Between) |
| `TDesu.FSharp.Types` | NonEmptyString, ApiResponse |

## Design Principles

- **Idiomatic F#** -- follows FSharp.Core naming conventions
- **Inline everything** -- zero-cost abstractions via `[<InlineIfLambda>]`
- **No dependencies** -- only FSharp.Core
- **XML docs** -- works with IDE tooltips and fsdocs

## Links

- [NuGet](https://www.nuget.org/packages/TDesu.FSharp)
- [GitHub](https://github.com/techiedesu/TDesu.FSharp)
- [Release Notes](https://github.com/techiedesu/TDesu.FSharp/blob/master/RELEASE_NOTES.md)

## License

[Unlicense](https://github.com/techiedesu/TDesu.FSharp/blob/master/LICENSE) -- public domain
