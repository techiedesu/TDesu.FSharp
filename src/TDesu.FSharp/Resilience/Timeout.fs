namespace TDesu.FSharp.Resilience

open System
open System.Threading
open System.Threading.Tasks
open TDesu.FSharp.Operators

/// <summary>
/// Timeout combinators — enforce deadlines on async operations.
/// </summary>
[<RequireQualifiedAccess>]
module Timeout =
    /// <summary>
    /// Runs <paramref name="work"/> with a hard deadline. Throws <see cref="TimeoutException"/> if exceeded.
    /// Propagates <see cref="CancellationToken"/> so underlying work can cooperatively stop.
    /// </summary>
    /// <exception cref="System.TimeoutException">When the operation exceeds <paramref name="duration"/>.</exception>
    /// <param name="duration">Maximum time allowed before the operation is cancelled.</param>
    /// <param name="work">Async work that receives a cancellation token linked to the deadline.</param>
    let after (duration: TimeSpan) (work: CancellationToken -> Task<'T>) : Task<'T> =
        task {
            use cts = new CancellationTokenSource(duration)

            try
                return! work cts.Token
            with :? OperationCanceledException when cts.IsCancellationRequested ->
                return timedOutf "Operation exceeded %gms" duration.TotalMilliseconds
        }

    /// Runs work with a deadline, linked to a parent cancellation token.
    /// <param name="duration">Maximum time allowed before the operation is cancelled.</param>
    /// <param name="parentCt">Parent token that can also trigger cancellation.</param>
    /// <param name="work">Async work that receives a cancellation token linked to both the deadline and the parent token.</param>
    let afterLinked
        (duration: TimeSpan)
        (parentCt: CancellationToken)
        (work: CancellationToken -> Task<'T>)
        : Task<'T> =
        task {
            use cts = new CancellationTokenSource(duration)
            use linked = CancellationTokenSource.CreateLinkedTokenSource(parentCt, cts.Token)

            try
                return! work linked.Token
            with :? OperationCanceledException when cts.IsCancellationRequested && not parentCt.IsCancellationRequested ->
                return timedOutf "Operation exceeded %gms" duration.TotalMilliseconds
        }
