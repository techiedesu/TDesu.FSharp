namespace TDesu.FSharp.Resilience

open System
open System.Threading.Tasks

/// <summary>
/// Simple circuit breaker — prevents cascading failures by tracking consecutive errors.
/// </summary>
/// <remarks>
/// Thread-safe: state transitions are taken under a <c>lock</c>.
/// State transitions: <c>Closed → Open → HalfOpen → Closed</c>.
/// </remarks>
[<RequireQualifiedAccess>]
module CircuitBreaker =

    /// Circuit breaker state.
    type State =
        | Closed of failures: int
        | Open of resetAt: DateTime
        | HalfOpen

    /// Circuit breaker configuration.
    type Config = {
        /// Number of consecutive failures before opening the circuit.
        Threshold: int

        /// How long the circuit stays open before allowing a probe.
        Cooldown: TimeSpan
    }

    /// <summary>
    /// Creates a circuit breaker. Returns a function that wraps calls.
    /// </summary>
    /// <remarks>
    /// State transitions are atomic; the wrapped function <c>f</c> runs outside the lock.
    /// After <see cref="Config.Threshold"/> consecutive failures, the circuit opens for <see cref="Config.Cooldown"/>.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">Thrown when calling through an open circuit.</exception>
    /// <param name="config">Circuit breaker configuration specifying threshold and cooldown.</param>
    let create (config: Config) =
        let state = ref (Closed 0)
        let sync = obj ()
        let inline writeState v = lock sync (fun () -> state.Value <- v)
        let inline atomicCheckAndTransition () =
            lock sync (fun () ->
                let now = DateTime.UtcNow
                match state.Value with
                | Open resetAt when now < resetAt -> state.Value
                | Open _ -> state.Value <- HalfOpen; HalfOpen
                | s -> s)
        let inline atomicRecordFailure () =
            lock sync (fun () ->
                let now = DateTime.UtcNow
                match state.Value with
                | Closed failures ->
                    let n = failures + 1
                    state.Value <-
                        if n >= config.Threshold then Open(now.Add config.Cooldown)
                        else Closed n
                | _ ->
                    state.Value <- Open(now.Add config.Cooldown)
                state.Value)

        fun (f: unit -> Task<'T>) -> task {
            match atomicCheckAndTransition () with
            | Open _ ->
                return invalidOp "Circuit is open"
            | HalfOpen ->
                try
                    let! result = f ()
                    writeState (Closed 0)
                    return result
                with ex ->
                    let _ = atomicRecordFailure ()
                    return raise ex
            | Closed failures ->
                try
                    let! result = f ()
                    if failures > 0 then
                        writeState (Closed 0)
                    return result
                with ex ->
                    let _ = atomicRecordFailure ()
                    return raise ex
        }
