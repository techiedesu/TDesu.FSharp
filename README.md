# TDesu.FSharp

[![NuGet](https://img.shields.io/nuget/v/TDesu.FSharp.svg)](https://www.nuget.org/packages/TDesu.FSharp)
[![Build](https://github.com/techiedesu/TDesu.FSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/techiedesu/TDesu.FSharp/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Practical F# utility library. Extends FSharp.Core with the functions you keep rewriting.

No Haskell jargon. Clear names. Inline everything.

## Install

```
dotnet add package TDesu.FSharp
```

## Modules

### Operators

```fsharp
open TDesu.FSharp

// ^ -- apply without parens
raise ^ exn "something went wrong"
log.LogInformation("count {N}", string ^ items.Length)

// %~ -- ignore return value
%httpClient.SendAsync(req)

// konst -- always the same value
items |> List.map (konst 0)   // [0; 0; 0; ...]
```

### String

```fsharp
"hello world" |> String.contains "world"     // true
"  padded  "  |> String.trim                 // "padded"
"a,b,c"       |> String.split ","            // [|"a";"b";"c"|]
"hello world" |> String.replace "world" "F#" // "hello F#"
"long text"   |> String.truncate 4           // "long"
["a";"b";"c"] |> String.join ", "            // "a, b, c"
"hello"       |> String.toOption             // Some "hello"
""            |> String.toOption             // None
```

### Option

```fsharp
// Convert to Result
Some 42 |> Option.toResult "missing"   // Ok 42
None    |> Option.toResult "missing"   // Error "missing"

// Combine options
Option.zip (Some 1) (Some "a")          // Some (1, "a")
Option.map2 (+) (Some 1) (Some 2)       // Some 3
Option.map3 (fun a b c -> a+b+c) (Some 1) (Some 2) (Some 3) // Some 6

// Side-effects
Some 42 |> Option.tee (printfn "got %d") // prints, returns Some 42

// From strings
Option.ofString "hello"   // Some "hello"
Option.ofString null       // None
Option.ofString "  "       // None
```

### Result

```fsharp
// Extract values
Ok 42  |> Result.defaultValue 0             // 42
Error "x" |> Result.defaultValue 0          // 0
Ok 42  |> Result.valueOr (fun e -> e.Length) // 42

// Chain and recover
Error "x" |> Result.orElse (Ok 0)           // Ok 0
Error "x" |> Result.orElseWith (fun _ -> Ok 0) // Ok 0

// Convert
Result.ofOption "missing" (Some 42)          // Ok 42
Result.ofOption "missing" None               // Error "missing"
Ok 42 |> Result.toOption                     // Some 42

// Combine
Result.zip (Ok 1) (Ok 2)                    // Ok (1, 2)

// Validate
Result.requireTrue "must be positive" (x > 0)
Result.requireNotNull "null!" someValue

// Side-effects (great for logging)
result
|> Result.tee (fun v -> log.LogInformation("ok: {V}", v))
|> Result.teeError (fun e -> log.LogError("fail: {E}", e))

// Safe calls
Result.catch (fun () -> riskyOperation())    // Ok value or Error exn
```

### Task

```fsharp
// Transform
task { return 21 } |> Task.map ((*) 2)       // Task<42>
task { return 21 } |> Task.bind (fun v -> task { return v * 2 }) // Task<42>

// Combine (concurrent)
Task.zip (getUser()) (getOrders())            // Task<user * orders>
Task.zip3 t1 t2 t3                           // Task<a * b * c>
Task.map2 (+) (task { return 1 }) (task { return 2 }) // Task<3>

// Utilities
Task.singleton 42                             // Task.FromResult(42)
task { return 42 } |> Task.ignore             // Task<unit>
task { return riskyOp() } |> Task.catch       // Task<Result<_, exn>>
```

### TaskResult

Composable functions for `Task<Result<'a, 'e>>` -- the workhorse of async F#.

```fsharp
fetchUser userId
|> TaskResult.bind (fun user -> fetchOrders user.Id)
|> TaskResult.map (fun orders -> orders.Length)
|> TaskResult.tee (fun count -> log.LogInformation("orders: {N}", count))
|> TaskResult.defaultValue 0
```

### tryParse

Every numeric type + Guid, Bool, DateTimeOffset:

```fsharp
Int32.tryParse "42"              // Some 42
Int64.tryParse "9999999999"      // Some 9999999999L
Double.tryParse "3.14"           // Some 3.14
Guid.tryParse "..."              // Some guid
Boolean.tryParse "true"          // Some true
DateTimeOffset.tryParse "2026-01-15" // Some dto
```

### Parse Active Patterns

```fsharp
open TDesu.FSharp.ActivePatterns

match input with
| Parse.Int n    -> printfn "integer: %d" n
| Parse.Double d -> printfn "float: %f" d
| Parse.Guid g   -> printfn "guid: %A" g
| Parse.Bool b   -> printfn "bool: %b" b
| other          -> printfn "text: %s" other
```

### ResizeArray

Functional wrappers for `System.Collections.Generic.List<T>`:

```fsharp
ResizeArray.create ()
|> ResizeArray.add 1
|> ResizeArray.add 2
|> ResizeArray.add 3
|> ResizeArray.filter (fun x -> x > 1)
|> ResizeArray.map ((*) 10)
|> ResizeArray.toArray   // [| 20; 30 |]

// From other collections
ResizeArray.ofList [ 1; 2; 3 ]
ResizeArray.ofSeq (seq { 1..10 })

// Searching
ra |> ResizeArray.tryFind (fun x -> x > 5)
ra |> ResizeArray.tryItem 3
ra |> ResizeArray.exists (fun x -> x = 42)

// Mutating (pipeable)
ra |> ResizeArray.sort
   |> ResizeArray.removeWhere (fun x -> x < 0)
   |> ResizeArray.joinWith ", "
```

### Seq

Safe aggregation for empty sequences:

```fsharp
Seq.tryMax [| 3; 1; 5 |]     // Some 5
Seq.tryMin Seq.empty<int>    // None
Seq.tryAverage [ 2.0; 4.0 ]  // Some 3.0
```

### Bytes & ArrayPool

Low-level buffer utilities for performance-critical code:

```fsharp
open TDesu.FSharp.Buffers

// XOR operations
Bytes.xor [| 0xFFuy |] [| 0x0Fuy |]              // [| 0xF0uy |]
Bytes.xorBlock src srcOff key keyOff dst dstOff 16 // block XOR

// Fast concatenation (BlockCopy, no intermediate arrays)
Bytes.concat2 header payload
Bytes.concat3 part1 part2 part3

// Timing-safe comparison (for crypto)
Bytes.constantTimeEquals hash1 hash2

// Pooled buffers (avoid GC pressure)
ArrayPool.useBytes 1024 (fun buf ->
    stream.Read(buf, 0, 1024) |> ignore
    processBuffer buf)
```

### Computation Expressions

```fsharp
open TDesu.FSharp.ResultBuilder
open TDesu.FSharp.MaybeBuilder
open TDesu.FSharp.TaskResultBuilder

// result { } -- synchronous Result pipelines
let validate input = result {
    let! name = Result.requireNotNull "name required" input.Name
    let! age = Result.requireTrue "must be adult" (input.Age >= 18)
    return { Name = name; IsAdult = true }
}

// maybe { } -- Option pipelines
let tryGetFullName user = maybe {
    let! first = user.FirstName
    let! last = user.LastName
    return $"{first} {last}"
}

// taskResult { } -- async Result pipelines
let processOrder orderId = taskResult {
    let! order = fetchOrder orderId           // Task<Result>
    let! items = fetchItems order.Id          // Task<Result>
    let! _ = validateStock items              // Result
    let! receipt = chargePayment order.Total  // Task<Result>
    return receipt
}
```

### Dictionary

```fsharp
dict |> Dictionary.tryGetValue "key"        // Some value
dict |> Dictionary.getOrDefault "key" 0     // value or 0
```

### Types

```fsharp
open TDesu.FSharp.Types

let name = NonEmptyString.createOrFail "hello"
let raw  = NonEmptyString.value name          // "hello"
let len  = NonEmptyString.length name         // 5

// Safe creation
match NonEmptyString.create input with
| Ok nes -> use nes
| Error NonEmptyStringError.Null -> handleNull()
| Error NonEmptyStringError.Empty -> handleEmpty()
```

## Design Principles

- **Idiomatic F#** -- follows FSharp.Core naming: `map`, `bind`, `iter`, `tryX`, `ofX`, `toX`
- **Inline everything** -- zero-cost abstractions via `[<InlineIfLambda>]`
- **No dependencies** -- only FSharp.Core
- **XML docs on all public APIs** -- works with IDE tooltips and `fsdocs`

## License

[MIT](LICENSE)
