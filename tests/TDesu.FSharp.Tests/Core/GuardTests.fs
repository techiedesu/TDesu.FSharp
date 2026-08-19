namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators

[<TestFixture>]
type GuardTests() =

    [<Test>]
    member _.``notNull throws for null``() =
        %Assert.Throws<ArgumentNullException>(fun () -> Guard.notNull "x" (null: string))

    [<Test>]
    member _.``inRange throws for out-of-range``() =
        %Assert.Throws<ArgumentOutOfRangeException>(fun () -> Guard.inRange "x" 1 10 15)
        %Assert.Throws<ArgumentOutOfRangeException>(fun () -> Guard.inRange "x" 1 10 0)

    [<Test>]
    member _.``positive throws for zero and negative``() =
        %Assert.Throws<ArgumentOutOfRangeException>(fun () -> Guard.positive "x" 0)
        %Assert.Throws<ArgumentOutOfRangeException>(fun () -> Guard.positive "x" -1)
