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
