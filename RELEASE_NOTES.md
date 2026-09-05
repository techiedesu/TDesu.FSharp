## 2.1.0

Three additions to `Concurrency` and a new `Union` module, each replacing a pattern a consumer had
been hand-rolling: sixteen background loops sharing the same `while`/`try`/`Task.Delay`/`with _ ->
()` shape whose first pass has to run at startup rather than after the first wait, a bounded update
queue built around a raw `SemaphoreSlim` with both a drop-and-report post and a wait-for-room post,
and an uncached `FSharpValue.GetUnionFields` call on every one of a high-volume stream of routed
messages just to read back the case it was already routing on.

### Added
- `PeriodicTimer.startImmediate` -- like `start`, but the first run happens immediately instead of
  waiting out the first interval. Cancellation and error handling are unchanged: a throwing run is
  reported to `onError` and the loop keeps going, and the returned task completes (never faults)
  once the token fires
- `PeriodicTimer.run` -- a loop whose own step decides the pause before the next run, for loops that
  need to tighten up when busy and back off when idle instead of ticking on a fixed `interval`.
  `step: unit -> Task<TimeSpan>` returns that pause directly; a throwing step's pause comes from
  `onError: exn -> TimeSpan` instead of a fixed retry cadence. In both loops only a cancellation
  that is `ct`'s own ends the loop quietly: an `OperationCanceledException` raised for any other
  reason — an inner deadline, a foreign token — is reported like any failure and waited out, so a
  step that keeps being cancelled is paused between attempts rather than spun on
- `ChannelWorker.startBounded` and `ChannelWorker.BoundedHandle<'T>` -- a
  `System.Threading.Channels`-backed counterpart to `start`/`Handle<'T>`, for a producer that must
  not be left to outrun its handler by growing an unbounded queue underneath it. `TryPost` keeps the
  never-blocks, false-when-full contract the hand-rolled version had; `PostAsync` is the
  wait-for-room half it did not have. Cancelling the worker's token is still the abandon-the-queue
  shutdown `start` has always had; `BoundedHandle.Complete()` is the new, graceful one -- it refuses
  further posts but lets everything already queued finish, and `Completion` completes only once that
  drain does
- `Union.caseName` / `Union.caseNames` -- the case name a union value was built with, and every case
  name of a union type in declaration order. Reading the case name back out of
  `FSharpValue.GetUnionFields` on every message of a routed stream repeats the same reflection
  lookup every time; `caseName` reads the tag through a `FSharpValue.PreComputeUnionTagReader`
  compiled once per union type and cached from then on. Raises `ArgumentException` for a type that
  is not an F# union

### Tests
- 561 tests to 578: cancellation and error-recovery coverage for `startImmediate` and `run` matching
  the existing `start`/`startCounted` suite, `startBounded` coverage for back pressure, ordering,
  error recovery and both shutdown paths, and `Union` coverage for a multi-case union with and
  without fields, case-name order, and the non-union `ArgumentException`

## 2.0.0

A loop for `task { }` code that used to be written as recursion, and eight `Result` functions
handed back to FSharp.Core. The removals are the reason for the major number: nothing that calls
them changes, but the library's own policy counts a removal as breaking, and a 2.0 says so.

### Added
- `Task.loop` and the `Loop<'State, 'Result>` union it returns (`Loop.Continue state` /
  `Loop.Stop result`). `return! step next` inside a `task { }` is not a tail call: the builder awaits
  the inner task, so every activation stays registered as the continuation of the one after it. A
  receive loop written that way was measured retaining 568 bytes per iteration until its connection
  dropped — one state-machine box per frame, independent of frame size — and overflowing a 1 MB
  thread-pool stack between 1,000 and 1,500 iterations once the awaited reads completed
  synchronously, which a socket read does whenever the data is already buffered. `Task.loop` runs
  the step from one `while`, so the same code runs in constant heap and stack. The test drives a
  million synchronous steps; the recursive form did not survive two thousand

