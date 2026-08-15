// Retry, CircuitBreaker, and Memoize -- the resilience patterns for code that talks
// to something unreliable. Delays here are milliseconds, not seconds, so the whole
// script runs in well under a second.
#load "_prelude.fsx"
open Prelude
open System
open System.Threading
open System.Threading.Tasks
open TDesu.FSharp
open TDesu.FSharp.Tasks
open TDesu.FSharp.Resilience

// ── Retry: exponential backoff, retries until success or attempts run out ────────
let retrySucceedsOnThirdTry () =
    let mutable attempts = 0
    let f () =
        task {
            attempts <- attempts + 1
            if attempts < 3 then failwith $"transient failure #{attempts}"
            return attempts
        }
    Retry.withBackoff 5 (TimeSpan.FromMilliseconds 1.0) CancellationToken.None f |> Task.getResult

assertEqual "Retry.withBackoff keeps going until f stops throwing" 3 (retrySucceedsOnThirdTry ())

let alwaysFails () : Task<int> = task { return failwith "boom" }
let exhausted = Retry.tryWithBackoff 2 (TimeSpan.FromMilliseconds 1.0) CancellationToken.None alwaysFails |> Task.getResult
assertTrue "Retry.tryWithBackoff returns Error instead of throwing once retries run out" (Result.isError exhausted)

// ── CircuitBreaker: stop calling a dependency after too many straight failures ───
let breaker = CircuitBreaker.create { Threshold = 2; Cooldown = TimeSpan.FromMinutes 5.0 }
let mutable calls = 0
let failing () : Task<int> = task { calls <- calls + 1; return failwith "down" }

let attempt () =
    try
        breaker failing |> Task.getResult |> ignore
        true
    with _ ->
        false

assertTrue "1st failure propagates -- breaker is still Closed" (not (attempt ()))
assertTrue "2nd failure propagates and trips the breaker to Open" (not (attempt ()))
assertTrue "3rd call is rejected by the now-open breaker" (not (attempt ()))
assertEqual "the open breaker never invoked the wrapped function a 3rd time" 2 calls

// ── Memoize: cache a function's results by key, optionally with a TTL ────────────
let mutable plainCalls = 0
let expensive = Memoize.create (fun (x: int) -> plainCalls <- plainCalls + 1; x * x)
assertEqual "first call computes" 16 (expensive 4)
assertEqual "second call with the same key is served from cache" 16 (expensive 4)
assertEqual "the underlying function ran exactly once" 1 plainCalls

let mutable ttlCalls = 0
let shortLived = Memoize.withTtl (TimeSpan.FromMilliseconds 5.0) (fun (x: int) -> ttlCalls <- ttlCalls + 1; x * x)
shortLived 4 |> ignore
Thread.Sleep 20
shortLived 4 |> ignore
assertEqual "withTtl recomputes once the cached entry has expired" 2 ttlCalls

printfn "05-resilience.fsx: all assertions passed"
