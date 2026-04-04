# TDesu.FSharp

Practical F# utility library. Extends FSharp.Core with the functions you keep rewriting.

No Haskell jargon. Clear names. Inline everything.

## Install

```
dotnet add package TDesu.FSharp
```

## What's Inside

| Namespace | Highlights |
|---|---|
| `TDesu.FSharp` | `^` (apply), `%` (ignore), `always`, `tee`, `icast`/`ecast`, `Guard`, `UnixTime`, `String`, `Option`, `Result` |
| `TDesu.FSharp.Builders` | `result { }`, `option { }`, `taskResult { }` computation expressions |
| `TDesu.FSharp.Tasks` | `Task.map/bind/zip/zip3/catch`, `TaskResult.*`, `TaskGroup`, `Task.parallelThrottle`, `Task.fireAndForget` |
| `TDesu.FSharp.Collections` | `Dictionary.tryGetValue`, `ResizeArray.*`, `Seq.tryMax/tryMin/tryAverage`, `Stack.*` |
| `TDesu.FSharp.Concurrency` | `AtomicInt64`, `BoundedDict`, `BoundedQueue`, `Signal`, `PeriodicTimer`, `ChannelWorker`, `SlidingWindowLimiter` |
| `TDesu.FSharp.Resilience` | `Retry.withBackoff`, `CircuitBreaker`, `Timeout.afterLinked`, `Memoize.withTtlAsync`, `Saga.run` |
| `TDesu.FSharp.IO` | `Env.getVar/requireVar`, `Disposable.deferStack`, `TemporaryFileStream`, `File/Directory` helpers |
| `TDesu.FSharp.Buffers` | `Bytes.xor/concat/constantTimeEquals`, `ArrayPool.useBytes` |
| `TDesu.FSharp.Hashing` | `ContentHash.sha256Hex/md5Hex/sha1Hex`, `Hash.combine2/3/4` |
| `TDesu.FSharp.ActivePatterns` | `Parse.Int/Double/Guid/Bool`, `String.NullOrWhiteSpace/Empty` |
| `TDesu.FSharp.Types` | `NonEmptyString`, `ApiResponse` |
| `TDesu.FSharp` | `Validation` (applicative with `and!`), `StateMachine`, `Clock`/`FakeClock`, `NumericParsing` |

## Quick Example

```fsharp
open TDesu.FSharp
open TDesu.FSharp.Builders
open TDesu.FSharp.Resilience

let getUser (http: HttpClient) (userId: string) = taskResult {
    let! user =
        Retry.withBackoff 3 (TimeSpan.FromMilliseconds 500.0) ct (fun () ->
            http.GetFromJsonAsync<User>($"/api/users/{userId}"))
        |> Task.catch
        |> TaskResult.mapError (fun ex -> $"fetch failed: {ex.Message}")

    do! Result.requireTrue "user inactive" user.IsActive
    return user
}
```

## Links

- [Full documentation & API reference](https://techiedesu.github.io/TDesu.FSharp/)
- [GitHub](https://github.com/techiedesu/TDesu.FSharp)

## License

[Unlicense](https://unlicense.org) -- public domain.
