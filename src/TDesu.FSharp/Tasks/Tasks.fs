namespace TDesu.FSharp.Tasks

open System
open System.Threading.Tasks
open TDesu.FSharp.Operators

/// <summary>
/// Task combinators — map, bind, zip, catch, and more for <see cref="Task{T}"/>.
/// </summary>
/// <remarks>
/// All functions are <c>inline</c> for zero-overhead abstractions. Thread-safe by design.
/// </remarks>
/// <namespacedoc>
///   <summary>Task and TaskResult combinators, TaskGroup (structured concurrency), parallelThrottle, fireAndForget.</summary>
/// </namespacedoc>
module Task =
    /// Converts a non-generic Task to Task&lt;unit&gt;.
    /// <param name="t">The non-generic task to convert.</param>
    let inline asUnit (t: Task) : Task<unit> =
        task { do! t }

    /// <summary>
    /// Fire-and-forget: starts a task without awaiting. Logs exceptions via <paramref name="onError"/>.
    /// </summary>
    /// <remarks><paramref name="onError"/> should not throw; if it does, the exception is silently swallowed.</remarks>
    /// <param name="onError">Callback invoked with the exception if the task faults.</param>
    /// <param name="f">Factory that produces the task to run.</param>
    let inline fireAndForget (onError: exn -> unit) (f: unit -> Task) =
        task {
            try do! f ()
            with ex -> try onError ex with _ -> ()
        } |> ignore

    /// <summary>
    /// Runs tasks in parallel with a throttle (max concurrent). Returns all results.
    /// </summary>
    /// <remarks>
    /// On Fable, runs without throttling (JS is single-threaded). A null <paramref name="items"/>
    /// sequence is treated as empty (returns an empty array).
    /// The semaphore is guaranteed to outlive every task that can still call <c>Release()</c> on it:
    /// if enumerating <paramref name="items"/> or awaiting the throttle throws (e.g. a cancelled wait
    /// or a throwing enumerator), every already-spawned task is drained before the semaphore is
    /// disposed, so none of them can release an already-disposed <see cref="System.Threading.SemaphoreSlim"/>.
    /// </remarks>
    /// <param name="maxConcurrent">Maximum number of tasks allowed to run concurrently.</param>
    /// <param name="items">The input sequence of items to process. Null is treated as empty.</param>
    /// <param name="f">Async function applied to each item.</param>
    let parallelThrottle (maxConcurrent: int) (items: 'T seq) (f: 'T -> Task<'TResult>) : Task<'TResult[]> =
        let items = if isNotNullRef items then items else Seq.empty
        task {
#if FABLE_COMPILER
            let tasks = items |> Seq.map f
            return! Task.WhenAll(tasks)
#else
            let semaphore = new System.Threading.SemaphoreSlim(maxConcurrent)
            try
                let tasks = ResizeArray<Task<'TResult>>()
                try
                    for item in items do
                        do! semaphore.WaitAsync()
                        tasks.Add(task {
                            try return! f item
                            finally semaphore.Release() |> ignore
                        })
                    return! Task.WhenAll(tasks)
                with ex ->
                    // Drain every already-spawned task so its `finally semaphore.Release()` still
                    // targets a live semaphore instead of the one about to be disposed below.
                    if tasks.Count > 0 then
                        try
                            do! Task.WhenAll(tasks) :> Task
                        with _ -> ()
                    return raise ex
            finally
                semaphore.Dispose()
#endif
        }

    /// <summary>
    /// Runs tasks in parallel with a throttle, ignoring results.
    /// </summary>
    /// <remarks>
    /// Same null-input and semaphore-lifetime guarantees as <see cref="parallelThrottle"/>: a null
    /// <paramref name="items"/> sequence is treated as empty, and the semaphore is disposed only after
    /// every spawned task has been drained, even if the loop itself throws.
    /// </remarks>
    /// <param name="maxConcurrent">Maximum number of tasks allowed to run concurrently.</param>
    /// <param name="items">The input sequence of items to process. Null is treated as empty.</param>
    /// <param name="f">Async function applied to each item.</param>
    let parallelThrottleUnit (maxConcurrent: int) (items: 'T seq) (f: 'T -> Task<unit>) : Task<unit> =
        let items = if isNotNullRef items then items else Seq.empty
        task {
#if FABLE_COMPILER
            let tasks = items |> Seq.map f
            let! _ = Task.WhenAll(tasks)
#else
            let semaphore = new System.Threading.SemaphoreSlim(maxConcurrent)
            try
                let tasks = ResizeArray<Task<unit>>()
                try
                    for item in items do
                        do! semaphore.WaitAsync()
                        tasks.Add(task {
                            try do! f item
                            finally semaphore.Release() |> ignore
                        })
                    let! _ = Task.WhenAll(tasks)
                    ()
                with ex ->
                    if tasks.Count > 0 then
                        try
                            do! Task.WhenAll(tasks) :> Task
                        with _ -> ()
                    return raise ex
            finally
                semaphore.Dispose()
#endif
            return ()
        }

    /// Blocks until the task completes (ConfigureAwait false). No-op if null or already completed.
    /// <param name="t">The task to wait on.</param>
    let runSynchronously (t: #Task) =
        if isNotNullRef t && not t.IsCompleted then
            t.ConfigureAwait(false).GetAwaiter().GetResult()

    /// Synchronously gets the result of a Task (ConfigureAwait false).
    /// <param name="t">The task whose result to retrieve.</param>
    let inline getResult (t: Task<_>) =
        t.ConfigureAwait(false).GetAwaiter().GetResult()

    /// Maps the result of a task with the given function.
    /// <param name="f">The mapping function.</param>
    /// <param name="t">The input task.</param>
    let inline map ([<InlineIfLambda>] f: 'T -> 'TResult) (t: Task<'T>) : Task<'TResult> =
        task {
            let! v = t
            return f v
        }

    /// Chains a task-returning function on the result of a task.
    /// <param name="f">The binding function returning a new task.</param>
    /// <param name="t">The input task.</param>
    let inline bind ([<InlineIfLambda>] f: 'T -> Task<'TResult>) (t: Task<'T>) : Task<'TResult> =
        task {
            let! v = t
            return! f v
        }

    /// Wraps a value in a completed task.
    /// <param name="v">The value to wrap.</param>
    let inline singleton (v: 'T) : Task<'T> = Task.FromResult(v)

    /// Discards the task result, returning Task&lt;unit&gt;.
    /// <param name="t">The task whose result to discard.</param>
    let inline ignore (t: Task<_>) : Task<unit> =
        task {
            let! _ = t
            return ()
        }

    /// Combines two tasks into a tuple, running them concurrently.
    /// <param name="t1">The first task.</param>
    /// <param name="t2">The second task.</param>
    let inline zip (t1: Task<'T1>) (t2: Task<'T2>) : Task<'T1 * 'T2> =
        task {
            let! a = t1
            and! b = t2
            return (a, b)
        }

    /// Combines three tasks into a triple, running them concurrently.
    /// <param name="t1">The first task.</param>
    /// <param name="t2">The second task.</param>
    /// <param name="t3">The third task.</param>
    let inline zip3 (t1: Task<'T1>) (t2: Task<'T2>) (t3: Task<'T3>) : Task<'T1 * 'T2 * 'T3> =
        task {
            let! a = t1
            and! b = t2
            and! c = t3
            return (a, b, c)
        }

    /// Maps a function over two task results, running them concurrently.
    /// <param name="f">The mapping function taking two values.</param>
    /// <param name="t1">The first task.</param>
    /// <param name="t2">The second task.</param>
    let inline map2 ([<InlineIfLambda>] f: 'T1 -> 'T2 -> 'TResult) (t1: Task<'T1>) (t2: Task<'T2>) : Task<'TResult> =
        task {
            let! a = t1
            and! b = t2
            return f a b
        }

    /// <summary>
    /// Runs a task inside a try/catch, returning <c>Ok</c> on success or <c>Error(exn)</c> on failure.
    /// </summary>
    /// <param name="t">The task to execute inside a try/catch.</param>
    let inline catch (t: Task<'T>) : Task<Result<'T, exn>> =
        task {
            try
                let! v = t
                return Ok v
            with e ->
                return Error e
        }

    /// <summary>
    /// Polls a condition with short delays until it returns true or timeout is reached.
    /// Returns true if condition was met, false on timeout.
    /// </summary>
    /// <param name="timeout">Maximum duration to wait.</param>
    /// <param name="condition">Predicate polled until it returns true.</param>
    let waitUntil (timeout: TimeSpan) (condition: unit -> bool) : Task<bool> =
        task {
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let mutable met = condition ()
            while not met && sw.Elapsed < timeout do
                do! Task.Delay 10
                met <- condition ()
            return met
        }

[<RequireQualifiedAccess>]
module TaskVOption =
    /// Binds a Task&lt;ValueOption&gt; — applies binding on ValueSome, returns ValueNone otherwise.
    /// <param name="binding">The binding function returning a task of ValueOption.</param>
    /// <param name="v">The input task containing a ValueOption.</param>
    let inline taskBind ([<InlineIfLambda>] binding) (v: 'T voption Task) =
        task {
            match! v with
            | ValueNone -> return ValueNone
            | ValueSome v -> return! binding v
        }

    /// Maps the inner ValueOption value inside a Task.
    /// <param name="f">The mapping function.</param>
    /// <param name="v">The input task containing a ValueOption.</param>
    let inline map ([<InlineIfLambda>] f: 'T -> 'TResult) (v: 'T voption Task) : 'TResult voption Task =
        task {
            match! v with
            | ValueNone -> return ValueNone
            | ValueSome v -> return ValueSome(f v)
        }

    /// Returns the ValueSome value or the given default from a Task&lt;ValueOption&gt;.
    /// <param name="def">The default value when ValueNone.</param>
    /// <param name="v">The input task containing a ValueOption.</param>
    let inline defaultValue (def: 'T) (v: 'T voption Task) : 'T Task =
        task {
            match! v with
            | ValueNone -> return def
            | ValueSome v -> return v
        }

    /// Wraps a Task result in ValueSome.
    /// <param name="t">The input task whose result to wrap.</param>
    let inline ofTask (t: 'T Task) : 'T voption Task =
        task {
            let! v = t
            return ValueSome v
        }

module TaskOption =
    /// Converts a Task&lt;Result&gt; to Task&lt;Option&gt;: Ok becomes Some, Error becomes None.
    /// <param name="v">The input task containing a Result.</param>
    let inline ofResult (v: Result<_, _> Task) =
        task {
            match! v with
            | Error _ -> return None
            | Ok v -> return Some v
        }

    /// Maps the inner Option value inside a Task.
    /// <param name="f">The mapping function.</param>
    /// <param name="v">The input task containing an Option.</param>
    let inline map ([<InlineIfLambda>] f: 'T -> 'TResult) v : 'TResult option Task =
        task {
            let! v = v
            return v |> Option.map f
        }

/// <summary>
/// Combinators for <c>Task&lt;Result&lt;'T, 'TError&gt;&gt;</c> — map, bind, tee on the inner Result.
/// </summary>
[<RequireQualifiedAccess>]
module TaskResult =
    /// Maps the Ok value inside a Task&lt;Result&gt;.
    /// <param name="f">The mapping function applied to the Ok value.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline map ([<InlineIfLambda>] f: 'T -> 'TResult) (tr: Task<Result<'T, 'TError>>) : Task<Result<'TResult, 'TError>> =
        task {
            match! tr with
            | Ok v ->
                return Ok(f v)
            | Error e ->
                return Error e
        }

    /// Maps the Error value inside a Task&lt;Result&gt;.
    /// <param name="f">The mapping function applied to the Error value.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline mapError ([<InlineIfLambda>] f: 'TError -> 'TError2) (tr: Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError2>> =
        task {
            match! tr with
            | Ok v -> return Ok v
            | Error e -> return Error(f e)
        }

    /// <summary>
    /// Chains a task-returning function on <c>Ok</c> inside a <c>Task&lt;Result&gt;</c>.
    /// Short-circuits on <c>Error</c>.
    /// </summary>
    /// <param name="f">The binding function applied to the Ok value.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline bind ([<InlineIfLambda>] f: 'T -> Task<Result<'TResult, 'TError>>) (tr: Task<Result<'T, 'TError>>) : Task<Result<'TResult, 'TError>> =
        task {
            match! tr with
            | Ok v -> return! f v
            | Error e -> return Error e
        }

    /// Returns the Ok value or the given default from a Task&lt;Result&gt;.
    /// <param name="def">The default value when Error.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline defaultValue (def: 'T) (tr: Task<Result<'T, _>>) : Task<'T> =
        task {
            match! tr with
            | Ok v -> return v
            | Error _ -> return def
        }

    /// Returns the Ok value or computes a fallback from the Error inside a Task&lt;Result&gt;.
    /// <param name="f">Function that computes a fallback from the Error value.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline valueOr ([<InlineIfLambda>] f: 'TError -> 'T) (tr: Task<Result<'T, 'TError>>) : Task<'T> =
        task {
            match! tr with
            | Ok v -> return v
            | Error e -> return f e
        }

    /// Applies a side-effect on Ok inside a Task&lt;Result&gt; and returns unchanged.
    /// <param name="f">Side-effect function applied to the Ok value.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline tee ([<InlineIfLambda>] f: 'T -> unit) (tr: Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError>> =
        task {
            let! r = tr
            match r with
            | Ok v -> f v
            | Error _ -> ()
            return r
        }

    /// Applies a side-effect on Error inside a Task&lt;Result&gt; and returns unchanged.
    /// <param name="f">Side-effect function applied to the Error value.</param>
    /// <param name="tr">The input task containing a Result.</param>
    let inline teeError ([<InlineIfLambda>] f: 'TError -> unit) (tr: Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError>> =
        task {
            let! r = tr
            match r with
            | Ok _ -> ()
            | Error e -> f e
            return r
        }

    /// Converts a Task&lt;Result&gt; to Task&lt;Option&gt;: Ok becomes Some, Error becomes None.
    /// <param name="tr">The input task containing a Result.</param>
    let inline toOption (tr: Task<Result<'T, _>>) : Task<'T option> =
        task {
            match! tr with
            | Ok v -> return Some v
            | Error _ -> return None
        }

    /// Discards the Ok value inside a Task&lt;Result&gt;.
    /// <param name="tr">The input task containing a Result.</param>
    let inline ignore (tr: Task<Result<_, 'TError>>) : Task<Result<unit, 'TError>> =
        task {
            match! tr with
            | Ok _ -> return Ok()
            | Error e -> return Error e
        }

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
