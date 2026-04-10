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
