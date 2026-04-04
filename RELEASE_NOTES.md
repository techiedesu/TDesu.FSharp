## 1.1.0

- **Breaking**: Removed `[<AutoOpen>]` from all modules — explicit namespace imports required
- New namespace layout: `TDesu.FSharp.Tasks`, `.Collections`, `.IO`, `.Concurrency`, `.Resilience`, `.Hashing`, `.Buffers`, `.Builders`
- Core modules (`String`, `Option`, `Result`, `Guard`, numeric parsing) remain under `TDesu.FSharp`
- Operators (`^`, `%`, `konst`, `isNotNull`, etc.) moved to `TDesu.FSharp.Operators`
- Merged 3 builder files into single `TDesu.FSharp.Builders` module
- Moved `(|Null|_|)` active pattern into `ActivePatterns.Ref`
- Removed duplicate `unitTask` (use `Task.asUnit`)
- License changed from MIT to Unlicense

## 1.0.0

- Initial release as standalone package
- Operators: `^`, `%`, `konst`
- String, Option, Result, Task, TaskResult modules
- ResizeArray functional wrappers
- Seq safe aggregation (`tryMax`, `tryMin`, `tryAverage`)
- Dictionary helpers
- Parse active patterns (Int, Double, Bool, Guid, DateTimeOffset)
- tryParse extensions for all numeric types
- Guard module for argument validation
- Disposable helpers (`create`, `combine`, `deferStack`, `tempFile`)
- NonEmptyString value type
- BoundedDict / BoundedSet collections
- Bytes utilities (XOR, concat, constant-time comparison)
- ArrayPool helpers
- ContentHash (SHA256, MD5)
- TemporaryFileStream
- Resilience module (retry with backoff)
- Computation expressions: `result {}`, `maybe {}`, `taskResult {}`
- Fable compatibility (source-included NuGet)