### Removed
- `Result.map`, `bind`, `mapError`, `isOk`, `isError`, `defaultValue`, `defaultWith` and `toOption`.
  FSharp.Core 9 ships all eight under the same names with the same shapes, and F# resolves
  `Result.map` across every module of that name in scope, so a caller that opens `TDesu.FSharp`
  and writes `Result.map` now gets FSharp.Core's — same `inline`, same `InlineIfLambda`. Verified
  by building every consumer of this package against 2.0.0 without a source change. What stays in
  `Result` is what FSharp.Core lacks: `get`, `valueOr`, `orElse`, `orElseWith`, `tee`, `teeError`,
  `ofOption`, `zip`, `ignore`, `requireTrue`, `requireFalse`, `requireNotNull`, `catch`

## 1.5.0

Fills out the parsing surface and settles a culture question `1.4.1` left open.

### Added
- `tryParseV` returning `ValueOption` beside every `tryParse` — the parity the library already keeps
  between `Tasks/TaskOption.fs` and `Tasks/TaskVOption.fs`, which the parsers were the last place to
  be missing. `tryParse` stays the default: it composes with `List.choose`, `Option.map` and the rest
  of FSharp.Core, and `tryParseV` only pays off where the value stays out of them — a hot loop parsing
  millions of fields
- Parsers for the types that had none: `SByte`, `UInt16`, `UInt32`, `UInt64`, `DateTime`, `TimeSpan`
- `Char.tryParse` / `Char.tryParseV` — a string of any length but one is a failure, not a truncation
- `Version.tryParse` / `Version.tryParseV`
- `Enum.tryParse` / `tryParseIgnoreCase` / `tryParseV` / `tryParseVIgnoreCase`, generic over the enum
- `tryParseInv` / `tryParseInvV` on the six culture-sensitive modules — `Single`, `Double`, `Decimal`,
  `DateTime`, `DateTimeOffset`, `TimeSpan`. Text that came from a machine — a database column, a JSON
  number, a protocol field, a config value — is invariant by construction, and parsing it with
  `tryParse` makes the result depend on the host: `Double.tryParse "1.5"` is `None` on every locale
  whose decimal separator is a comma, which is most of Europe. `tryParseInv` pins
  `CultureInfo.InvariantCulture` so that text reads the same regardless of where the process runs.
  The naming follows the distinction this library already draws in `Char.toUpper`/`toUpperInv` and
  `String.toLower`/`toLowerInv`: the plain name follows the ambient culture, the `Inv` suffix pins the
  invariant one. `tryParse` is unchanged from `1.4.1` and is still the right call for text a person
  typed in their own locale, where the ambient culture is the question actually being asked
- The `Inv` numeric parsers (`Single`, `Double`, `Decimal`) explicitly request `NumberStyles.Float`
  and so refuse group separators, even though the ambient `tryParse` overloads accept them by BCL
  default. Under the invariant culture the group separator is a comma, so allowing it would read
  `"1,5"` as `15.0` — ten times the intended value, and silently. Machine-generated text never
  legitimately carries a group separator, so refusing it costs nothing and removes the corruption

### Not added, and why
- `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half` have no parser: the package targets
  `netstandard2.1` and none of those types exist there — verified by compiling each against the
  netstandard2.1 reference assemblies, which reports all five as undefined. `netstandard` has no
  version above 2.1, so the only way to reach them is to target .NET directly, which this package
  deliberately does not do

### Tests
- The parsing modules had no tests at all before this release; they now do, including a two-way
  culture-contract check — one test forces `ru-RU` and asserts `tryParseInv` is unmoved by it, a
  second forces the same culture and asserts `tryParse` does follow it, reading `"1,5"` as
  one-and-a-half and refusing `"1.5"`. 517 tests to 558

## 1.4.1

No API change — infrastructure, tests and formatting only.

### Added
- Tests for the modules that had none: `CancellationToken` helpers, `File`/`Directory`/`Path`,
  `TemporaryFileStream`, the `ArrayPool` helpers (including that the buffer is returned when the
  callback throws) and `PeriodicTimer` cancellation. 477 tests to 517
- `RELEASING.md`, documenting the release process `release.yml` already implements
- `manage.fsx benchcheck` — compiles the benchmark project, now gated in CI so benchmark code
  cannot rot into non-compiling code unnoticed
