namespace TDesu.FSharp.Tests

open System
open System.IO
open System.Threading
open NUnit.Framework
open TDesu.FSharp.IO

[<TestFixture>]
type TemporaryFileStreamTests() =

    [<Test>]
    member _.``the backing file exists on disk while the stream is open``() =
        // ARRANGE
        use stream = new TemporaryFileStream()

        // ACT / ASSERT
        isTrue (File.Exists stream.FileName)

    [<Test>]
    member _.``the backing file is deleted after Dispose``() =
        // ARRANGE
        let stream = new TemporaryFileStream()
        let path = stream.FileName

        // ACT
        stream.Dispose()

        // ASSERT
        isFalse (File.Exists path)

    [<Test>]
    member _.``disposing twice does not throw``() =
        // ARRANGE
        let stream = new TemporaryFileStream()

        // ACT
        stream.Dispose()
        stream.Dispose()

        // ASSERT
        Assert.Pass()

    [<Test>]
    member _.``an explicit tempFileName is used as the backing file path``() =
        // ARRANGE
        let dir, cleanupDir = Disposable.tempDir ()

        try
            let customPath = Path.Combine(dir, "custom.tmp")

            // ACT
            use stream = new TemporaryFileStream(customPath)

            // ASSERT
            equals stream.FileName customPath
            isTrue (File.Exists customPath)
        finally
            cleanupDir.Dispose()

    [<Test>]
    member _.``doNotDeleteFileAfterDispose keeps the backing file after Dispose``() =
        // ARRANGE
        let stream = new TemporaryFileStream(doNotDeleteFileAfterDispose = true)
        let path = stream.FileName

        try
            // ACT
            stream.Dispose()

            // ASSERT
            isTrue (File.Exists path)
        finally
            File.Delete path

    [<Test>]
    member _.``an undisposed stream's backing file is still deleted once its handle is finalized``() =
        // ARRANGE: created and abandoned on its own thread so its frame is fully unwound (not just
        // logically out of scope) by the time that thread finishes — the test runner's own
        // reflection-invoke frame can otherwise keep a quick-JITted local conservatively reachable
        // for the rest of the test method.
        let path =
            let mutable captured = Unchecked.defaultof<string>

            let t =
                Thread(fun () ->
                    let stream = new TemporaryFileStream()
                    captured <- stream.FileName
                )

            t.Start()
            t.Join()
            captured

        // ACT: FileOptions.DeleteOnClose means the OS deletes the file the instant the handle
        // closes, through the finalizer that runs when nothing disposed the stream explicitly.
        for _ in 1..5 do
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()

        // ASSERT
        isFalse (File.Exists path)

    [<Test>]
    member _.``Read/Write via Span and ReadOnlySpan round-trip through the backing file``() =
        // ARRANGE
        use stream = new TemporaryFileStream()
        let payload = [| 1uy; 2uy; 3uy; 4uy; 5uy |]

        // ACT
        stream.Write(ReadOnlySpan<byte> payload)
        stream.Position <- 0L
        let buffer = Array.zeroCreate<byte> 5
        let read = stream.Read(Span<byte> buffer)

        // ASSERT
        equals read 5
        equals buffer payload

    [<Test>]
    member _.``ReadAsync/WriteAsync via Memory round-trip through the backing file``() =
        // ARRANGE
        use stream = new TemporaryFileStream()
        let payload = [| 9uy; 8uy; 7uy |]

        // ACT
        stream.WriteAsync(ReadOnlyMemory<byte> payload).AsTask().GetAwaiter().GetResult()
        stream.Position <- 0L
        let buffer = Array.zeroCreate<byte> 3
        let read = stream.ReadAsync(Memory<byte> buffer).AsTask().GetAwaiter().GetResult()

        // ASSERT
        equals read 3
        equals buffer payload

    [<Test>]
    member _.``DisposeAsync deletes the backing file``() =
        // ARRANGE
        let stream = new TemporaryFileStream()
        let path = stream.FileName

        // ACT
        stream.DisposeAsync().AsTask().GetAwaiter().GetResult()

        // ASSERT
        isFalse (File.Exists path)
