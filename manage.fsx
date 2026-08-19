open System
open System.Diagnostics
open System.IO

// ── Paths ──────────────────────────────────────────────────────────────
[<Literal>]
let solution = "TDesu.FSharp.slnx"

[<Literal>]
let testProj = "tests/TDesu.FSharp.Tests/TDesu.FSharp.Tests.fsproj"

[<Literal>]
let benchProj = "benchmarks/TDesu.FSharp.Benchmarks/TDesu.FSharp.Benchmarks.fsproj"

[<Literal>]
let artifactsDir = "artifacts"

[<Literal>]
let examplesDir = "examples"

// ── Guards ──────────────────────────────────────────────────────────────
let scriptDir = __SOURCE_DIRECTORY__

do
    if not (File.Exists(Path.Combine(scriptDir, solution))) then
        eprintfn "ERROR: %s not found — run this script from the repo root." solution
        exit 1

// ── Helpers ─────────────────────────────────────────────────────────────
let runCapture (cmd: string) (args: string) =
    let psi =
        ProcessStartInfo(
            FileName = cmd,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            WorkingDirectory = scriptDir
        )

    use p = Process.Start(psi)
    let output = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    output.Trim()

/// Resolve a local dotnet tool DLL from the NuGet global-packages cache.
/// Workaround for .NET 10 SDK bug where `dotnet <tool>` shim fails.
let resolveToolDll (toolId: string) (version: string) (tfm: string) =
    let raw = runCapture "dotnet" "nuget locals global-packages --list"
    let prefix = "global-packages: "

    let pkgDir =
        match raw.IndexOf(prefix) with
        | -1 -> failwith $"Cannot resolve NuGet global-packages path from: {raw}"
        | i -> raw.Substring(i + prefix.Length).Trim()

    let toolCmd = toolId.Replace("-tool", "")

    let dll =
        Path.Combine(pkgDir, toolId, version, "tools", tfm, "any", $"{toolCmd}.dll")

    if not (File.Exists dll) then
        runCapture "dotnet" "tool restore" |> ignore

        if not (File.Exists dll) then
            failwith $"Tool DLL not found: {dll}"

    dll

let color c (msg: string) =
    let prev = Console.ForegroundColor

    try
        Console.ForegroundColor <- c
        Console.WriteLine(msg)
    finally
        Console.ForegroundColor <- prev

let run (cmd: string) (args: string) =
    let psi =
        ProcessStartInfo(FileName = cmd, Arguments = args, UseShellExecute = false, WorkingDirectory = scriptDir)

    use p = Process.Start(psi)

    use _cancel =
        Console.CancelKeyPress.Subscribe(fun e ->
            e.Cancel <- true

            try
                p.Kill(true)
            with _ ->
                ()
        )

    p.WaitForExit()
    p.ExitCode

let runOrExit cmd args =
    let code = run cmd args

    if code <> 0 then
        color ConsoleColor.Red $"FAILED: {cmd} {args} (exit code {code})"
        exit code

    code

/// Like `run`, but sets one extra environment variable on the child process.
let runWithEnv (envName: string) (envValue: string) (cmd: string) (args: string) =
    let psi =
        ProcessStartInfo(FileName = cmd, Arguments = args, UseShellExecute = false, WorkingDirectory = scriptDir)

    psi.Environment[envName] <- envValue
    use p = Process.Start(psi)

    use _cancel =
        Console.CancelKeyPress.Subscribe(fun e ->
            e.Cancel <- true

            try
                p.Kill(true)
            with _ ->
                ()
        )

    p.WaitForExit()
    p.ExitCode

let deleteDirs (dirs: string list) =
    for d in dirs do
        let full = Path.Combine(scriptDir, d)

        if Directory.Exists(full) then
            Directory.Delete(full, true)
            printfn "Deleted %s" d

let deleteRecursive (name: string) =
    Directory.EnumerateDirectories(scriptDir, name, SearchOption.AllDirectories)
    |> Seq.iter (fun d ->
        Directory.Delete(d, true)
        printfn "Deleted %s" (Path.GetRelativePath(scriptDir, d))
    )

// ── Commands ────────────────────────────────────────────────────────────
let cmdBuild () =
    color ConsoleColor.Cyan "Building solution (Release)..."
    runOrExit "dotnet" $"build {solution} -c Release"

let cmdTest () =
    color ConsoleColor.Cyan "Running tests..."
    runOrExit "dotnet" $"test {solution} -c Release"

