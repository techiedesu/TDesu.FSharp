namespace TDesu.FSharp.Tests

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.IO
open TDesu.FSharp.Tasks

/// A disposable that records how many times it was disposed, and optionally throws.
type private Spy(name: string, throwOnDispose: bool) =
    let mutable count = 0
    member _.Name = name
    member _.DisposeCount = count

    interface IDisposable with
        member _.Dispose() =
            count <- count + 1
            if throwOnDispose then
                raise (InvalidOperationException($"dispose failed: {name}"))

[<TestFixture>]
type DisposableCombineTests() =

    [<Test>]
    member _.``a throwing disposable does not stop the others from being disposed``() =
        // ARRANGE
        let first = new Spy("first", throwOnDispose = true)
        let second = new Spy("second", throwOnDispose = false)
        let third = new Spy("third", throwOnDispose = false)
        let combined = Disposable.combine [ first; second; third ]

        // ACT
        % Assert.Throws<InvalidOperationException>(fun () -> combined.Dispose())

        // ASSERT
        equals first.DisposeCount 1
        equals second.DisposeCount 1
        equals third.DisposeCount 1

    [<Test>]
    member _.``two throwing disposables surface as an AggregateException``() =
        // ARRANGE
        let first = new Spy("first", throwOnDispose = true)
        let second = new Spy("second", throwOnDispose = true)
        let combined = Disposable.combine [ first; second ]

        // ACT
        let ex = Assert.Throws<AggregateException>(fun () -> combined.Dispose())

        // ASSERT
        equals ex.InnerExceptions.Count 2
        equals first.DisposeCount 1
        equals second.DisposeCount 1

    [<Test>]
    member _.``an empty list disposes without throwing``() =
        // ARRANGE
        let combined = Disposable.combine []

        // ACT
        combined.Dispose()

        // ASSERT
        Assert.Pass()

    [<Test>]
    member _.``null entries in the list are skipped``() =
        // ARRANGE
        let real = new Spy("real", throwOnDispose = false)
        let combined = Disposable.combine [ null; real; null ]

        // ACT
        combined.Dispose()

        // ASSERT
        equals real.DisposeCount 1

    [<Test>]
    member _.``a null list is treated as empty``() =
        // ARRANGE
        let nullList = Unchecked.defaultof<IDisposable list>
        let combined = Disposable.combine nullList

        // ACT
        combined.Dispose()

        // ASSERT
        Assert.Pass()

    [<Test>]
    member _.``disposing twice disposes each child exactly once``() =
        // ARRANGE
        let child = new Spy("child", throwOnDispose = false)
        let combined = Disposable.combine [ child ]

        // ACT
        combined.Dispose()
        combined.Dispose()

        // ASSERT
        equals child.DisposeCount 1

type private CountingStream(payload: byte[]) =
    inherit MemoryStream(payload)
    let mutable disposeCount = 0
    member _.DisposeCount = disposeCount
    override _.Dispose(disposing) =
        disposeCount <- disposeCount + 1
        base.Dispose(disposing)

[<TestFixture>]
type StreamCopyUpToTests() =

    [<Test>]
    member _.``a payload exactly at the cap is copied successfully``() =
        // ARRANGE
        let payload = Array.create 100 1uy
        use source = new MemoryStream(payload)
        use destination = new MemoryStream()

        // ACT
        let result = source |> Stream.copyUpTo 100L destination CancellationToken.None |> Task.getResult

        // ASSERT
        match result with
        | Ok written -> equals written 100L
        | Error e -> Assert.Fail($"expected Ok, got Error {e}")

    [<Test>]
    member _.``a payload one byte over the cap is rejected``() =
        // ARRANGE
        let payload = Array.create 101 1uy
        use source = new MemoryStream(payload)
        use destination = new MemoryStream()

        // ACT
        let result = source |> Stream.copyUpTo 100L destination CancellationToken.None |> Task.getResult

        // ASSERT
        match result with
        | Ok written -> Assert.Fail($"expected Error, got Ok {written}")
        | Error e -> equals e.MaxBytes 100L

    [<Test>]
    member _.``a zero cap rejects any non-empty payload``() =
        // ARRANGE
        use source = new MemoryStream([| 1uy |])
        use destination = new MemoryStream()

        // ACT
        let result = source |> Stream.copyUpTo 0L destination CancellationToken.None |> Task.getResult

        // ASSERT
        isTrue (match result with Error _ -> true | Ok _ -> false)

    [<Test>]
    member _.``an empty source succeeds with zero bytes written``() =
        // ARRANGE
        use source = new MemoryStream([||])
        use destination = new MemoryStream()

        // ACT
        let result = source |> Stream.copyUpTo 100L destination CancellationToken.None |> Task.getResult

        // ASSERT
        match result with
        | Ok written -> equals written 0L
        | Error e -> Assert.Fail($"expected Ok, got Error {e}")

    [<Test>]
    member _.``a null source throws ArgumentNullException, not NullReferenceException``() =
        // ARRANGE
        use destination = new MemoryStream()

        // ACT
        let act () = (null: Stream) |> Stream.copyUpTo 100L destination CancellationToken.None |> ignore

        // ASSERT
        % Assert.Throws<ArgumentNullException>(fun () -> act ())

    [<Test>]
    member _.``a null destination throws ArgumentNullException, not NullReferenceException``() =
        // ARRANGE
        use source = new MemoryStream([| 1uy |])

        // ACT
        let act () = source |> Stream.copyUpTo 100L (null: Stream) CancellationToken.None |> ignore

        // ASSERT
        % Assert.Throws<ArgumentNullException>(fun () -> act ())

    [<Test>]
    member _.``cancellation surfaces and leaves the caller's streams open``() =
        // ARRANGE
        let payload = Array.create 1_000_000 1uy
        use source = new CountingStream(payload)
        use destination = new MemoryStream()
        use cts = new CancellationTokenSource()
        cts.Cancel()

        // ACT
        let act () = source |> Stream.copyUpTo 10_000_000L destination cts.Token |> Task.getResult |> ignore

        // ASSERT
        % Assert.Throws<TaskCanceledException>(fun () -> act ())
        equals source.DisposeCount 0

    [<Test>]
    member _.``the caller's streams are never disposed by a successful copy``() =
        // ARRANGE
        use source = new CountingStream(Array.create 10 1uy)
        use destination = new MemoryStream()

        // ACT
        let result = source |> Stream.copyUpTo 100L destination CancellationToken.None |> Task.getResult

        // ASSERT
        isTrue (match result with Ok _ -> true | Error _ -> false)
        equals source.DisposeCount 0
