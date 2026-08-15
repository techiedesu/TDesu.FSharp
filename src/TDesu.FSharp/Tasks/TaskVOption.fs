namespace TDesu.FSharp.Tasks

open System.Threading.Tasks

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
