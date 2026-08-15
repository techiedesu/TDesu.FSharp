namespace TDesu.FSharp.Tasks

open System.Threading.Tasks

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