let cmdBench () =
    color ConsoleColor.Cyan "Running benchmarks..."
    runOrExit "dotnet" $"run --project {benchProj} -c Release"

let cmdBenchCheck () =
    // BenchmarkDotNet's `--job dry` CLI flag does not replace the Job.ShortRun already
    // baked into Program.fs -- ManualConfig.Union adds job lists together rather than
    // overriding them, so passing it here would run every benchmark under *both* jobs
    // instead of the fast one. Wiring a real dry run cleanly needs a change inside the
    // benchmark project itself, which is out of scope for this command. A Release
    // compile is the cheap, zero-surprise regression gate: it catches the actual failure
    // mode (benchmarks rotting into non-compiling code) without ever executing them.
    color ConsoleColor.Cyan "Checking benchmarks compile (Release)..."
    runOrExit "dotnet" $"build {benchProj} -c Release"

/// Everything Funtomatoes should own. Kept in one place so `format` and
/// `format --check` can never drift apart, which is how a format gate starts
/// failing on files nobody formats.
let formatTargets = "src tests examples benchmarks manage.fsx"

let cmdFormat (check: bool) =
    if check then
        color ConsoleColor.Cyan "Checking formatting..."
        runOrExit "dotnet" $"funtomatoes {formatTargets} --check"
    else
        color ConsoleColor.Cyan "Formatting..."
        runOrExit "dotnet" $"funtomatoes {formatTargets}"

let resolveFsdocs () =
    let manifest =
        File.ReadAllText(Path.Combine(scriptDir, ".config/dotnet-tools.json"))

    let vStart = manifest.IndexOf("\"version\": \"") + "\"version\": \"".Length
    let vEnd = manifest.IndexOf("\"", vStart)
    let fsdocsVersion = manifest.Substring(vStart, vEnd - vStart)
    resolveToolDll "fsdocs-tool" fsdocsVersion "net8.0"

let cmdDocs () =
    color ConsoleColor.Cyan "Building docs..."
    let fsdocs = resolveFsdocs ()

    runOrExit
        "dotnet"
        $"\"{fsdocs}\" build --clean --output docs/output --properties Configuration=Release --parameters root /TDesu.FSharp/"

let cmdDocsWatch () =
    color ConsoleColor.Cyan "Watching docs..."
    let fsdocs = resolveFsdocs ()
    runOrExit "dotnet" $"\"{fsdocs}\" watch --properties Configuration=Release --parameters root /TDesu.FSharp/"

let cmdPack () =
    color ConsoleColor.Cyan $"Packing into ./{artifactsDir}..."
    runOrExit "dotnet" $"pack {solution} -c Release -o {artifactsDir}"

/// Regenerates examples/obj/ref.generated.fsx -- the single `#r`/`#i` pair every
/// example script picks up via `#load "_prelude.fsx"`. Written here, in this
/// process, rather than by the prelude itself: FSI resolves every #load/#r/#i
/// directive reachable from a script -- transitively, through nested #load -- before
/// any code in that script runs, so a script cannot compute its own reference and
/// #load it in the same process. This function is the separate, earlier step that
/// makes the file exist before any example script's `dotnet fsi` process starts.
let private writeExamplesRef (useNupkg: bool) =
    let forwardSlash (p: string) =
        p.Replace(Path.DirectorySeparatorChar, '/')

    let objDir = Path.Combine(scriptDir, examplesDir, "obj")
    Directory.CreateDirectory(objDir) |> ignore

    let lines =
        if useNupkg then
            let artifactsFull = Path.Combine(scriptDir, artifactsDir)
            let prefix, suffix = "TDesu.FSharp.", ".nupkg"

            let newest =
                Directory.GetFiles(artifactsFull, "TDesu.FSharp.*.nupkg")
                |> Array.filter (fun f -> not (f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase)))
                |> Array.sortByDescending File.GetLastWriteTimeUtc
                |> Array.tryHead

            match newest with
            | None -> failwith $"No TDesu.FSharp.*.nupkg found in ./{artifactsDir} after packing."
            | Some path ->
                let name = Path.GetFileName path

                let version =
                    name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length)

                color ConsoleColor.Cyan $"Examples reference: nupkg TDesu.FSharp {version} (from ./{artifactsDir})"

                [
                    sprintf "#i \"nuget: %s\"" (forwardSlash artifactsFull)
                    sprintf "#r \"nuget: TDesu.FSharp, %s\"" version
                ]
        else
            let dll =
                Path.Combine(scriptDir, "src/TDesu.FSharp/bin/Release/netstandard2.1/TDesu.FSharp.dll")

            if not (File.Exists dll) then
                failwith $"Built DLL not found at {dll}"

            color ConsoleColor.Cyan $"Examples reference: dll {dll}"
            [ sprintf "#r \"%s\"" (forwardSlash dll) ]

    File.WriteAllText(Path.Combine(objDir, "ref.generated.fsx"), String.Join("\n", lines) + "\n")

