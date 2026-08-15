// TDesu.FSharp.Collections: pipeable, allocation-conscious wrappers around the
// mutable BCL collections. TDesu.FSharp.Tasks: Task combinators plus TaskGroup for
// structured concurrency (run several tasks, cancel the rest on the first failure).
#load "_prelude.fsx"
open Prelude
open System.Collections.Generic
open TDesu.FSharp.Collections

// ── Dictionary: Some/None instead of the TryGetValue out-param dance ─────
let ages = Dictionary<string, int>()
ages["alice"] <- 30
assertEqual "Dictionary.tryGetValue on a present key" (Some 30) (ages |> Dictionary.tryGetValue "alice")
assertEqual "Dictionary.tryGetValue on a missing key" None (ages |> Dictionary.tryGetValue "bob")
assertEqual "Dictionary.getOrDefault falls back when the key is missing" 0 (ages |> Dictionary.getOrDefault "bob" 0)

// ── Seq: safe aggregation over sequences that might be empty ─────────────
assertEqual "Seq.tryMax on a non-empty seq" (Some 5) (Seq.tryMax [ 3; 1; 5 ])
assertEqual "Seq.tryMax on an empty seq is None, not an exception" None (Seq.tryMax Seq.empty<int>)
assertEqual "Seq.tryAverage" (Some 3.0) (Seq.tryAverage [ 2.0; 4.0 ])

// ── ResizeArray: pipeable wrappers around System.Collections.Generic.List<T> ─────
let topScores =
    ResizeArray.ofList [ 95; 42; 88; 73 ]
    |> ResizeArray.filter (fun x -> x > 50)
    |> ResizeArray.sort
    |> ResizeArray.toArray

assertEqual "ResizeArray: ofList |> filter |> sort |> toArray" [| 73; 88; 95 |] topScores

// ── Stack: note Stack.push returns unit (it mirrors Stack<T>.Push), so unlike
// ResizeArray.add it is a statement, not a pipeline step.
let stack = Stack<int>()
Stack.push 1 stack
Stack.push 2 stack
Stack.push 3 stack
assertEqual "Stack.pop takes the most recently pushed item" 3 (Stack.pop stack)
assertEqual "List.ofStack drains top-first (LIFO)" [ 2; 1 ] (List.ofStack stack)

// ── Tasks: a plain combinator, then TaskGroup for structured concurrency ─────────
open TDesu.FSharp.Tasks

let doubled = task { return 21 } |> Task.map ((*) 2)
assertEqual "Task.map runs after the task completes" 42 (doubled |> Task.getResult)

let runBothConcurrently () =
    task {
        use group = new TaskGroup()
        let mutable a = 0
        let mutable b = 0
        group.Run(fun _ -> task { a <- 1 })
        group.Run(fun _ -> task { b <- 2 })
        do! group.WaitAll()
        return a + b
    }

assertEqual "TaskGroup.WaitAll only returns once every Run'd task has completed" 3 (runBothConcurrently () |> Task.getResult)

printfn "04-collections-and-tasks.fsx: all assertions passed"
