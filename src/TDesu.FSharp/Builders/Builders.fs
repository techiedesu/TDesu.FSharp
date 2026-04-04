module TDesu.FSharp.Builders

open System
open System.Threading.Tasks

// ── ResultBuilder ──

/// Computation expression builder for Result&lt;'T,'TError&gt; workflows (short-circuits on Error).
[<Sealed>]
type ResultBuilder() =
    member inline _.Return(value: 'T) : Result<'T, 'TError> = Ok value

    member inline _.ReturnFrom(result: Result<'T, 'TError>) : Result<'T, 'TError> = result

    member inline _.Bind(result: Result<'T, 'TError>, [<InlineIfLambda>] f: 'T -> Result<'TResult, 'TError>) : Result<'TResult, 'TError> =
        match result with
        | Ok value -> f value
        | Error err -> Error err

    member inline _.Zero() : Result<unit, 'TError> = Ok()

    member inline _.Combine(result: Result<unit, 'TError>, continuation: unit -> Result<'T, 'TError>) : Result<'T, 'TError> =
        match result with
        | Ok() -> continuation ()
        | Error err -> Error err

    member inline _.Delay([<InlineIfLambda>] f: unit -> Result<'T, 'TError>) = f

    member inline _.Run([<InlineIfLambda>] f: unit -> Result<'T, 'TError>) = f ()

    member inline _.TryWith(body: unit -> Result<'T, 'TError>, handler: exn -> Result<'T, 'TError>) : Result<'T, 'TError> =
        try
            body ()
        with ex ->
            handler ex

    member inline _.TryFinally(body: unit -> Result<'T, 'TError>, compensation: unit -> unit) : Result<'T, 'TError> =
        try
            body ()
        finally
            compensation ()

    member inline _.Using(resource: 'TResource when 'TResource :> IDisposable, body: 'TResource -> Result<'T, 'TError>) : Result<'T, 'TError> =
        try
            body resource
        finally
            if not (obj.ReferenceEquals(resource, null)) then
                (resource :> IDisposable).Dispose()

/// Computation expression instance for Result workflows: `result { ... }`.
let result = ResultBuilder()

// ── OptionBuilder ──

/// Computation expression builder for Option-based workflows (short-circuits on None).
[<Sealed>]
type OptionBuilder() =
    member inline _.Bind(optionValue, f) =
        match optionValue with
        | None -> None
        | Some value -> f value

#if !FABLE_COMPILER
    member inline _.Bind(voptionValue, f) =
        match voptionValue with
        | ValueNone -> None
        | ValueSome value -> f value
#endif

    member inline _.Return(maybeNull) =
        if Object.ReferenceEquals(maybeNull, null) then
            None
        else
            Some maybeNull

    member inline _.ReturnFrom(optionValue: 'T option) = optionValue

    member inline _.Combine(optionValue, f) =
        match optionValue with
        | None -> f ()
        | Some _ -> optionValue

#if !FABLE_COMPILER
    member inline _.Combine(optionValue: 'T voption, f) =
        match optionValue with
        | ValueNone -> f ()
        | ValueSome v -> Some v
#endif

    member inline _.Delay f = f

    member inline _.Run f = f ()

    member inline _.Zero() = None

    member inline _.TryWith(expr, handler) =
        try
            expr ()
        with ex ->
            handler ex

    member inline _.TryFinally(body, compensation) =
        try
            body ()
        finally
            compensation ()

    member inline _.Using(resource: 'TResource when 'TResource :> IDisposable, body: 'TResource -> _ option) =
        try
            body resource
        finally
            if not (obj.ReferenceEquals(resource, null)) then
                resource.Dispose()

/// Computation expression instance for Option workflows: `option { ... }`.
let option = OptionBuilder()

module OptionBuilderAnyReferenceTypeEx =
    type OptionBuilder with
        member inline _.Bind(maybeNull, f) =
            if Object.ReferenceEquals(maybeNull, null) then
                None
            else
                f maybeNull

// ── TaskResultBuilder ──

/// Computation expression builder for Task&lt;Result&lt;'T,'TError&gt;&gt; workflows (railway-oriented).
[<Sealed>]
type TaskResultBuilder() =
    member inline _.Return(value: 'T) : Task<Result<'T, 'TError>> =
        Task.FromResult(Ok value)

    member inline _.ReturnFrom(taskResult: Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError>> =
        taskResult

    member inline _.Bind(taskResult: Task<Result<'T, 'TError>>, [<InlineIfLambda>] f: 'T -> Task<Result<'TResult, 'TError>>) : Task<Result<'TResult, 'TError>> =
        task {
            match! taskResult with
            | Ok value -> return! f value
            | Error err -> return Error err
        }

    member inline _.Bind(result: Result<'T, 'TError>, [<InlineIfLambda>] f: 'T -> Task<Result<'TResult, 'TError>>) : Task<Result<'TResult, 'TError>> =
        match result with
        | Ok value -> f value
        | Error err -> Task.FromResult(Error err)

    member inline _.Bind(t: Task<'T>, [<InlineIfLambda>] f: 'T -> Task<Result<'TResult, 'TError>>) : Task<Result<'TResult, 'TError>> =
        task {
            let! value = t
            return! f value
        }

    member inline _.Zero() : Task<Result<unit, 'TError>> =
        Task.FromResult(Ok())

    member inline _.Combine(taskResult: Task<Result<unit, 'TError>>, continuation: unit -> Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError>> =
        task {
            match! taskResult with
            | Ok() -> return! continuation ()
            | Error err -> return Error err
        }

    member inline _.Delay([<InlineIfLambda>] f: unit -> Task<Result<'T, 'TError>>) = f

    member inline _.Run([<InlineIfLambda>] f: unit -> Task<Result<'T, 'TError>>) = f ()

    member inline _.TryWith(body: unit -> Task<Result<'T, 'TError>>, handler: exn -> Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError>> =
        task {
            try
                return! body ()
            with ex ->
                return! handler ex
        }

    member inline _.TryFinally(body: unit -> Task<Result<'T, 'TError>>, compensation: unit -> unit) : Task<Result<'T, 'TError>> =
        task {
            try
                return! body ()
            finally
                compensation ()
        }

    member inline _.Using(resource: 'TResource when 'TResource :> IDisposable, body: 'TResource -> Task<Result<'T, 'TError>>) : Task<Result<'T, 'TError>> =
        task {
            use r = resource
            return! body r
        }

/// Computation expression instance for Task+Result workflows: `taskResult { ... }`.
let taskResult = TaskResultBuilder()
