namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp

// ── Test domain ──

[<Flags>]
type private Permissions =
    | None = 0
    | Read = 1
    | Write = 2
    | Execute = 4

[<TestFixture>]
type NumericTests() =

    [<Test>]
    member _.``clamp returns the value when inside the range``() = equals (Numeric.clamp 0 10 4) 4

    [<Test>]
    member _.``clamp returns lo when the value is below the range``() = equals (Numeric.clamp 0 10 -3) 0

    [<Test>]
    member _.``clamp returns hi when the value is above the range``() = equals (Numeric.clamp 0 10 15) 10

    [<Test>]
    member _.``clamp with lo greater than hi always returns lo or hi``() =
        equals (Numeric.clamp 10 1 5) 10
        equals (Numeric.clamp 10 1 -5) 10
        equals (Numeric.clamp 10 1 100) 1

    [<Test>]
    member _.``lerp at t=0 returns a``() = equals (Numeric.lerp 0.0 10.0 0.0) 0.0

    [<Test>]
    member _.``lerp at t=1 returns b``() = equals (Numeric.lerp 0.0 10.0 1.0) 10.0

    [<Test>]
    member _.``lerp at t=0.5 returns the midpoint``() = equals (Numeric.lerp 0.0 10.0 0.5) 5.0

    [<Test>]
    member _.``lerp outside the unit interval extrapolates instead of clamping``() =
        equals (Numeric.lerp 0.0 10.0 2.0) 20.0
        equals (Numeric.lerp 0.0 10.0 -1.0) -10.0

    [<Test>]
    member _.``inverseLerp recovers t for an interior value``() =
        equals (Numeric.inverseLerp 0.0 10.0 5.0) 0.5

    [<Test>]
    member _.``inverseLerp for a value past b returns t past 1``() =
        equals (Numeric.inverseLerp 0.0 10.0 15.0) 1.5

    [<Test>]
    member _.``inverseLerp with equal endpoints returns NaN instead of throwing``() =
        isTrue (Double.IsNaN(Numeric.inverseLerp 5.0 5.0 5.0))

    [<Test>]
    member _.``isBetween includes both boundaries``() =
        isTrue (Numeric.isBetween 0 10 0)
        isTrue (Numeric.isBetween 0 10 10)

    [<Test>]
    member _.``isBetween excludes values outside the range``() =
        isFalse (Numeric.isBetween 0 10 -1)
        isFalse (Numeric.isBetween 0 10 11)

    [<Test>]
    member _.``zero and one are the additive and multiplicative identities``() =
        equals Numeric.zero<int> 0
        equals Numeric.one<int> 1
        equals Numeric.zero<float> 0.0
        equals Numeric.one<float> 1.0

[<TestFixture>]
type EnumTests() =

    [<Test>]
    member _.``hasFlag is true when every bit of the flag is set``() =
        isTrue (Enum.hasFlag Permissions.Read (Permissions.Read ||| Permissions.Write))

    [<Test>]
    member _.``hasFlag is false when a bit of the flag is missing``() =
        isFalse (Enum.hasFlag Permissions.Execute (Permissions.Read ||| Permissions.Write))

    [<Test>]
    member _.``addFlag sets the flag``() =
        equals (Enum.addFlag Permissions.Write Permissions.Read) (Permissions.Read ||| Permissions.Write)

    [<Test>]
    member _.``addFlagWhen true adds the flag``() =
        equals (Enum.addFlagWhen true Permissions.Write Permissions.Read) (Permissions.Read ||| Permissions.Write)

    [<Test>]
    member _.``addFlagWhen false leaves the value unchanged``() =
        equals (Enum.addFlagWhen false Permissions.Write Permissions.Read) Permissions.Read

    [<Test>]
    member _.``removeFlag clears the flag``() =
        equals (Enum.removeFlag Permissions.Write (Permissions.Read ||| Permissions.Write)) Permissions.Read

    [<Test>]
    member _.``removeFlagWhen true removes the flag``() =
        equals (Enum.removeFlagWhen true Permissions.Write (Permissions.Read ||| Permissions.Write)) Permissions.Read

    [<Test>]
    member _.``removeFlagWhen false leaves the value unchanged``() =
        let combined = Permissions.Read ||| Permissions.Write
        equals (Enum.removeFlagWhen false Permissions.Write combined) combined
