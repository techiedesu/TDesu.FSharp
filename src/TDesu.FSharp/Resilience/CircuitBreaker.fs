namespace TDesu.FSharp.Resilience

open System
open System.Diagnostics
open System.Runtime.ExceptionServices
open System.Threading.Tasks

/// <summary>
/// Simple circuit breaker — prevents cascading failures by tracking consecutive errors.
/// </summary>
/// <remarks>
/// Thread-safe: state transitions are taken under a <c>lock</c>.
/// State transitions: <c>Closed → Open → HalfOpen → Closed</c>. Exactly one probe is admitted while
/// <c>HalfOpen</c>; every other caller is rejected until that probe resolves. Cooldown is measured
/// with <see cref="System.Diagnostics.Stopwatch"/>, which is monotonic — unlike
/// <see cref="System.DateTime.UtcNow"/>, a backward wall-clock step (an NTP correction) cannot hold
/// the circuit open past its cooldown, nor a forward step close it early.
/// </remarks>
[<RequireQualifiedAccess>]
module CircuitBreaker =

    /// Circuit breaker state. A <c>Stopwatch</c> timestamp, not a wall-clock time, so it is only
    /// ever compared against <see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>.
    type private State =
        | Closed of failures: int
        | Open of resetAt: int64
        | HalfOpen

    /// What a caller may do, decided atomically by <c>atomicCheckAndTransition</c>.
    type private Decision =
        | RunClosed of failures: int
        | RunProbe
        | Reject

    /// Circuit breaker configuration.
    type Config = {
        /// Number of consecutive failures before opening the circuit.
        Threshold: int

        /// How long the circuit stays open before allowing a probe.
        Cooldown: TimeSpan
    }

    /// Converts a <see cref="System.TimeSpan"/> to an equivalent span of
    /// <see cref="System.Diagnostics.Stopwatch"/> ticks.
    let inline private stopwatchTicksOf (span: TimeSpan) : int64 =
        int64 (span.TotalSeconds * float Stopwatch.Frequency)

    /// Re-raises <paramref name="ex"/> with its original stack trace intact, usable where an
    /// expression of type <c>'a</c> is required (a <c>task</c> exception handler cannot use F#'s
    /// <c>reraise()</c>).
    /// <param name="ex">The exception to re-raise.</param>
    let inline private reraiseCaptured (ex: exn) : 'a =
        ExceptionDispatchInfo.Capture(ex).Throw()
        Unchecked.defaultof<'a>

    /// <summary>
    /// A circuit breaker returned by <see cref="create"/>. One instance tracks one dependency's
    /// health across calls of any result type.
    /// </summary>
    [<Sealed>]
    type Breaker internal (config: Config) =
        let state = ref (Closed 0)
        let sync = obj ()
        let cooldownTicks = stopwatchTicksOf config.Cooldown

        let atomicCheckAndTransition () =
            lock
                sync
                (fun () ->
                    match state.Value with
                    | Open resetAt when Stopwatch.GetTimestamp() < resetAt -> Reject
                    | Open _ ->
                        state.Value <- HalfOpen
                        RunProbe
                    | HalfOpen -> Reject
                    | Closed failures -> RunClosed failures
                )

        let atomicRecordFailure () =
            lock
                sync
                (fun () ->
                    match state.Value with
                    | Closed failures ->
                        let n = failures + 1

                        state.Value <-
                            if n >= config.Threshold then
                                Open(Stopwatch.GetTimestamp() + cooldownTicks)
                            else
                                Closed n
                    | _ -> state.Value <- Open(Stopwatch.GetTimestamp() + cooldownTicks)
                )

        // Never overwrites a state some other call has since reopened: a slow call that started
        // while Closed/HalfOpen and succeeds only after other calls tripped the breaker must not
        // silently re-close what they just opened.
        let writeStateIfNotOpen v =
            lock
                sync
                (fun () ->
                    match state.Value with
                    | Open _ -> ()
                    | _ -> state.Value <- v
                )

        /// <summary>
        /// Runs <paramref name="f"/> through the circuit breaker.
        /// </summary>
        /// <remarks>
        /// State transitions are atomic; <paramref name="f"/> runs outside the lock. After
        /// <see cref="Config.Threshold"/> consecutive failures, the circuit opens for
        /// <see cref="Config.Cooldown"/>; the first call past the cooldown is admitted as a single
        /// probe, and every other caller is rejected until that probe resolves.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when calling through an open circuit, or when a half-open probe is already in flight.
        /// </exception>
        /// <param name="f">The call to guard.</param>
        member _.Run<'T>(f: unit -> Task<'T>) : Task<'T> =
            task {
                match atomicCheckAndTransition () with
                | Reject -> return invalidOp "Circuit is open"
                | RunProbe ->
                    try
                        let! result = f ()
                        writeStateIfNotOpen (Closed 0)
                        return result
                    with ex ->
                        atomicRecordFailure ()
                        return reraiseCaptured ex
                | RunClosed failures ->
                    try
                        let! result = f ()

                        if failures > 0 then
                            writeStateIfNotOpen (Closed 0)

                        return result
                    with ex ->
                        atomicRecordFailure ()
                        return reraiseCaptured ex
            }

    /// <summary>
    /// Creates a circuit breaker that wraps calls through <see cref="Breaker.Run"/>.
    /// </summary>
    /// <param name="config">Circuit breaker configuration specifying threshold and cooldown.</param>
    let create (config: Config) = Breaker(config)
