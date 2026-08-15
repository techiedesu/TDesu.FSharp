(*** hide ***)
#r "../src/TDesu.FSharp/bin/Release/netstandard2.1/TDesu.FSharp.dll"
open System
open TDesu.FSharp
open TDesu.FSharp.Buffers
open TDesu.FSharp.Hashing
open TDesu.FSharp.Resilience
(**
# TDesu.FSharp Tutorial

## Result: Railway-Oriented Programming

Chain operations that might fail. Short-circuits on first Error.
*)

let validateAge age =
    if age < 0 then Error "age must be positive"
    elif age > 150 then Error "age too large"
    else Ok age

let validateName name =
    Result.requireNotNull "name is required" name
    |> Result.bind (fun n ->
        if String.IsNullOrWhiteSpace(n) then Error "name is empty"
        else Ok n)

// Pipeline: validate, transform, log
let processUser name age =
    validateName name
    |> Result.bind (fun n -> validateAge age |> Result.map (fun a -> n, a))
    |> Result.tee (fun (n, a) -> printfn "Valid: %s, %d" n a)
    |> Result.teeError (fun e -> printfn "Invalid: %s" e)

let valid = processUser "Alice" 30
(*** include-value: valid ***)

let invalid = processUser "" -5
(*** include-value: invalid ***)

(**
## Option: Maybe Pipeline

Short-circuit on None. Great for lookups and parsing.
*)

open TDesu.FSharp.Builders

let tryFindUser (id: int) =
    if id > 0 then Some {| Name = "Alice"; Email = "alice@example.com" |}
    else None

let tryGetDomain email =
    match email |> String.split "@" with
    | [| _; domain |] -> Some domain
    | _ -> None

let userDomain = option {
    let! user = tryFindUser 1
    let! domain = tryGetDomain user.Email
    return domain
}
(*** include-value: userDomain ***)

(**
## Parse Active Patterns

Match and extract typed values from strings in one step.
*)

open TDesu.FSharp.ActivePatterns

let describe input =
    match input with
    | Parse.Int n -> sprintf "integer: %d" n
    | Parse.Double d -> sprintf "float: %f" d
    | Parse.Bool b -> sprintf "bool: %b" b
    | Parse.Guid g -> sprintf "guid: %A" g
    | other -> sprintf "text: %s" other

let examples = [ "42"; "3.14"; "true"; "hello" ] |> List.map describe
(*** include-value: examples ***)

(**
## String Utilities

Pipeline-friendly string operations.
*)

let processed =
    "  Hello, World!  "
    |> String.trim
    |> String.replace "World" "F#"
    |> String.toLowerInv
(*** include-value: processed ***)

let parts = "one,two,three" |> String.split "," |> String.join " | "
(*** include-value: parts ***)

(**
## ResizeArray: Functional Mutable Lists

Pipeable wrappers for `System.Collections.Generic.List<T>`.
*)

let topScores =
    ResizeArray.create ()
    |> ResizeArray.add 95
    |> ResizeArray.add 42
    |> ResizeArray.add 88
    |> ResizeArray.add 73
    |> ResizeArray.filter (fun x -> x > 50)
    |> ResizeArray.sort
    |> ResizeArray.toArray
(*** include-value: topScores ***)

(**
## Guard: Validate-and-Throw

One-liner argument validation. Throws standard .NET exceptions.
*)

let divide a b =
    Guard.positive "b" b
    a / b

try divide 10 0 |> ignore
with :? ArgumentOutOfRangeException as ex -> printfn "Caught: %s" ex.ParamName

(**
## Disposable Helpers

RAII patterns for F#.
*)

// Go-style defer stack
let processFiles () =
    use cleanup = Disposable.deferStack ()
    let path, pathDispose = Disposable.tempFile ()
    cleanup.AddDisposable(pathDispose)
    printfn "Temp file: %s" path
    // cleanup runs on exit -- deletes temp file

// Combine multiple disposables
let combined = Disposable.combine [
    Disposable.create (fun () -> printfn "cleanup 1")
    Disposable.create (fun () -> printfn "cleanup 2")
]

(**
## Bytes & Hashing

Low-level buffer operations and content hashing.
*)

let hash = ContentHash.sha256Hex "hello world"
(*** include-value: hash ***)

let data1 = [| 1uy; 2uy |]
let data2 = [| 3uy; 4uy |]
let joined = Bytes.concat2 data1 data2
(*** include-value: joined ***)

let dictKey = Hash.ofArray [| 1; 2; 3 |]
(*** include-value: dictKey ***)

(**
## Bounded Collections

Auto-evict oldest when at capacity. Perfect for caches and dedup buffers.
*)

let recentIds = BoundedDict<string, int>(3)
recentIds.Set("a", 1)
recentIds.Set("b", 2)
recentIds.Set("c", 3)
recentIds.Set("d", 4)  // "a" evicted
let hasA = recentIds.ContainsKey "a"
let hasD = recentIds.TryGet "d"
(*** include-value: hasA ***)
(*** include-value: hasD ***)
