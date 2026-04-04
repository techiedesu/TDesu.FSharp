# TDesu.FSharp

[![NuGet](https://img.shields.io/nuget/v/TDesu.FSharp.svg)](https://www.nuget.org/packages/TDesu.FSharp)
[![Build](https://github.com/techiedesu/TDesu.FSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/techiedesu/TDesu.FSharp/actions/workflows/ci.yml)
[![License: Unlicense](https://img.shields.io/badge/License-Unlicense-blue.svg)](https://unlicense.org)
[![API Docs](https://img.shields.io/badge/docs-fsdocs-blueviolet)](https://techiedesu.github.io/TDesu.FSharp/)

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

// % -- ignore return value
%httpClient.SendAsync(req)

// always -- constant function
items |> List.map (always 0)   // [0; 0; 0; ...]

// tee -- side-effect and passthrough
value |> tee (printfn "got: %A")

// icast / ecast -- implicit/explicit casts
let header : Microsoft.Extensions.Primitives.StringValues = icast "text/csv"
```

### Guard

```fsharp
Guard.notNull "arg" value
Guard.notEmpty "name" str
Guard.inRange "port" 1 65535 port
Guard.positive "count" n
```

### UnixTime

```fsharp
UnixTime.seconds ()        // int64, cached high-resolution
UnixTime.milliseconds ()   // int64
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
"abc"         |> String.countOccurrences "a" // 1
"hello"       |> String.toUpperInv           // "HELLO"
```

### Option

```fsharp
Some 42 |> Option.toResult "missing"   // Ok 42
None    |> Option.toResult "missing"   // Error "missing"

Option.zip (Some 1) (Some "a")          // Some (1, "a")
Option.map2 (+) (Some 1) (Some 2)       // Some 3
Some 42 |> Option.tee (printfn "got %d") // prints, returns Some 42

Option.ofString "hello"   // Some "hello"
Option.ofString null       // None
Option.ofString "  "       // None
```

### Result

```fsharp
Ok 42  |> Result.defaultValue 0             // 42
Ok 42  |> Result.valueOr (fun e -> e.Length) // 42
Error "x" |> Result.orElse (Ok 0)           // Ok 0

Result.ofOption "missing" (Some 42)          // Ok 42
Result.zip (Ok 1) (Ok 2)                    // Ok (1, 2)

Result.requireTrue "must be positive" (x > 0)
Result.requireNotNull "null!" someValue
Result.catch (fun () -> riskyOperation())    // Ok value or Error exn

result
|> Result.tee (fun v -> log.LogInformation("ok: {V}", v))
|> Result.teeError (fun e -> log.LogError("fail: {E}", e))
```

### Validation (applicative)

Collects all errors instead of short-circuiting:

```fsharp
open TDesu.FSharp

validation {
    let! name = validateName input.Name
    and! email = validateEmail input.Email
    and! age = validateAge input.Age
    return { Name = name; Email = email; Age = age }
}
// Validation.Ok { ... } or Validation.Error [ err1; err2; ... ]
```

### Task

```fsharp
task { return 21 } |> Task.map ((*) 2)       // Task<42>
task { return 21 } |> Task.bind (fun v -> task { return v * 2 })

Task.zip (getUser()) (getOrders())            // Task<user * orders>
Task.zip3 t1 t2 t3                           // Task<a * b * c>
Task.singleton 42                             // Task.FromResult(42)
task { return 42 } |> Task.ignore             // Task<unit>
task { return riskyOp() } |> Task.catch       // Task<Result<_, exn>>

// Fire-and-forget with error handler
Task.fireAndForget (fun ex -> log.Error(ex)) (fun () -> sendEmail())

// Throttled parallelism
Task.parallelThrottle 5 urls (fun url -> httpClient.GetAsync(url))
```

### TaskResult

Composable functions for `Task<Result<'a, 'e>>`:

```fsharp
fetchUser userId
|> TaskResult.bind (fun user -> fetchOrders user.Id)
|> TaskResult.map (fun orders -> orders.Length)
|> TaskResult.tee (fun count -> log.LogInformation("orders: {N}", count))
|> TaskResult.defaultValue 0
```

### TaskGroup (structured concurrency)

```fsharp
use group = new TaskGroup(ct)
group.Run(fun ct -> fetchUserAsync ct)
group.Run(fun ct -> fetchOrdersAsync ct)
do! group.WaitAll()  // throws AggregateException if any failed
```

### Computation Expressions

```fsharp
open TDesu.FSharp.Builders

// result { } -- synchronous Result pipelines
let validate input = result {
    let! name = Result.requireNotNull "name required" input.Name
    do! Result.requireTrue "must be adult" (input.Age >= 18)
    return { Name = name; IsAdult = true }
}

// option { } -- Option pipelines
let tryGetFullName user = option {
    let! first = user.FirstName
    let! last = user.LastName
    return $"{first} {last}"
}

// taskResult { } -- async Result pipelines (binds Task<Result>, Result, and Task)
let processOrder orderId = taskResult {
    let! order = fetchOrder orderId           // Task<Result>
    let! items = fetchItems order.Id          // Task<Result>
    do! validateStock items                   // Result
    let! receipt = chargePayment order.Total  // Task<Result>
    return receipt
}
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

match str with
| String.NullOrWhiteSpace -> // handle empty
| _ -> // handle value
```

### Collections

```fsharp
// Dictionary
dict |> Dictionary.tryGetValue "key"        // Some value
dict |> Dictionary.getOrDefault "key" 0     // value or 0

// Safe aggregation
Seq.tryMax [| 3; 1; 5 |]     // Some 5
Seq.tryMin Seq.empty<int>    // None
Seq.tryAverage [ 2.0; 4.0 ]  // Some 3.0

// ResizeArray (pipeable wrappers for List<T>)
ResizeArray.ofList [ 1; 2; 3 ]
|> ResizeArray.filter (fun x -> x > 1)
|> ResizeArray.map ((*) 10)
|> ResizeArray.toArray   // [| 20; 30 |]
```

### Concurrency

```fsharp
open TDesu.FSharp.Concurrency

// Thread-safe counters
let counter = AtomicInt64()
counter.Increment()

// Bounded collections (auto-evict oldest)
let cache = BoundedDict<string, int>(100)
cache.Set("key", 42)

// One-shot signal
let signal = Signal()
signal.Set()
do! signal.Wait()

// Background periodic work
PeriodicTimer.start (TimeSpan.FromSeconds 60.0) (fun () -> task {
    log.Info("tick")
}) ct onError

// Sequential background worker
let worker = ChannelWorker.start processItem onError ct
worker.Post(item)

// Rate limiting
let limiter = SlidingWindowLimiter(100, TimeSpan.FromMinutes 1.0)
match limiter.TryAcquire() with
| Ok () -> processRequest()
| Error waitTime -> return TooManyRequests waitTime
```

### Resilience

```fsharp
open TDesu.FSharp.Resilience

// Retry with exponential backoff
let! result = Retry.withBackoff 3 (TimeSpan.FromMilliseconds 500.0) ct (fun () ->
    httpClient.GetAsync(url))

// Circuit breaker
let breaker = CircuitBreaker.create { Threshold = 5; Cooldown = TimeSpan.FromSeconds 30.0 }
let! data = breaker (fun () -> callExternalApi())

// Timeout with cancellation
let! data = Timeout.afterLinked (TimeSpan.FromSeconds 10.0) ct (fun ct ->
    slowOperation ct)

// Memoize with TTL
let cachedFetch = Memoize.withTtlAsync (TimeSpan.FromMinutes 5.0) fetchExpensiveData

// Saga (transactional orchestration with compensation)
let! result = Saga.run [
    Saga.step "create-order" createOrder compensateOrder
    Saga.step "charge-payment" chargePayment refundPayment
    Saga.step "send-email" sendEmail (fun _ -> Task.singleton ())
] initialCtx
```

### I/O & Disposable

```fsharp
open TDesu.FSharp.IO

Env.getVar "API_KEY"           // string option
Env.requireVar "DATABASE_URL"  // throws if missing

// Deferred cleanup (like Go's defer)
use cleanup = Disposable.deferStack ()
cleanup.AddDisposable(connection)
cleanup.Add(fun () -> log.Info("done"))

// Temporary file that auto-deletes
use tfs = new TemporaryFileStream()
tfs.Write(data, 0, data.Length)
```

### Bytes & ArrayPool

```fsharp
open TDesu.FSharp.Buffers

Bytes.xor [| 0xFFuy |] [| 0x0Fuy |]     // [| 0xF0uy |]
Bytes.concat2 header payload
Bytes.constantTimeEquals hash1 hash2      // timing-safe

ArrayPool.useBytes 1024 (fun buf ->
    stream.Read(buf, 0, 1024) |> ignore)
```

### Hashing

```fsharp
open TDesu.FSharp.Hashing

ContentHash.sha256Hex "hello"             // "2cf24dba..."
ContentHash.md5Hex "hello"               // "5d41402a..."
ContentHash.sha1Hex [| 0uy; 1uy |]       // hex string

// Structural hash combining
Hash.combine2 key1 key2
Hash.ofList [ "a"; "b"; "c" ]
```

### Types

```fsharp
open TDesu.FSharp.Types

let name = NonEmptyString.createOrFail "hello"
let raw  = NonEmptyString.value name          // "hello"

// API response wrapper
ApiResponse.ok data        // { Success = true; Data = Some data; Error = None }
ApiResponse.ofResult result // auto-convert Result to ApiResponse
```

### State Machine

```fsharp
let definition =
    let b = StateMachine.Builder()
    b.StateTag(fun s -> match s with Idle -> 0 | Running _ -> 1 | Done -> 2)
    b.EventTag(fun e -> match e with Start _ -> 0 | Finish -> 1)
    b.On(0, 0, fun _state event -> StateMachine.goto (Running event.Data) [LogStarted])
    b.On(1, 1, fun _state _event -> StateMachine.goto Done [LogFinished])
    b.Build()

let result = StateMachine.apply definition currentState event
```

### Clock

```fsharp
// Production
let clock = SystemClock.Instance

// Testing
let fake = FakeClock(DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
fake.Advance(TimeSpan.FromHours 1.0)
```

## Design Principles

- **Idiomatic F#** -- follows FSharp.Core naming: `map`, `bind`, `iter`, `tryX`, `ofX`, `toX`
- **Inline everything** -- zero-cost abstractions via `[<InlineIfLambda>]`
- **No dependencies** -- only FSharp.Core
- **Fable compatible** -- sources included in nupkg for JS/TS transpilation
- **XML docs on all public APIs** -- works with IDE tooltips and [fsdocs](https://techiedesu.github.io/TDesu.FSharp/)

## Acknowledgments

Built with assistance from [Claude](https://claude.ai) (Anthropic).

## License

[Unlicense](LICENSE)