- `manage.fsx format` / `format --check`, gated in CI. The repository is formatted with
  Funtomatoes; the style is pinned in `.editorconfig` and the tool version in the manifest so
  neither can change the whole tree by surprise

### Changed
- `Concurrency/Concurrency.fs` split one file per type, the last multi-module file left
- `end_of_line = unset` dropped from `.editorconfig`: not a valid value

## 1.4.0

### Added
- `Byref`: in-place `inc`, `dec`, `setv`, `add`, `sub`, `mul`, `div` over a byref, generic via `LanguagePrimitives`
- `Array.valueTryFind`, `valueTryFindLast`, `valueChooseFirst`, `valueChooseLast` — the `tryFind` family returning `ValueOption`, so a lookup in a hot loop allocates nothing
- `EqualityComparer.create` / `createBy` — netstandard2.1 has no `EqualityComparer<'T>.Create`; the BCL added it in .NET 8
- `Seq.toResizeArray` — single pass, pre-sized when the source is an `ICollection<'T>`

### Changed
- `Array.ofMemoryStream` moved from `Collections/Streams.fs` to `Collections/Array.fs`. Same qualified name, same behaviour: F# does not allow one module to span two files (FS0248)
- Tests reorganised to mirror the source layout; no library change

## 1.3.0

### Removed
- Fable support. It was never real: 20 of 43 source files carried `#if FABLE_COMPILER`
  branches and the package shipped a `fable/` source copy, but the library has never
  compiled under Fable — it fails on `LinkedList<T>`, `Stack<T>.Enumerator`, `Stopwatch`,
  `ConfiguredTaskAwaitable` and `Interlocked.Exchange`. The conditionals, the `fable/`
  pack target, the `Fable` build configuration and the claim are gone.

Nothing changes for .NET consumers: the public surface is byte-identical, 551 members
before and after.

## 1.2.1

Documentation only; no API or behaviour change.

### Fixed
- The readme shipped inside the 1.2.0 package documented none of what 1.2.0 added. Every new API now has a readme entry and a runnable example
- Several readme and `docs/index.md` code blocks never compiled: missing `open` lines in the Operators, Task and Collections sections, a bare `dict` colliding with FSharp.Core's builtin, `Result.valueOr` needing a type annotation, and a `taskResult` Bind-overload ambiguity
- `docs/tutorial.fsx` opened a `TDesu.FSharp.MaybeBuilder` module that does not exist

## 1.2.0

### Added
- Comparison active patterns: `Eq`, `NEq`, `Lt`, `Gt`, `LtEq`, `GtEq`, `Between`
- Numeric: `clamp`, `lerp`, `inverseLerp`, `isBetween`, `zero`, `one`
- Enum: `hasFlag`, `addFlag`, `addFlagWhen`, `removeFlag`, `removeFlagWhen`
- ValueStringBuilder: stack-first string builder over a caller-supplied `Span`, growing into `ArrayPool`
- String: `equalsAny`, `containsAny`, `endsWithAny` and their char-set variants
- Option/ValueOption: `tryCast`, `ofPredicate`
- ResizeArray: `choose`, `mapi`, `rev`, `partition`, `tryFindIndex`, `forall`
- Seq: `tryMaxBy`, `tryMinBy`
- Stream: `copyUpTo` — bounded copy that fails past a byte cap instead of reading an unbounded stream

### Fixed
- `Disposable.combine` disposed nothing after the first child that threw; it now disposes all of them, aggregates the failures, and is idempotent
- `Task.parallelThrottle` disposed its semaphore while spawned tasks could still release it, surfacing unobserved `ObjectDisposedException`s when the loop threw
- `SlidingWindowLimiter` held its window one tick longer than configured and refused boundary requests with a zero wait time
- Null arguments raised `NullReferenceException` from a dereference in several helpers (notably `String.startsWithAny`); a null collection now reads as empty and a null mutation target throws `ArgumentNullException` naming the parameter