let cmdExamples (useNupkg: bool) =
    cmdBuild () |> ignore

    if useNupkg then
        cmdPack () |> ignore

    writeExamplesRef useNupkg
    let sourceLabel = if useNupkg then "nupkg" else "dll"

    let scripts =
        Directory.GetFiles(Path.Combine(scriptDir, examplesDir), "*.fsx")
        |> Array.filter (fun f -> not ((Path.GetFileName f).StartsWith "_"))
        |> Array.sortBy Path.GetFileName

    color ConsoleColor.Cyan $"Running {scripts.Length} example script(s) (source={sourceLabel})..."
    let mutable exitCode = 0
    let mutable i = 0

    while exitCode = 0 && i < scripts.Length do
        let name = Path.GetFileName scripts[i]
        printfn ""

        let code =
            runWithEnv "TDESU_EXAMPLES_SOURCE" sourceLabel "dotnet" $"fsi \"{scripts[i]}\""

        if code = 0 then
            color ConsoleColor.Green $"PASS  {name}"
        else
            color ConsoleColor.Red $"FAIL  {name} (exit code {code})"
            exitCode <- code

        i <- i + 1

    if exitCode = 0 then
        color ConsoleColor.Green $"All {scripts.Length} example script(s) passed."

    exitCode

let cmdClean () =
    color ConsoleColor.Cyan "Cleaning..."
    run "dotnet" $"clean {solution}" |> ignore
    deleteRecursive "bin"
    deleteRecursive "obj"
    deleteDirs [ artifactsDir; "output" ]
    color ConsoleColor.Green "Clean complete."
    0

let cmdWatch () =
    color ConsoleColor.Cyan "Watching tests..."
    runOrExit "dotnet" $"watch test --project {testProj}"

let showHelp () =
    printfn ""
    printfn "Usage: dotnet fsi manage.fsx <command>"
    printfn ""
    printfn "Commands:"
    printfn "  build   Build solution (Release)"
    printfn "  test    Run tests"
    printfn "  bench   Run benchmarks"
    printfn "  benchcheck        Compile-check the benchmark project (fast CI regression gate)"
    printfn "  format            Format all F# sources with Funtomatoes"
    printfn "  format --check    Fail if anything is unformatted (what CI runs)"
    printfn "  examples          Build, then run examples/*.fsx via dotnet fsi"
    printfn "  examples --nupkg  Pack, then run examples against the packed nupkg"
    printfn "  docs             Build fsdocs documentation"
    printfn "  docs --watch     Watch & live-reload docs"
    printfn "  pack    Pack NuGet into ./artifacts"
    printfn "  clean   Clean all build outputs"
    printfn "  watch   Watch & re-run tests on change"
    printfn ""
    0

// ── Entry point ─────────────────────────────────────────────────────────
let args = fsi.CommandLineArgs // argv.[0] = script name
let command = if args.Length > 1 then args.[1].ToLowerInvariant() else ""

let hasFlag (flag: string) =
    args |> Array.exists (fun a -> a.ToLowerInvariant() = flag)

let exitCode =
    match command with
    | "build" -> cmdBuild ()
    | "test" -> cmdTest ()
    | "bench" -> cmdBench ()
    | "benchcheck" -> cmdBenchCheck ()
    | "format" -> cmdFormat (hasFlag "--check")
    | "examples" -> cmdExamples (hasFlag "--nupkg")
    | "docs" -> if hasFlag "--watch" then cmdDocsWatch () else cmdDocs ()
    | "pack" -> cmdPack ()
    | "clean" -> cmdClean ()
    | "watch" -> cmdWatch ()
    | other ->
        if other <> "" then
            color ConsoleColor.Red $"Unknown command: {other}"

        showHelp ()

if exitCode = 0 && command <> "" then
    color ConsoleColor.Green "Done."

exit exitCode
