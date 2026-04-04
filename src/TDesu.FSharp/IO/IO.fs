namespace TDesu.FSharp.IO

open System
#if !FABLE_COMPILER
open System.IO
#endif
open TDesu.FSharp.Operators

#if !FABLE_COMPILER
[<RequireQualifiedAccess>]
module File =
    /// <summary>
    /// Deletes a file, returning <c>Ok(())</c> on success or <c>Error(exn)</c> on failure.
    /// </summary>
    /// <param name="filePath">The path of the file to delete.</param>
    let delete filePath =
        try
            File.Delete(filePath)
            Ok ()
        with e ->
            Error e

    /// Deletes a file, ignoring the result.
    let deleteIgnore = delete >> ignore

    /// Returns true if the file does not exist at the given path.
    /// <param name="path">The file path to check.</param>
    let inline notExists path =
        File.Exists(path) |> not

module Directory =
    /// Returns true if the directory does not exist at the given path.
    /// <param name="path">The directory path to check.</param>
    let notExists path =
        Directory.Exists(path) |> not

    /// Creates a directory (and parents) at the given path.
    /// <param name="path">The directory path to create.</param>
    let create path =
        %Directory.CreateDirectory(path)

/// <summary>
/// Environment variable helpers.
/// </summary>
/// <example>
/// <code>
/// let connStr = Env.requireVar "DATABASE_URL"
/// let port = Env.getVarOr "8080" "PORT"
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Env =
    /// Gets an environment variable as Some, or None if missing/empty.
    /// <param name="name">The environment variable name.</param>
    let getVar (name: string) : string option =
        match Environment.GetEnvironmentVariable(name) with
        | null | "" -> None
        | v -> Some v

    /// Gets an environment variable, throwing if missing/empty.
    /// <param name="name">The environment variable name.</param>
    /// <exception cref="System.InvalidOperationException">When the variable is not set or empty.</exception>
    let requireVar (name: string) : string =
        match getVar name with
        | Some v -> v
        | None -> invalidOp $"Environment variable '{name}' is not set"

    /// Gets an environment variable, or a default if missing/empty.
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="name">The environment variable name.</param>
    let getVarOr (defaultValue: string) (name: string) : string =
        match getVar name with
        | Some v -> v
        | None -> defaultValue
#endif

[<RequireQualifiedAccess>]
module Path =
    /// Generates a random file name from a GUID (no dashes or extension).
    let getRandomFileName () =
        let g = Guid.NewGuid()
        g.ToString("N")

/// Static helper for path utilities.
[<Sealed; AbstractClass>]
type Path private () =
    /// Gets a new random file name (GUID-based, no dashes).
    static member RandomFileName with get () = Path.getRandomFileName ()


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
    /// <example>
    /// <code>
    /// use all = Disposable.combine [ stream; connection; timer ]
    /// </code>
    /// </example>
    /// <param name="disposables">The list of disposables to combine.</param>
    let combine (disposables: IDisposable list) =
        { new IDisposable with
            member _.Dispose() =
                disposables
                |> List.rev
                |> List.iter (fun d ->
                    if not (obj.ReferenceEquals(d, null)) then
                        d.Dispose()) }

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

#if !FABLE_COMPILER
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
#endif
