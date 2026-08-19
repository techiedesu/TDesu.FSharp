// TDesu.FSharp.Buffers.ValueStringBuilder: a stack-first string builder for a hot path that
// builds and consumes exactly one string per call. It is a `[<Struct; IsByRefLike>]`, the same
// tradeoff that keeps the BCL's own System.Text.ValueStringBuilder internal, so it comes with
// hard restrictions this file exists to demonstrate correctly, not to work around.
#load "_prelude.fsx"

open Prelude
open System
open TDesu.FSharp.Buffers

// ── The one supported shape: local `let mutable`, statement calls, Dispose() in `finally` ──
// IsByRefLike means it can never be captured by a closure, stored in a field, or held live
// across an `async`/`task` suspension point -- the compiler rejects all of those outright, so
// there is no closure-taking helper API for it (a `ValueStringBuilder byref -> 'R` callback
// parameter simply cannot be written). It also has no IDisposable implementation, so `use`
// doesn't apply -- dispose it by hand from a `finally`; Dispose() is idempotent, so that is
// safe even after ToString() (below) has already disposed it once.
//
// It must never be passed by value: a copy shares the same backing array, so growing or
// disposing through either the original or the copy leaves the other pointing at memory the
// pool may already have handed to someone else. That is why no member returns `this` -- fluent
// chaining would copy -- and why every call site here mutates one local in place instead.
let describe (name: string) (count: int) =
    let buffer = Array.zeroCreate<char> 64
    let mutable sb = ValueStringBuilder(Span<char>(buffer))

    try
        sb.Append("name=")
        sb.Append(name)
        sb.Append(", count=")
        sb.Append(string count)
        sb.ToString()
    finally
        sb.Dispose()

assertEqual
    "ValueStringBuilder builds a string from statement-style Append calls"
    "name=widget, count=3"
    (describe "widget" 3)

// ── Growth: once the initial buffer is full it rents from ArrayPool<char>.Shared instead of
// throwing -- the caller never has to size the buffer for the worst case, only the common one.
let buildLong () =
    let buffer = Array.zeroCreate<char> 4 // deliberately too small -- forces a Grow()
    let mutable sb = ValueStringBuilder(Span<char>(buffer))

    try
        for _ in 1..20 do
            sb.Append('x')

        sb.AppendLine()
        sb.Append("done")
        sb.ToString()
    finally
        sb.Dispose()

assertEqual
    "ValueStringBuilder grows into the pool past its initial buffer without losing data"
    (String('x', 20) + Environment.NewLine + "done")
    (buildLong ())

// ── The other constructor: skip the caller buffer entirely and rent from the pool up front --
// for call sites with no convenient stack buffer to hand in.
let buildPooled () =
    let mutable sb = ValueStringBuilder(8)

    try
        sb.Append("pooled from the start")
        sb.ToString()
    finally
        sb.Dispose()

assertEqual
    "ValueStringBuilder(initialCapacity) rents its whole buffer from the pool"
    "pooled from the start"
    (buildPooled ())

printfn "07-value-string-builder.fsx: all assertions passed"
