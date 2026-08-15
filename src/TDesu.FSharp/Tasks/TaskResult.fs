namespace TDesu.FSharp.Tasks

open System.Threading.Tasks

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
