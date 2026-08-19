namespace TDesu.FSharp.Concurrency

open System
open System.Threading.Tasks

/// <summary>
/// One-shot async signal. Wraps <see cref="TaskCompletionSource{T}"/> for idiomatic F# async coordination.
/// </summary>
/// <remarks>
/// Thread-safe — uses <c>RunContinuationsAsynchronously</c> to avoid inline continuations.
/// </remarks>
[<Sealed>]
type Signal() =
    let tcs =
        TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)

    /// Completes the signal, releasing all waiters. Idempotent.
    member _.Set() = tcs.TrySetResult() |> ignore
    /// Returns a task that completes when the signal is set.
    member _.Wait() : Task = tcs.Task

    /// Returns a task that completes when the signal is set, with a timeout.
    /// Returns true if signaled, false if timed out.
    /// <param name="timeout">The maximum duration to wait for the signal.</param>
    member _.Wait(timeout: TimeSpan) : Task<bool> =
        task {
            if tcs.Task.IsCompleted then
                return true
            else
                use delayCts = new System.Threading.CancellationTokenSource()
                let! completed = Task.WhenAny(tcs.Task, Task.Delay(timeout, delayCts.Token))
                let signaled = obj.ReferenceEquals(completed, tcs.Task)

                if signaled then
                    delayCts.Cancel()

                return signaled
        }

    /// Whether the signal has been set.
    member _.IsSet = tcs.Task.IsCompleted