### Changed
- Partial active patterns carrying no value now return `bool`; the extracting ones return `ValueOption` via `[<return: Struct>]`. Both are allocation-free and unchanged at the use site
- `Operators.isNotNull` is renamed `isNotNullRef` and paired with a new `isNullRef`. The old name remains as an `[<Obsolete>]` alias and will be removed in 2.0
- Source split one module per file; no API change

## 1.1.0

### Changed
- StateMachine: removed `Builder`, `Definition`, and `apply` — use plain `match` expressions for transitions
- StateMachine: `tryApply` now takes `(state, result)` instead of `(definition, state, event)`

## 1.0.0

Initial public release.

### Core
- Operators: `^` (apply), `%` (ignore), `always`, `tee`, `swap`, `icast`/`ecast`
- Guard: `notNull`, `notEmpty`, `inRange`, `positive`
- UnixTime: cached high-resolution `seconds`/`milliseconds`
- String: 25+ pipeable wrappers (`contains`, `split`, `replace`, `truncate`, `toOption`, ...)
- Option: `toResult`, `zip`, `map2`/`map3`, `tee`, `ofString`
- Result: `defaultValue`, `orElse`, `zip`, `requireTrue`, `catch`, `tee`/`teeError`
- Validation: applicative error accumulation with `and!` support
- NumericParsing: `tryParse` for Int16–Int64, Double, Single, Decimal, Byte, Bool, Guid, DateTimeOffset
- Clock: `IClock` interface, `SystemClock`, `FakeClock` for testing
- StateMachine: lightweight FSM helpers (`goto`/`stay`/`fail`/`tryApply`) — use plain `match` for transitions

### Tasks
- Task: `map`, `bind`, `zip`/`zip3`, `catch`, `singleton`, `fireAndForget`, `parallelThrottle`
- TaskResult: `map`, `bind`, `mapError`, `defaultValue`, `tee`/`teeError`
- TaskGroup: structured concurrency with cancellation

### Collections
- Dictionary: `tryGetValue`, `getOrDefault`
- ResizeArray: full functional wrapper (`map`, `filter`, `fold`, `sort`, `tryFind`, ...)
- Seq/List: `tryMax`, `tryMin`, `tryAverage`
- Stack: `tryPeek`, `push`, `pop`, `reverse`

### Concurrency
- AtomicInt / AtomicInt64: thread-safe counters
- BoundedDict / BoundedQueue: auto-evicting bounded collections
- Signal: one-shot async notification
- PeriodicTimer: background recurring work
- ChannelWorker: sequential background processor
- SlidingWindowLimiter: rate limiting

### Resilience
- Retry: exponential backoff, fixed delay
- CircuitBreaker: threshold + cooldown
- Timeout: hard deadline with linked cancellation
- Memoize: sync/async with optional TTL
- Saga: transactional orchestration with compensation

### IO
- Env: `getVar`, `requireVar`, `getVarOr`
- Disposable: `deferStack`, `create`, `combine`, `createOnce`
- TemporaryFileStream: auto-deleting temp file backed stream
- File / Directory helpers

### Buffers
- Bytes: `xor`, `concat2`/`3`/`4`, `constantTimeEquals`, `slice`, `fill`
- ArrayPool: `useBytes`, `usePooled`, `withCopy`

### Hashing
- ContentHash: SHA256, SHA1, MD5 (hex output)
- Hash: `combine2`/`3`/`4`, `ofSeq`, `ofArray`, `ofList`
- CollectionComparer: structural equality for byte[], arrays, lists

### ActivePatterns
- Parse: `Int`, `Int64`, `Double`, `Decimal`, `Bool`, `Guid`, `DateTimeOffset`
- String: `NullOrWhiteSpace`, `Empty`, `WhiteSpace`, `StartsWithAny`

### Builders
- `result {}` — synchronous Result pipelines
- `option {}` — Option pipelines (binds Option and ValueOption)
- `taskResult {}` — async Result pipelines (binds Task<Result>, Result, Task)
- `validation {}` — applicative validation with `and!`

### Types
- NonEmptyString: validated non-null/non-empty string
- ApiResponse: `ok`/`error`/`ofResult` wrapper

### Other
- Fable compatible (sources included in nupkg)
- netstandard2.1 target
- XML docs on all public APIs
- Unlicense
