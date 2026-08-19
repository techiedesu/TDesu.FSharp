namespace TDesu.FSharp.Tests

open System.IO
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
