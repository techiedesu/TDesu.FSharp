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
assertEqual "Seq.tryMaxBy picks the element with the greatest projected key" (Some "banana") (Seq.tryMaxBy String.length [ "a"; "banana"; "kiwi" ])
assertEqual "Seq.tryMinBy picks the element with the smallest projected key" (Some "a") (Seq.tryMinBy String.length [ "a"; "banana"; "kiwi" ])
assertEqual "Seq.tryMaxBy on an empty seq is None, not an exception" None (Seq.tryMaxBy id Seq.empty<int>)

// ── ResizeArray: pipeable wrappers around System.Collections.Generic.List<T> ─────
let topScores =
    ResizeArray.ofList [ 95; 42; 88; 73 ]
    |> ResizeArray.filter (fun x -> x > 50)
    |> ResizeArray.sort
    |> ResizeArray.toArray

assertEqual "ResizeArray: ofList |> filter |> sort |> toArray" [| 73; 88; 95 |] topScores

assertEqual "ResizeArray.mapi pairs each element with its index"
    [ 0, "a"; 1, "b"; 2, "c" ]
    (ResizeArray.ofList [ "a"; "b"; "c" ] |> ResizeArray.mapi (fun i x -> i, x) |> ResizeArray.toList)

assertEqual "ResizeArray.choose keeps only the Some results, dropping None"
    [ 4; 16 ]
    (ResizeArray.ofList [ 1; 2; 3; 4 ]
     |> ResizeArray.choose (fun x -> if x % 2 = 0 then Some(x * x) else None)
     |> ResizeArray.toList)

assertEqual "ResizeArray.rev reverses without mutating the input" [ 3; 2; 1 ] (ResizeArray.ofList [ 1; 2; 3 ] |> ResizeArray.rev |> ResizeArray.toList)

let evens, odds = ResizeArray.ofList [ 1; 2; 3; 4; 5 ] |> ResizeArray.partition (fun x -> x % 2 = 0)
assertEqual "ResizeArray.partition splits into matching elements..." [ 2; 4 ] (ResizeArray.toList evens)
assertEqual "...then the rest, both in original order" [ 1; 3; 5 ] (ResizeArray.toList odds)

assertEqual "ResizeArray.tryFindIndex finds the first matching index" (Some 2) (ResizeArray.ofList [ 5; 9; 12; 20 ] |> ResizeArray.tryFindIndex (fun x -> x > 10))
assertEqual "ResizeArray.tryFindIndex is None when nothing matches" None (ResizeArray.ofList [ 1; 2 ] |> ResizeArray.tryFindIndex (fun x -> x > 10))

assertTrue "ResizeArray.forall is true when every element matches" (ResizeArray.ofList [ 2; 4; 6 ] |> ResizeArray.forall (fun x -> x % 2 = 0))
assertTrue "ResizeArray.forall is vacuously true for an empty ResizeArray" (ResizeArray.create<int> () |> ResizeArray.forall (fun _ -> false))

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
