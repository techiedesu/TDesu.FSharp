## 1.5.0-alpha.1

Pre-release: the culture change below alters what existing float and date parsers accept, so it wants
use against real data before a stable `1.5.0`.

### Added
- `tryParseV` returning `ValueOption` beside every `tryParse` — the parity the library already keeps
  between `Tasks/TaskOption.fs` and `Tasks/TaskVOption.fs`, which the parsers were the last place to
  be missing. `tryParse` remains the one to reach for: it composes with `List.choose`, `Option.map`
  and the rest of FSharp.Core, and `tryParseV` only pays off where the value stays out of them
- Parsers for the types that had none: `SByte`, `UInt16`, `UInt32`, `UInt64`, `DateTime`, `TimeSpan`
- `Char.tryParse` / `Char.tryParseV` — a string of any length but one is a failure, not a truncation
- `Version.tryParse` / `Version.tryParseV`
- `Enum.tryParse` / `tryParseIgnoreCase` / `tryParseV` / `tryParseVIgnoreCase`, generic over the enum
- Tests for the parsing modules, which had none at all: 517 tests to 557

### Changed
- **Floating point and dates now parse against the invariant culture rather than the ambient one.**
  `Double.tryParse "1.5"` returned `None` on every locale whose decimal separator is a comma — ru-RU,
  de-DE, fr-FR, most of Europe — because the ambient culture decided what a decimal point meant. The
  text these functions actually receive is machine-generated (a database column, a JSON number, a
  protocol field, a config value) and is invariant by construction, so the ambient culture was never
  the right question to ask of it. Affects `Single`, `Double`, `Decimal`, `DateTimeOffset` and the new
  `DateTime` and `TimeSpan`
- Group separators are no longer accepted in floating point: `Double.tryParse "1,234.5"` is now
  `None` where it used to be `Some 1234.5` on an English locale. This is the deliberate half of the
  change above — under the invariant culture the comma *is* the group separator, so permitting it read
  `"1,5"` as `15.0`, ten times the intended value and silently. A rejected parse the caller can see
  beats a plausible wrong number
- `Core/Options.fs` now compiles ahead of `Core/Char.fs` instead of after `Core/String.fs`, so the
  `Option`/`ValueOption` TryXxx adapters are in scope for every parser that builds on them. It depends
  on nothing but `Core/Operators.fs`

### Not added, and why
- `DateOnly`, `TimeOnly`, `Int128`, `UInt128` and `Half` have no parser: the package targets
  `netstandard2.1` and none of those types exist there — verified by compiling each against the
  netstandard2.1 reference assemblies, which reports all five as undefined. `netstandard` has no
  version above 2.1, so the only way to reach them is to target .NET directly, which this package
  deliberately does not do

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
