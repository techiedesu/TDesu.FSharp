// Numeric: generic clamp/lerp/isBetween over anything with the right operators, not
// just int. Enum: bitwise flag helpers over any [<Flags>] enum.
#load "_prelude.fsx"

open Prelude
open System
open TDesu.FSharp

// ── Numeric ────────────────────────────────────────────────────────────
assertEqual "Numeric.clamp clamps above hi" 10 (Numeric.clamp 0 10 15)
assertEqual "Numeric.clamp clamps below lo" 0 (Numeric.clamp 0 10 -3)
assertEqual "Numeric.clamp passes values inside the range through" 4 (Numeric.clamp 0 10 4)

assertEqual "Numeric.lerp at t=0 is a" 0.0 (Numeric.lerp 0.0 10.0 0.0)
assertEqual "Numeric.lerp at t=1 is b" 10.0 (Numeric.lerp 0.0 10.0 1.0)
assertEqual "Numeric.lerp at t=0.5 is the midpoint" 5.0 (Numeric.lerp 0.0 10.0 0.5)
assertEqual "Numeric.lerp is unclamped -- t>1 extrapolates past b" 20.0 (Numeric.lerp 0.0 10.0 2.0)

assertEqual
    "Numeric.inverseLerp finds the fraction t for a value between a and b"
    0.5
    (Numeric.inverseLerp 0.0 10.0 5.0)

assertEqual "Numeric.inverseLerp is unclamped -- t>1 when the value is past b" 1.5 (Numeric.inverseLerp 0.0 10.0 15.0)

assertTrue "Numeric.isBetween is inclusive on the low end" (Numeric.isBetween 0 10 0)
assertTrue "Numeric.isBetween is inclusive on the high end" (Numeric.isBetween 0 10 10)
assertTrue "Numeric.isBetween is false just outside the range" (not (Numeric.isBetween 0 10 11))

// zero/one: the additive/multiplicative identities, generic over any numeric type via
// GenericZero/GenericOne -- explicit type application since there is no argument to
// infer the type from.
assertEqual "Numeric.zero is the additive identity" 0 Numeric.zero<int>
assertEqual "Numeric.one is the multiplicative identity" 1.0 Numeric.one<float>

// ── Enum: generic over any [<Flags>] enum via the EnumShape<'enum> constraint ────
[<Flags>]
type Permissions =
    | None = 0
    | Read = 1
    | Write = 2
    | Execute = 4

let readWrite = Permissions.Read ||| Permissions.Write

assertTrue "Enum.hasFlag finds a flag that is set" (Enum.hasFlag Permissions.Read readWrite)
assertTrue "Enum.hasFlag rejects a flag that is not set" (not (Enum.hasFlag Permissions.Execute readWrite))

assertEqual
    "Enum.addFlag ors the flag in"
    (Permissions.Read ||| Permissions.Write ||| Permissions.Execute)
    (Enum.addFlag Permissions.Execute readWrite)

assertEqual "Enum.removeFlag clears just that flag" Permissions.Read (Enum.removeFlag Permissions.Write readWrite)

assertEqual
    "Enum.addFlagWhen true behaves like addFlag"
    readWrite
    (Enum.addFlagWhen true Permissions.Write Permissions.Read)

assertEqual
    "Enum.addFlagWhen false leaves the value unchanged"
    Permissions.Read
    (Enum.addFlagWhen false Permissions.Write Permissions.Read)

assertEqual
    "Enum.removeFlagWhen false leaves the value unchanged"
    readWrite
    (Enum.removeFlagWhen false Permissions.Write readWrite)

printfn "03-numerics.fsx: all assertions passed"
