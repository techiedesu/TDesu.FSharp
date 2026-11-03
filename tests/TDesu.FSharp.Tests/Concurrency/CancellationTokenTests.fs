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
        use linkedCts = CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token
        isFalse linkedCts.Token.IsCancellationRequested
        parentCts.Cancel()
        isTrue linkedCts.Token.IsCancellationRequested

    [<Test>]
    member _.``linked cancels on timeout``() =
        task {
            use parentCts = new CancellationTokenSource()

            use linkedCts =
                CancellationToken.linked (TimeSpan.FromMilliseconds 50.) parentCts.Token

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
    member _.``linked returns a single CancellationTokenSource usable with a bare use``() =
        // ARRANGE
        use parentCts = new CancellationTokenSource()

        // ACT — the type annotation alone pins the API shape: a tuple would fail to type-check here.
        let linkedCts: CancellationTokenSource =
            CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token

        // ASSERT
        isFalse linkedCts.Token.IsCancellationRequested
        linkedCts.Dispose()
        %Assert.Throws<ObjectDisposedException>(fun () -> linkedCts.Token |> ignore)

    [<Test>]
    member _.``linked can be disposed twice without throwing``() =
        // ARRANGE
        use parentCts = new CancellationTokenSource()
        let cts = CancellationToken.linked (TimeSpan.FromSeconds 30.) parentCts.Token

        // ACT
        cts.Dispose()
        cts.Dispose()

        // ASSERT
        Assert.Pass()
