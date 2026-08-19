// Shared plumbing loaded by every numbered example: `#load "_prelude.fsx"` then
// `open Prelude`. Do not run this file directly -- it has nothing to demonstrate on
// its own.
//
// It does two things:
//
// 1. `#load "obj/ref.generated.fsx"` -- brings TDesu.FSharp into scope, either from
//    the locally built DLL or from a freshly packed nupkg. That file is written by
//    `manage.fsx`'s `cmdExamples`, driven by TDESU_EXAMPLES_SOURCE=dll|nupkg (default
//    "dll"; set by `--nupkg`). It is *not* generated here: FSI resolves every
//    #load/#r/#i directive reachable from a script -- transitively, through nested
//    #load -- before any code in that script executes, so a script cannot write its
//    own reference and then #load it in the same process. `manage.fsx` writes it in
//    an earlier, separate `dotnet fsi` invocation, so by the time this file's #load
//    runs, it already exists. See `examples/obj/ref.generated.fsx` after running
//    `dotnet fsi manage.fsx examples` to see what got generated.
//
//      dll   -- catches "the API changed shape" (a function renamed/removed/re-typed).
//      nupkg -- catches "the nupkg is missing a dependency / targets the wrong TFM",
//               a failure mode the DLL reference above cannot see, since it never
//               goes through NuGet restore or the .nuspec dependency list.
//
// 2. Defines `assertEqual`/`assertTrue` -- these scripts double as the FSI consumption
//    test, so a wrong result must be a non-zero process exit, not a line of printed
//    output nobody reads.
module Prelude

#load "obj/ref.generated.fsx"

/// Prints the label and value on success; raises (non-zero process exit) on mismatch.
let assertEqual (label: string) (expected: 'T) (actual: 'T) =
    if actual = expected then
        printfn "  ok   %s = %A" label actual
    else
        failwithf "FAIL %s: expected %A, got %A" label expected actual

/// Same as `assertEqual` but for a bare boolean condition.
let assertTrue (label: string) (condition: bool) =
    if condition then
        printfn "  ok   %s" label
    else
        failwithf "FAIL %s: expected true" label
