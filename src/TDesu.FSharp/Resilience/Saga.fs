namespace TDesu.FSharp.Resilience

open System
open System.Threading.Tasks

/// <summary>
/// Saga orchestrator — executes steps sequentially with automatic compensation on failure.
/// </summary>
/// <remarks>
/// On failure, compensates all completed steps in reverse order (LIFO).
/// If compensations also fail, returns <see cref="AggregateException"/> containing all errors.
/// </remarks>
[<RequireQualifiedAccess>]
module Saga =
    /// A saga step: an action that can be compensated (rolled back).
    [<NoEquality; NoComparison>]
    type Step<'ctx> = {
        Name: string
        Execute: 'ctx -> Task<'ctx>
        Compensate: 'ctx -> Task<unit>
    }

    /// Creates a saga step.
    /// <param name="name">Descriptive name for the step (used in diagnostics).</param>
    /// <param name="execute">Async action that advances the saga context.</param>
    /// <param name="compensate">Async rollback action invoked on failure.</param>
    let step name execute compensate = {
        Name = name
        Execute = execute
        Compensate = compensate
    }

    /// Creates a saga step with no compensation (fire-and-forget).
    /// <param name="name">Descriptive name for the step (used in diagnostics).</param>
    /// <param name="execute">Async action that advances the saga context.</param>
    let stepNoCompensate name execute = {
        Name = name
        Execute = execute
        Compensate = fun _ -> task { return () }
    }

    /// Runs saga steps sequentially. On failure, compensates all completed steps in reverse.
    /// Each compensation receives the context that was the output of that step.
    /// If compensations also fail, returns AggregateException containing the original + compensation errors.
    /// <param name="steps">Ordered list of saga steps to execute sequentially.</param>
    /// <param name="ctx">Initial context passed to the first step.</param>
    let run (steps: Step<'ctx> list) (ctx: 'ctx) : Task<Result<'ctx, exn>> =
        task {
            let mutable completed: (Step<'ctx> * 'ctx) list = []
            let mutable current = ctx

            try
                for s in steps do
                    let! next = s.Execute current
                    completed <- (s, next) :: completed
                    current <- next

                return Ok current
            with ex ->
                let mutable compensationErrors = []

                for s, ctxAfterStep in completed do
                    try
                        do! s.Compensate ctxAfterStep
                    with cex ->
                        compensationErrors <- cex :: compensationErrors

                let error: exn =
                    match compensationErrors with
                    | [] -> ex
                    | _ -> AggregateException(ex :: List.rev compensationErrors)

                return Error error
        }
