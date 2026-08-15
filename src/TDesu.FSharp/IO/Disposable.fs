namespace TDesu.FSharp.IO

open System
open TDesu.FSharp.Operators

/// <summary>
/// Disposable utilities — create, combine, defer, null-safe dispose.
/// </summary>
[<RequireQualifiedAccess>]
module Disposable =
    /// <summary>
    /// Creates an <see cref="IDisposable"/> from a cleanup function.
    /// </summary>
    /// <example>
    /// <code>
    /// use d = Disposable.create (fun () -> connection.Close())
    /// </code>
    /// </example>
    /// <param name="f">The cleanup function to invoke on disposal.</param>
    let inline create ([<InlineIfLambda>] f: unit -> unit) =
        { new IDisposable with member _.Dispose() = f () }

    /// An IDisposable that does nothing on Dispose.
    let empty : IDisposable =
        { new IDisposable with member _.Dispose() = () }

    /// <summary>
    /// Combines multiple disposables into one. All are disposed in reverse order.
    /// </summary>
    /// <remarks>
    /// Hardened like <see cref="DeferStack"/>: every disposable is still disposed even if an earlier
    /// one throws. A single failure surfaces as itself; two or more are aggregated into an
    /// <see cref="System.AggregateException"/>. Disposal is idempotent — a second <c>Dispose()</c> call
    /// is a no-op and never re-disposes the children. A null <paramref name="disposables"/> list is
    /// treated as empty; null entries within the list are ignored.
    /// </remarks>
    /// <example>
    /// <code>
    /// use all = Disposable.combine [ stream; connection; timer ]
    /// </code>
    /// </example>
    /// <exception cref="System.AggregateException">When two or more disposables throw on disposal.</exception>
    /// <param name="disposables">The list of disposables to combine. Null is treated as empty; null entries are ignored.</param>
    let combine (disposables: IDisposable list) =
        let disposables = if isNotNullRef disposables then disposables else []
        let mutable disposed = 0
        { new IDisposable with
            member _.Dispose() =
                if Threading.Interlocked.Exchange(&disposed, 1) = 0 then
                    let mutable errors = []
                    for d in disposables |> List.rev do
                        if not (obj.ReferenceEquals(d, null)) then
                            try d.Dispose() with ex -> errors <- ex :: errors
                    match errors with
                    | [] -> ()
                    | [ single ] -> raise single
                    | _ -> aggregate errors }

    /// Safely disposes a value (null-safe, no-op if null).
    /// <param name="d">The disposable to dispose.</param>
    let inline dispose (d: IDisposable) =
        if not (obj.ReferenceEquals(d, null)) then d.Dispose()

    /// Disposes the inner value if Some, no-op if None.
    /// <param name="d">The optional disposable to dispose.</param>
    let inline disposeOption (d: IDisposable option) =
        match d with Some v -> dispose v | None -> ()

    /// Disposes the inner value if ValueSome, no-op if ValueNone.
    /// <param name="d">The value-option disposable to dispose.</param>
    let inline disposeValueOption (d: IDisposable voption) =
        match d with ValueSome v -> dispose v | ValueNone -> ()

    /// <summary>
    /// Creates a disposable that runs the cleanup function at most once (thread-safe).
    /// </summary>
    /// <remarks>Thread-safe: uses <see cref="System.Threading.Interlocked.Exchange"/> internally.</remarks>
    /// <param name="f">The cleanup function to run at most once.</param>
    let createOnce (f: unit -> unit) =
        let mutable disposed = 0
        { new IDisposable with
            member _.Dispose() =
                if Threading.Interlocked.Exchange(&disposed, 1) = 0 then
                    f () }

    /// Wraps a function: creates a resource, applies f, then disposes the resource.
    /// <param name="create">Factory function that produces the disposable resource.</param>
    /// <param name="f">Function to apply to the created resource.</param>
    let inline using ([<InlineIfLambda>] create: unit -> 'T when 'T :> IDisposable) ([<InlineIfLambda>] f: 'T -> 'R) : 'R =
        use resource = create ()
        f resource

    /// <summary>
    /// Deferred cleanup stack (like Go's <c>defer</c>). Add cleanups, all run on Dispose in LIFO order.
    /// Not thread-safe: do not call <c>Add</c> concurrently with <c>Dispose</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// use cleanup = new Disposable.DeferStack()
    /// cleanup.Add (fun () -> file.Close())
    /// cleanup.Add (fun () -> connection.Release())
    /// </code>
    /// </example>
    type DeferStack() =
        let mutable actions: (unit -> unit) list = []

        /// Adds a cleanup action to run on Dispose.
        member _.Add(f: unit -> unit) = actions <- f :: actions

        /// Adds a disposable to dispose on Dispose.
        member _.AddDisposable(d: IDisposable) =
            actions <- (fun () -> if not (obj.ReferenceEquals(d, null)) then d.Dispose()) :: actions

        interface IDisposable with
            member _.Dispose() =
                let mutable errors = []
                for f in actions do
                    try f () with ex -> errors <- ex :: errors
                match errors with
                | [] -> ()
                | [ single ] -> raise single
                | _ -> aggregate errors

    /// Creates a new deferred cleanup stack.
    let deferStack () = new DeferStack()

    /// Creates a temporary directory that is deleted on Dispose.
    let tempDir () =
        let g = Guid.NewGuid()
        let path = IO.Path.Combine(IO.Path.GetTempPath(), g.ToString("N"))
        System.IO.Directory.CreateDirectory(path) |> ignore
        path, create (fun () -> try System.IO.Directory.Delete(path, true) with _ -> ())

    /// Creates a temporary file path that is deleted on Dispose.
    let tempFile () =
        let path = IO.Path.GetTempFileName()
        path, create (fun () -> try System.IO.File.Delete(path) with _ -> ())
