// Operators, String, and the Option/ValueOption/Result combinators -- the small
// pieces you reach for constantly. Run directly with `dotnet fsi examples/01-core.fsx`
// (defaults to the built DLL), or via `dotnet fsi manage.fsx examples`.
#load "_prelude.fsx"
open Prelude
open System
open TDesu.FSharp
// `Operators` is a plain module, not [<AutoOpen>] -- `open TDesu.FSharp` alone does
// not bring `^`/`%`/`tee` into scope. Without this line `^` silently resolves to
// FSharp.Core's own deprecated string-concat operator instead of failing to compile.
open TDesu.FSharp.Operators

// ── Operators ──────────────────────────────────────────────────────────
// `^` is reverse application, like `<|` but binding tighter than comparison/logical
// operators, so it clears up nested calls that would otherwise need parens.
assertEqual "^ applies without parens" "43" (string ^ 42 + 1)

// `%` discards a return value -- useful for fluent/void-ish C# APIs called from F#.
let sb = System.Text.StringBuilder()
%sb.Append("hi")
assertEqual "% discards Append's chained return" "hi" (sb.ToString())

// `tee` runs a side effect and passes its input through unchanged: great for logging
// mid-pipeline without breaking the pipeline.
let mutable observed = 0
let teed = 42 |> tee (fun v -> observed <- v)
assertEqual "tee passes the value through" 42 teed
assertEqual "tee also ran its side effect" 42 observed

// `swap` flips a 2-arg function's argument order; `always` ignores its 2nd argument.
assertEqual "swap f a b = f b a" 2 (swap (-) 3 5)
assertEqual "always x _ = x" 0 (always 0 "ignored")

// ── String ─────────────────────────────────────────────────────────────
// Pipeline-friendly wrappers over System.String members.
assertEqual "String.trim" "hello" (String.trim "  hello  ")
assertEqual "String.split + String.join re-punctuates" "a|b|c" ("a,b,c" |> String.split "," |> String.join "|")
assertEqual "String.truncate" "long" (String.truncate 4 "long text")
assertEqual "String.toOption on blank -> None" None (String.toOption "   ")
assertEqual "String.toOption on content -> Some" (Some "hi") (String.toOption "hi")
assertEqual "String.countOccurrences" 2 ("abcabc" |> String.countOccurrences "abc")

// ── Option ─────────────────────────────────────────────────────────────
assertEqual "Option.toResult Some -> Ok" (Ok 42) (Some 42 |> Option.toResult "missing")
assertEqual "Option.toResult None -> Error" (Error "missing") (None |> Option.toResult "missing")
assertEqual "Option.zip pairs two Somes" (Some(1, "a")) (Option.zip (Some 1) (Some "a"))
assertEqual "Option.map2 combines two Somes" (Some 3) (Option.map2 (+) (Some 1) (Some 2))
assertEqual "Option.ofString blank -> None" None (Option.ofString "   ")

// ── ValueOption ────────────────────────────────────────────────────────
// The struct counterpart of Option -- no allocation, meant for hot paths.
// `ofCSharpTryPattern` adapts a TryXxx (bool * value) tuple straight from a BCL call.
assertEqual "ValueOption.ofCSharpTryPattern success" (ValueSome 42) (Int32.TryParse "42" |> ValueOption.ofCSharpTryPattern)
assertEqual "ValueOption.ofCSharpTryPattern failure" ValueNone (Int32.TryParse "nope" |> ValueOption.ofCSharpTryPattern)

// ── Result ─────────────────────────────────────────────────────────────
assertEqual "Result.defaultValue on Ok" 42 (Ok 42 |> Result.defaultValue 0)
assertEqual "Result.defaultValue on Error" 0 (Error "x" |> Result.defaultValue 0)
assertEqual "Result.bind short-circuits on Error" (Error "boom") (Error "boom" |> Result.bind (fun v -> Ok(v + 1)))
assertEqual "Result.ofOption mirrors Option.toResult" (Ok 42) (Result.ofOption "missing" (Some 42))
assertEqual "Result.catch turns a thrown exception into Error" true (Result.catch (fun () -> int "not a number") |> Result.isError)
assertEqual "Result.catch turns success into Ok" (Ok 42) (Result.catch (fun () -> int "42"))

printfn "01-core.fsx: all assertions passed"
