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
    /// Runs <paramref name="work"/> with a deadline, enforced cooperatively. <paramref name="work"/>
    /// is handed a token cancelled once <paramref name="duration"/> elapses, and
    /// <see cref="TimeoutException"/> is thrown once <paramref name="work"/>'s task completes after
    /// observing that cancellation.
    /// </summary>
    /// <remarks>
    /// The deadline is not forcibly enforced: work that never checks the token, or that keeps running
    /// after the token is cancelled, keeps running to completion, and its result (or exception) is
    /// returned as-is — nothing here aborts it. This is deliberate: there is no safe way to abandon
    /// in-flight work without either orphaning it or racing its own cleanup.
    /// </remarks>
    /// <exception cref="System.TimeoutException">When <paramref name="work"/>'s task ends with the cancellation this deadline raised.</exception>
    /// <param name="duration">Maximum time allowed before the operation is cancelled.</param>
    /// <param name="work">Async work that receives a cancellation token linked to the deadline.</param>
    let after (duration: TimeSpan) (work: CancellationToken -> Task<'T>) : Task<'T> =
        task {
            use cts = new CancellationTokenSource(duration)

            try
                return! work cts.Token
            with :? OperationCanceledException as oce when
                oce.CancellationToken = cts.Token && cts.IsCancellationRequested ->
                return timedOutf "Operation exceeded %gms" duration.TotalMilliseconds
        }

    /// <summary>
    /// Runs <paramref name="work"/> with a deadline linked to a parent cancellation token, enforced
    /// cooperatively — see <see cref="after"/> for what that means.
    /// </summary>
    /// <remarks>
    /// Which of the two tokens caused a cancellation is decided from the exception's own
    /// <see cref="System.Threading.CancellationToken"/> (the linked token <paramref name="work"/> was
    /// given), not from re-reading <paramref name="parentCt"/>'s current state — state that
    /// <paramref name="work"/> itself may have changed by the time this is checked. A cancellation
    /// raised through the linked token while the deadline has fired is reported as
    /// <see cref="TimeoutException"/> even if <paramref name="parentCt"/> is also cancelled by then;
    /// one raised through any other token is never relabelled as a timeout.
    /// </remarks>
    /// <exception cref="System.TimeoutException">When <paramref name="work"/>'s task ends with the cancellation this deadline raised.</exception>
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
            with :? OperationCanceledException as oce when
                oce.CancellationToken = linked.Token && cts.IsCancellationRequested ->
                return timedOutf "Operation exceeded %gms" duration.TotalMilliseconds
        }
