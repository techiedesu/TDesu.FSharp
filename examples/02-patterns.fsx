// TDesu.FSharp.ActivePatterns: parse-and-match a string in one step, string-shape
// patterns for null/blank checks, and (from the `Comparisons` module, which -- unlike
// most modules here -- *is* [<AutoOpen>]) curried comparison patterns usable directly
// in a `match` arm.
#load "_prelude.fsx"
open Prelude
open TDesu.FSharp.ActivePatterns

// ── Parse: match a string, extract the typed value in the same step ─────
let describe (input: string) =
    match input with
    | Parse.Int n -> $"int {n}"
    | Parse.Guid g -> $"guid {g}"
    | Parse.Bool b -> $"bool {b}"
    | String.NullOrWhiteSpace -> "blank"
    | other -> $"text {other}"

assertEqual "Parse.Int wins for an integer string" "int 42" (describe "42")
assertEqual "Parse.Bool wins for a bool string" "bool True" (describe "true")
assertEqual "Parse.Guid wins for a guid string" "guid 00000000-0000-0000-0000-000000000001" (describe "00000000-0000-0000-0000-000000000001")
assertEqual "String.NullOrWhiteSpace catches blank input" "blank" (describe "   ")
assertEqual "anything else falls through to text" "text hello" (describe "hello")

// ── String patterns beyond NullOrWhiteSpace ──────────────────────────────
assertTrue "String.Empty matches \"\"" (match "" with String.Empty -> true | _ -> false)
assertTrue "String.WhiteSpace matches a single space" (match " " with String.WhiteSpace -> true | _ -> false)
assertTrue "String.StartsWithAny checks several prefixes at once"
    (match "https://x" with
     | String.StartsWithAny [| "http://"; "https://" |] -> true
     | _ -> false)

// ── Comparisons: Eq/NEq/Lt/Gt/LtEq/GtEq/Between -- comparand first, value last, so
// `match x with | Lt 10 -> ...` reads as "x is less than 10". Works over any
// comparable type (structural equality/comparison), not just numbers.
let classifyStatus code =
    match code with
    | Between 200 299 -> "success"
    | Between 400 499 -> "client error"
    | Between 500 599 -> "server error"
    | _ -> "other"

assertEqual "Between 200 299 matches 204" "success" (classifyStatus 204)
assertEqual "Between 400 499 matches 404" "client error" (classifyStatus 404)
assertEqual "no range matches 100 -> other" "other" (classifyStatus 100)

let classifyAge age =
    match age with
    | Lt 0 -> "invalid"
    | Lt 18 -> "minor"
    | GtEq 18 -> "adult"
    | _ -> "unreachable"

assertEqual "Lt 18 matches a minor" "minor" (classifyAge 12)
assertEqual "GtEq 18 matches an adult" "adult" (classifyAge 30)
assertEqual "Lt 0 matches a negative age" "invalid" (classifyAge -1)

assertTrue "Eq matches structurally equal strings" (match "ok" with Eq "ok" -> true | _ -> false)
assertTrue "NEq matches structurally different strings" (match "fail" with NEq "ok" -> true | _ -> false)

printfn "02-patterns.fsx: all assertions passed"
