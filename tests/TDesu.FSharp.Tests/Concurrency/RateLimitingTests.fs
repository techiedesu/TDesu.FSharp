namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp.Concurrency

[<TestFixture>]
type SlidingWindowLimiterTests() =

    [<Test>]
    member _.``allows requests under limit``() =
        let limiter = SlidingWindowLimiter(3, TimeSpan.FromSeconds 60.)

        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())

        equals limiter.Count 3

    [<Test>]
    member _.``rejects requests over limit``() =
        let limiter = SlidingWindowLimiter(2, TimeSpan.FromSeconds 60.)

        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())

        let result = limiter.TryAcquire()
        isError result

        match result with
        | Error waitTime -> isTrue (waitTime.Ticks > 0L)
        | _ -> Assert.Fail("Expected Error")

    [<Test>]
    member _.``window slides after expiry``() =
        let limiter = SlidingWindowLimiter(1, TimeSpan.FromMilliseconds 50.)

        isOk () (limiter.TryAcquire())
        isError (limiter.TryAcquire())

        Threading.Thread.Sleep(80)

        isOk () (limiter.TryAcquire())

    [<Test>]
    member _.``reset clears state``() =
        let limiter = SlidingWindowLimiter(2, TimeSpan.FromSeconds 60.)

        isOk () (limiter.TryAcquire())
        isOk () (limiter.TryAcquire())
        isError (limiter.TryAcquire())

        limiter.Reset()
        equals limiter.Count 0

        isOk () (limiter.TryAcquire())

    [<Test>]
    member _.``properties are accessible``() =
        let limiter = SlidingWindowLimiter(10, TimeSpan.FromMinutes 1.)
        equals limiter.MaxRequests 10
        equals limiter.Window (TimeSpan.FromMinutes 1.)

    [<Test>]
    member _.``rejects zero maxRequests``() =
        Assert.Throws<ArgumentException>(fun () ->
            SlidingWindowLimiter(0, TimeSpan.FromSeconds 1.) |> ignore)
        |> ignore

    [<Test>]
    member _.``rejects negative maxRequests``() =
        Assert.Throws<ArgumentException>(fun () ->
            SlidingWindowLimiter(-1, TimeSpan.FromSeconds 1.) |> ignore)
        |> ignore

    [<Test>]
    member _.``rejects zero window``() =
        Assert.Throws<ArgumentException>(fun () ->
            SlidingWindowLimiter(10, TimeSpan.Zero) |> ignore)
        |> ignore

    [<Test>]
    member _.``rejects negative window``() =
        Assert.Throws<ArgumentException>(fun () ->
            SlidingWindowLimiter(10, TimeSpan.FromSeconds -1.) |> ignore)
        |> ignore

    [<Test>]
    member _.``single request allowed with maxRequests 1``() =
        let limiter = SlidingWindowLimiter(1, TimeSpan.FromSeconds 60.)
        isOk () (limiter.TryAcquire())
        isError (limiter.TryAcquire())
