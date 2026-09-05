namespace TDesu.FSharp.Concurrency

open System
open System.Threading.Tasks

/// <summary>
/// Periodic background timer — runs an action at fixed intervals with cancellation.
/// </summary>
[<RequireQualifiedAccess>]
module PeriodicTimer =
    /// Starts a background loop that runs action every interval.
    /// <param name="interval">The delay between each tick.</param>
    /// <param name="action">The async action to execute on each tick.</param>
    /// <param name="ct">The cancellation token to stop the loop.</param>
    /// <param name="onError">Handler invoked when the action throws a non-cancellation exception.</param>
    let start
        (interval: TimeSpan)
        (action: unit -> Task<unit>)
        (ct: Threading.CancellationToken)
        (onError: exn -> unit)
        =
        let t =
            task {
                while not ct.IsCancellationRequested do
                    try
                        do! Task.Delay(interval, ct)
                        do! action ()
                    with
                    | :? OperationCanceledException -> ()
                    | ex ->
                        try
                            onError ex
                        with _ ->
                            ()
            }

        t :> Task

    /// Like start, but action receives a counter (0-based) for each tick.
    /// <param name="interval">The delay between each tick.</param>
    /// <param name="action">The async action to execute, receiving the current tick index.</param>
    /// <param name="ct">The cancellation token to stop the loop.</param>
    /// <param name="onError">Handler invoked when the action throws a non-cancellation exception.</param>
    let startCounted
        (interval: TimeSpan)
        (action: int -> Task<unit>)
        (ct: Threading.CancellationToken)
        (onError: exn -> unit)
        =
        let mutable tick = 0

        let t =
            task {
                while not ct.IsCancellationRequested do
                    try
                        do! Task.Delay(interval, ct)
                        do! action tick
                        tick <- tick + 1
                    with
                    | :? OperationCanceledException -> ()
                    | ex ->
                        try
                            onError ex
                        with _ ->
                            ()
            }

        t :> Task

    /// Runs action at once, then again interval after each run ends, until ct fires.
    /// A run that throws is reported to onError and the next run still happens. Only a
    /// cancellation that is ct's own ends the loop quietly; an OperationCanceledException
    /// raised for any other reason — an inner deadline, a foreign token — is a failure like
    /// any other, so it is reported and waited out rather than spun on.
    /// <param name="interval">The delay after each run ends, before the next one starts.</param>
    /// <param name="action">The async action to execute on each run.</param>
    /// <param name="ct">The cancellation token to stop the loop.</param>
    /// <param name="onError">Handler invoked when a run throws. The next run still happens.</param>
    let startImmediate
        (interval: TimeSpan)
        (action: unit -> Task<unit>)
        (ct: Threading.CancellationToken)
        (onError: exn -> unit)
        : Task =
        let t =
            task {
                while not ct.IsCancellationRequested do
                    try
                        do! action ()
                    with
                    | :? OperationCanceledException when ct.IsCancellationRequested -> ()
                    | ex ->
                        try
                            onError ex
                        with _ ->
                            ()

                    try
                        do! Task.Delay(interval, ct)
                    with :? OperationCanceledException ->
                        ()
            }

        t :> Task

    /// Runs step at once and again after the pause it answers with, until ct fires.
    /// A step that throws is reported to onError, whose answer is the pause before the next
    /// attempt. Only a cancellation that is ct's own ends the loop quietly; an
    /// OperationCanceledException raised for any other reason — an inner deadline, a foreign
    /// token — goes to onError like any other failure, so a step that keeps being cancelled
    /// is paused between attempts rather than spun on.
    /// <param name="step">The async step to execute; its result is the pause before the next run.</param>
    /// <param name="ct">The cancellation token to stop the loop.</param>
    /// <param name="onError">Handler invoked when a step throws; its return value is the pause before the next attempt.</param>
    let run (step: unit -> Task<TimeSpan>) (ct: Threading.CancellationToken) (onError: exn -> TimeSpan) : Task =
        let t =
            task {
                while not ct.IsCancellationRequested do
                    try
                        let! pause = step ()
                        do! Task.Delay(pause, ct)
                    with
                    | :? OperationCanceledException when ct.IsCancellationRequested -> ()
                    | ex ->
                        try
                            do! Task.Delay(onError ex, ct)
                        with _ ->
                            ()
            }

        t :> Task
