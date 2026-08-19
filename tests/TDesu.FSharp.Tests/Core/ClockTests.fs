namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp

[<TestFixture>]
type ClockTests() =

    [<Test>]
    member _.``SystemClock returns current time``() =
        let before = DateTimeOffset.UtcNow
        let clockNow = SystemClock.Instance.UtcNow
        let after = DateTimeOffset.UtcNow
        isTrue (clockNow >= before && clockNow <= after)

    [<Test>]
    member _.``SystemClock singleton is same instance``() =
        let a = SystemClock.Instance
        let b = SystemClock.Instance
        isTrue (obj.ReferenceEquals(a, b))

    [<Test>]
    member _.``FakeClock starts at given time``() =
        let t = DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero)
        let clock = FakeClock(t)
        equals (clock :> IClock).UtcNow t

    [<Test>]
    member _.``FakeClock Advance moves time forward``() =
        let t = DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        let clock = FakeClock(t)
        clock.Advance(TimeSpan.FromHours 2.)
        equals clock.Current (t + TimeSpan.FromHours 2.)

    [<Test>]
    member _.``FakeClock Set replaces time``() =
        let clock = FakeClock(DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero))
        let newTime = DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero)
        clock.Set(newTime)
        equals (clock :> IClock).UtcNow newTime

    [<Test>]
    member _.``FakeClock works as IClock``() =
        let t = DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero)
        let clock: IClock = FakeClock(t)
        equals clock.UtcNow t
