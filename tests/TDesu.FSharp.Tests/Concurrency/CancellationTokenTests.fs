namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.Concurrency

[<TestFixture>]
type CancellationTokenTests() =

    [<Test>]
    member _.``linked cancels when parent cancels``() =
        use parentCts = new CancellationTokenSource()

        let linkedCts, cleanup =
            CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token

        use _ = cleanup
        isFalse linkedCts.Token.IsCancellationRequested
        parentCts.Cancel()
        isTrue linkedCts.Token.IsCancellationRequested

    [<Test>]
    member _.``linked cancels on timeout``() =
        task {
            use parentCts = new CancellationTokenSource()

            let linkedCts, cleanup =
                CancellationToken.linked (TimeSpan.FromMilliseconds 50.) parentCts.Token

            use _ = cleanup

            let! expired =
                Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> linkedCts.Token.IsCancellationRequested)

            isTrue expired
        }

    [<Test>]
    member _.``withTimeout does not cancel before the timeout elapses``() =
        // ARRANGE
        use cts = CancellationToken.withTimeout (TimeSpan.FromSeconds 30.)

        // ACT / ASSERT
        isFalse cts.Token.IsCancellationRequested

    [<Test>]
    member _.``withTimeout cancels after the timeout elapses``() =
        task {
            // ARRANGE
            use cts = CancellationToken.withTimeout (TimeSpan.FromMilliseconds 50.)

            // ACT
            let! expired =
                Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> cts.Token.IsCancellationRequested)

            // ASSERT
            isTrue expired
        }

    [<Test>]
    member _.``linked cleanup disposes the returned token source``() =
        // ARRANGE
        use parentCts = new CancellationTokenSource()

        let linkedCts, cleanup =
            CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token

        // ACT
        cleanup.Dispose()

        // ASSERT
        %Assert.Throws<ObjectDisposedException>(fun () -> linkedCts.Token |> ignore)

    [<Test>]
    member _.``linked cleanup can be disposed twice without throwing``() =
        // ARRANGE
        use parentCts = new CancellationTokenSource()
        let _, cleanup = CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token

        // ACT
        cleanup.Dispose()
        cleanup.Dispose()

        // ASSERT
        Assert.Pass()
