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
