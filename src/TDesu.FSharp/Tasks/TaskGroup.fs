namespace TDesu.FSharp.Tasks

open System
open System.Threading.Tasks

#if !FABLE_COMPILER
/// <summary>
/// Structured concurrency: run multiple tasks, cancel all on first failure.
/// Similar to Go's <c>errgroup</c>.
/// </summary>
/// <example>
/// <code>
/// use group = new TaskGroup()
/// let mutable user = Unchecked.defaultof&lt;_&gt;
/// let mutable orders = Unchecked.defaultof&lt;_&gt;
/// group.Run(fun ct -> task { let! u = fetchUser ct in user &lt;- u })
/// group.Run(fun ct -> task { let! o = fetchOrders ct in orders &lt;- o })
/// do! group.WaitAll()
/// </code>
/// </example>
[<Sealed>]
type TaskGroup private (cts: System.Threading.CancellationTokenSource) =
    let tasks = ResizeArray<Task>()
    let errors = System.Collections.Concurrent.ConcurrentBag<exn>()

    /// Creates a TaskGroup with its own CancellationTokenSource.
    new() = new TaskGroup(new System.Threading.CancellationTokenSource())

    /// Creates a TaskGroup linked to a parent CancellationToken.
    /// <param name="parentToken">Parent token — group cancels when parent does.</param>
    new(parentToken: System.Threading.CancellationToken) =
        new TaskGroup(System.Threading.CancellationTokenSource.CreateLinkedTokenSource(parentToken))

    /// Token that is cancelled when any task fails or the group is disposed.
    member _.Token = cts.Token

    /// <summary>
    /// Add a task to the group. On failure, all other tasks are cancelled.
    /// </summary>
    /// <param name="f">Async function receiving the group's CancellationToken.</param>
    member _.Run(f: System.Threading.CancellationToken -> Task) =
        let t = task {
            try
                do! f cts.Token
            with
            | :? OperationCanceledException -> ()
            | ex ->
                errors.Add(ex)
                try cts.Cancel() with :? ObjectDisposedException -> ()
        }
        tasks.Add(t)

    /// <summary>
    /// Wait for all tasks to complete. Throws <see cref="AggregateException"/> if any failed.
    /// </summary>
    member _.WaitAll() : Task = task {
        try
            do! Task.WhenAll(tasks)
        with _ -> ()
        if not errors.IsEmpty then
            raise (AggregateException(errors.ToArray()))
    }

    interface IDisposable with
        member _.Dispose() =
            try cts.Cancel() with :? ObjectDisposedException -> ()
            cts.Dispose()
#endif
