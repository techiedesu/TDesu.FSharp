namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.IO

[<TestFixture>]
type FileDeleteTests() =

    [<Test>]
    member _.``delete removes an existing file and returns Ok``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "victim.txt")
            System.IO.File.WriteAllText(path, "data")

            // ACT
            let result = File.delete path

            // ASSERT
            isOk () result
            isFalse (System.IO.File.Exists path)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``delete on a missing file is a no-op and returns Ok``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "never-existed.txt")

            // ACT
            let result = File.delete path

            // ASSERT
            isOk () result
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``delete with a null path returns Error wrapping ArgumentNullException``() =
        // ACT
        let result = File.delete null

        // ASSERT
        match result with
        | Error e -> isTrue (e :? ArgumentNullException)
        | Ok() -> Assert.Fail("expected Error")

    [<Test>]
    member _.``delete with an empty path returns Error wrapping ArgumentException``() =
        // ACT
        let result = File.delete ""

        // ASSERT
        match result with
        | Error e -> isTrue (e :? ArgumentException)
        | Ok() -> Assert.Fail("expected Error")

    [<Test>]
    member _.``deleteIgnore swallows errors from an invalid path without throwing``() =
        // ACT
        File.deleteIgnore null

        // ASSERT
        Assert.Pass()

    [<Test>]
    member _.``deleteIgnore removes an existing file``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "victim.txt")
            System.IO.File.WriteAllText(path, "data")

            // ACT
            File.deleteIgnore path

            // ASSERT
            isFalse (System.IO.File.Exists path)
        finally
            cleanup.Dispose()

[<TestFixture>]
type FileNotExistsTests() =

    [<Test>]
    member _.``notExists is true for a missing file``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "missing.txt")

            // ACT / ASSERT
            isTrue (File.notExists path)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``notExists is false for an existing file``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "present.txt")
            System.IO.File.WriteAllText(path, "data")

            // ACT / ASSERT
            isFalse (File.notExists path)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``notExists is true for a null path``() =
        // ACT / ASSERT
        isTrue (File.notExists null)

[<TestFixture>]
type DirectoryTests() =

    [<Test>]
    member _.``notExists is true for a missing directory``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "missing-subdir")

            // ACT / ASSERT
            isTrue (Directory.notExists path)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``notExists is false for an existing directory``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            // ACT / ASSERT
            isFalse (Directory.notExists dir)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``create makes a new directory``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "child")

            // ACT
            Directory.create path

            // ASSERT
            isTrue (System.IO.Directory.Exists path)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``create makes all missing parent directories``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            let path = System.IO.Path.Combine(dir, "a", "b", "c")

            // ACT
            Directory.create path

            // ASSERT
            isTrue (System.IO.Directory.Exists path)
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``create on an already-existing directory does not throw``() =
        // ARRANGE
        let dir, cleanup = Disposable.tempDir ()

        try
            // ACT
            Directory.create dir

            // ASSERT
            Assert.Pass()
        finally
            cleanup.Dispose()

    [<Test>]
    member _.``create with a null path propagates ArgumentNullException raw, unlike File.delete``() =
        // ACT / ASSERT
        %Assert.Throws<ArgumentNullException>(fun () -> Directory.create null)

[<TestFixture>]
type PathRandomFileNameTests() =

    [<Test>]
    member _.``getRandomFileName produces a 32-character lowercase hex string``() =
        // ACT
        let name = Path.getRandomFileName ()

        // ASSERT
        equals name.Length 32
        isTrue (name |> Seq.forall (fun c -> (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))

    [<Test>]
    member _.``getRandomFileName produces distinct values across calls``() =
        // ACT
        let a = Path.getRandomFileName ()
        let b = Path.getRandomFileName ()

        // ASSERT
        notEquals a b

    [<Test>]
    member _.``RandomFileName static property produces a 32-character lowercase hex string``() =
        // ACT
        let name = Path.RandomFileName

        // ASSERT
        equals name.Length 32
        isTrue (name |> Seq.forall (fun c -> (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))

    [<Test>]
    member _.``RandomFileName produces a distinct value on each read``() =
        // ACT
        let a = Path.RandomFileName
        let b = Path.RandomFileName

        // ASSERT
        notEquals a b
