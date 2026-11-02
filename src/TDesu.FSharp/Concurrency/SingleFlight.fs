namespace TDesu.FSharp.Concurrency

open System
open System.Collections.Generic
open System.Threading.Tasks

/// <summary>
/// One run per key at a time. A call for a key whose work is already running joins that run instead
/// of starting a second one, and gets the same value — or the same exception, or the same
/// cancellation. Once the run completes the key is free again: the next call starts a fresh run.
/// </summary>
/// <remarks>
/// <para>
/// Nothing is remembered after a run completes, which is what separates this from a cache: a failed
/// run is not replayed to later callers, and a successful one is not served to them either. Put the
/// result in a cache from inside the work if it should outlive the run (see <c>Memoize</c>).
/// </para>
/// <para>
/// The work runs on its first caller's terms — it is that caller's delegate, closing over that
/// caller's cancellation token if it uses one. A joining caller that wants to stop waiting sooner
/// bounds its own wait with <c>WaitAsync(ct)</c>; the run carries on for everyone else.
/// </para>
/// <para>
/// The key is freed before the run's result is published, so a continuation that calls
/// <see cref="Run"/> for the same key again starts a new run rather than joining the finished one.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// let connections = SingleFlight&lt;int, Connection&gt;()
/// // Three concurrent requests for DC 4 open one connection between them.
/// let! conn = connections.Run(4, fun () -> openConnection 4)
/// </code>
/// </example>
[<Sealed>]
type SingleFlight<'TKey, 'TValue>(comparer: IEqualityComparer<'TKey>) =
    let gate = obj ()
    let inFlight = Dictionary<'TKey, Task<'TValue>>(comparer)

    let free (key: 'TKey) =
        lock gate (fun () -> inFlight.Remove key |> ignore)

    new() = SingleFlight<'TKey, 'TValue>(EqualityComparer<'TKey>.Default)

    /// <summary>
    /// The value of <paramref name="work"/> for <paramref name="key"/>: from the run already in
    /// progress for that key if there is one, otherwise from a run this call starts.
    /// </summary>
    /// <param name="key">What identifies the work; equal keys share a run.</param>
    /// <param name="work">Started only when no run for <paramref name="key"/> is in progress.</param>
    member _.Run(key: 'TKey, work: unit -> Task<'TValue>) : Task<'TValue> =
        let mutable owner: TaskCompletionSource<'TValue> = null

        let shared =
            lock
                gate
                (fun () ->
                    match inFlight.TryGetValue key with
                    | true, running -> running
                    | _ ->
                        owner <- TaskCompletionSource<'TValue>(TaskCreationOptions.RunContinuationsAsynchronously)
                        inFlight[key] <- owner.Task
                        owner.Task
                )

        if not (isNull owner) then
            // Every outcome is caught and handed to the shared task, so nothing escapes unobserved
            // and the key is always freed — a run that threw synchronously included.
            task {
                try
                    let! value = work ()
                    free key
                    owner.SetResult value
                with
                | :? OperationCanceledException as oce ->
                    free key
                    owner.TrySetCanceled oce.CancellationToken |> ignore
                | ex ->
                    free key
                    owner.SetException ex
            }
            |> ignore

        shared

    /// Whether a run for <paramref name="key"/> is in progress.
    /// <param name="key">The key to check.</param>
    member _.IsRunning(key: 'TKey) : bool =
        lock gate (fun () -> inFlight.ContainsKey key)

    /// How many keys have a run in progress.
    member _.Count: int = lock gate (fun () -> inFlight.Count)
