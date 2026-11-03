namespace TDesu.FSharp.Tests

open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp.Tasks

/// An OperationCanceledException subclass, like a typed deadline a caller might catch
/// specifically — used to prove a cancellation surfaces as its original instance, not a
/// generic replacement.
type private TypedCancellation(token: System.Threading.CancellationToken) =
    inherit System.OperationCanceledException(token)

[<TestFixture>]
type TaskTests() =

    [<Test>]
    member _.``catch captures exception``() =
        match task { return 42 } |> Task.catch |> Task.getResult with
        | Ok v -> equals v 42
        | Error _ -> Assert.Fail("Expected Ok")

        match task { return failwith "boom" } |> Task.catch |> Task.getResult with
        | Error e -> equals e.Message "boom"
        | Ok _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``runSynchronously handles null task``() = Task.runSynchronously (null: Task) // should not throw

    [<Test>]
    member _.``runSynchronously observes a fault that already happened before the call``() =
        let t: Task = Task.FromException(System.InvalidOperationException "boom")
        isTrue t.IsCompleted

        let ex =
            Assert.Throws<System.InvalidOperationException>(fun () -> Task.runSynchronously t)

        equals ex.Message "boom"

    [<Test>]
    member _.``runSynchronously observes a cancellation that already happened before the call``() =
        let t: Task = Task.FromCanceled(System.Threading.CancellationToken(true))
        isTrue t.IsCompleted

        Assert.Throws<TaskCanceledException>(fun () -> Task.runSynchronously t)
        |> ignore

    // Task.loop exists so a recursive task { } can be written without `return! step`, which is
    // not a tail call: that shape keeps one state machine per iteration alive and nests real
    // stack frames when the awaited work completes synchronously. A million synchronous
    // iterations is far past where the recursive form overflowed (between 1,000 and 1,500 on a
    // 1 MB stack), so finishing at all is the point of the first test.

    [<Test>]
    member _.``loop runs a million synchronous steps in constant stack``() =
        let total =
            Task.loop
                (fun (i, acc) ->
                    Task.FromResult(
                        if i = 1_000_000 then
                            Loop.Stop acc
                        else
                            Loop.Continue(i + 1, acc + int64 i)
                    )
                )
                (0, 0L)
            |> Task.getResult

        equals total 499_999_500_000L

    [<Test>]
    member _.``loop threads the state and returns the stop result``() =
        let words =
            Task.loop
                (fun (n: int, acc: string list) ->
                    task {
                        do! Task.Yield()

                        return
                            if n = 0 then
                                Loop.Stop(List.rev acc)
                            else
                                Loop.Continue(n - 1, string n :: acc)
                    }
                )
                (3, [])
            |> Task.getResult

        equals words [ "3"; "2"; "1" ]

    [<Test>]
    member _.``loop faults when a step throws and does not run further steps``() =
        let mutable steps = 0

        let t =
            Task.loop
                (fun (n: int) ->
                    task {
                        steps <- steps + 1

                        if n = 2 then
                            failwith "boom"

                        return Loop.Continue(n + 1)
                    }
                )
                0

        let ex = Assert.Throws<System.AggregateException>(fun () -> t.Wait())
        equals ex.InnerException.Message "boom"
        equals steps 3

    // map/bind/ignore short-circuit an input that already completed successfully. That path
    // must report a throwing mapper exactly like the task builder does — faulted for an
    // ordinary exception, cancelled for an OperationCanceledException, and never a
    // synchronous throw at the call site.
    [<Test>]
    member _.``map and bind keep the builder's fault and cancel contract on a completed input``() =
        let completed = Task.FromResult 42
        let token = System.Threading.CancellationToken(true)

        let statusOf (t: Task<int>) =
            try
                t.Wait()
            with :? System.AggregateException ->
                ()

            t.Status

        equals (Task.map (fun v -> v + 1) completed |> Task.getResult) 43
        equals (Task.bind (fun v -> Task.FromResult(v + 1)) completed |> Task.getResult) 43

        equals (statusOf (Task.map (fun _ -> failwith "boom") completed)) TaskStatus.Faulted
        equals (statusOf (Task.bind (fun _ -> failwith "boom") completed)) TaskStatus.Faulted

        equals
            (statusOf (Task.map (fun _ -> raise (System.OperationCanceledException token)) completed))
            TaskStatus.Canceled

        equals
            (statusOf (Task.bind (fun _ -> raise (System.OperationCanceledException token)) completed))
            TaskStatus.Canceled

        // An OperationCanceledException carrying no cancelled token still has to cancel rather
        // than throw out of the combinator — Task.FromCanceled would reject that token.
        equals
            (statusOf (Task.map (fun _ -> raise (System.OperationCanceledException())) completed))
            TaskStatus.Canceled

        equals
            (statusOf (Task.map id (Task.FromException<int>(System.InvalidOperationException "x"))))
            TaskStatus.Faulted

        equals (statusOf (Task.map id (Task.FromCanceled<int> token))) TaskStatus.Canceled

    [<Test>]
    member _.``map and bind on a completed input rethrow the original cancellation instance, not a generic one``() =
        let completed = Task.FromResult 42
        let token = System.Threading.CancellationToken(true)

        let causeOf (t: Task<int>) =
            try
                Task.getResult t |> ignore
                None
            with ex ->
                Some ex

        match causeOf (Task.map (fun _ -> raise (TypedCancellation token)) completed) with
        | Some(:? TypedCancellation) -> ()
        | other -> Assert.Fail($"map: expected TypedCancellation, got {other}")

        match causeOf (Task.bind (fun _ -> raise (TypedCancellation token)) completed) with
        | Some(:? TypedCancellation) -> ()
        | other -> Assert.Fail($"bind: expected TypedCancellation, got {other}")

    [<Test>]
    member _.``map and bind still await an input that completes later``() =
        let source =
            TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)

        let mapped = Task.map (fun v -> v + 1) source.Task
        let bound = Task.bind (fun v -> Task.FromResult(v * 2)) source.Task
        let ignored = Task.ignore source.Task

        equals mapped.IsCompleted false
        source.SetResult 20

        equals (Task.getResult mapped) 21
        equals (Task.getResult bound) 40
        equals (Task.getResult ignored) ()

    [<Test>]
    member _.``ignore propagates a failed input instead of completing``() =
        let faulted = Task.FromException<int>(System.InvalidOperationException "x")
        let t = Task.ignore faulted

        try
            t.Wait()
        with :? System.AggregateException ->
            ()

        equals t.Status TaskStatus.Faulted
