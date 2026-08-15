// TDesu.FSharp.IO.Stream: a bounded stream copy that turns "the source produced more bytes
// than allowed" into a Result instead of an unbounded read into memory -- the shape you want
// for request bodies and other untrusted sources where a byte cap is a real limit, not a
// suggestion. Uses MemoryStream throughout; no real files are touched.
#load "_prelude.fsx"
open Prelude
open System.Text
open System.Threading
open TDesu.FSharp.IO
open TDesu.FSharp.Tasks

// ── copyUpTo: the source fits comfortably under the cap -- Ok with the byte count ────────
let copyWithinCap () =
    let payload = Encoding.UTF8.GetBytes "hello world"
    use source = new System.IO.MemoryStream(payload)
    use destination = new System.IO.MemoryStream()
    let result = source |> Stream.copyUpTo 1024L destination CancellationToken.None |> Task.getResult
    result, destination.ToArray() |> Encoding.UTF8.GetString

let okResult, copiedText = copyWithinCap ()
assertEqual "Stream.copyUpTo returns Ok with the number of bytes copied" (Ok 11L) okResult
assertEqual "Stream.copyUpTo actually wrote every byte through to the destination" "hello world" copiedText

// ── copyUpTo: the source exceeds the cap -- Error, not a thrown exception or a truncated
// write. The chunk that would push the total over the limit is discarded rather than
// partially written, so BytesWritten never counts a chunk that didn't make it across.
let copyOverCap () =
    let payload = Encoding.UTF8.GetBytes "this payload is way too long for an 8-byte cap"
    use source = new System.IO.MemoryStream(payload)
    use destination = new System.IO.MemoryStream()
    source |> Stream.copyUpTo 8L destination CancellationToken.None |> Task.getResult

let expectedExceeded: Stream.MaxBytesExceeded = { MaxBytes = 8L; BytesWritten = 0L }
assertEqual "Stream.copyUpTo reports Error once the source would exceed the cap"
    (Error expectedExceeded)
    (copyOverCap ())

printfn "06-io.fsx: all assertions passed"
