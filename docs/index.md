# TDesu.FSharp

Practical F# utility library. Extends FSharp.Core with the functions you keep rewriting.

## Getting Started

```
dotnet add package TDesu.FSharp
```

```fsharp
open TDesu.FSharp

// Operators
raise ^ exn "something went wrong"
%httpClient.SendAsync(req)

// String helpers
"hello world" |> String.contains "world"

// Option/Result combinators
Some 42 |> Option.toResult "missing"
Ok 42 |> Result.tee (printfn "got %d")

// Task combinators
Task.zip (getUser()) (getOrders())

// Computation expressions
let result = taskResult {
    let! user = fetchUser id
    let! orders = fetchOrders user.Id
    return orders.Length
}
```

## Modules

| Module | Description |
|--------|-------------|
| `String` | Pipeline-friendly string operations |
| `Option` | Extended option combinators |
| `Result` | Railway-oriented programming |
| `Task` | Task combinators (map, bind, zip) |
| `TaskResult` | Async Result pipelines |
| `ResizeArray` | Functional mutable list wrappers |
| `Seq` | Safe aggregation (tryMax, tryMin) |
| `Dictionary` | tryGetValue, getOrDefault |
| `Bytes` | XOR, concat, constant-time compare |
| `ArrayPool` | Pooled buffer helpers |
| `ContentHash` | SHA256, MD5 hashing |
| `Guard` | Argument validation |
| `Disposable` | RAII patterns for F# |
| `Types` | NonEmptyString, BoundedDict, BoundedSet |
| `ActivePatterns` | Parse.Int, Parse.Double, etc. |
| `Resilience` | Retry with exponential backoff |

## Design Principles

- **Idiomatic F#** -- follows FSharp.Core naming conventions
- **Inline everything** -- zero-cost abstractions via `[<InlineIfLambda>]`
- **No dependencies** -- only FSharp.Core
- **XML docs** -- works with IDE tooltips and fsdocs

## License

[Unlicense](https://github.com/techiedesu/TDesu.FSharp/blob/main/LICENSE) — public domain
