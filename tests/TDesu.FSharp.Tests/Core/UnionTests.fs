namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators

// ── Test domain ──

type WeatherReport =
    | Sunny
    | Cloudy
    | Rainy of millimeters: float

type Coordinates = { X: int; Y: int }

[<Struct>]
type StructWeather =
    | ClearSkies
    | Overcast

[<TestFixture>]
type UnionTests() =

    [<Test>]
    member _.``caseName returns the case a value carrying a field was built with``() =
        equals (Union.caseName (Rainy 4.5)) "Rainy"

    [<Test>]
    member _.``caseName returns the case a fieldless value was built with``() =
        equals (Union.caseName Sunny) "Sunny"
        equals (Union.caseName Cloudy) "Cloudy"

    [<Test>]
    member _.``caseNames lists every case in declaration order``() =
        equals (Union.caseNames<WeatherReport> ()) [| "Sunny"; "Cloudy"; "Rainy" |]

    [<Test>]
    member _.``caseName raises ArgumentException, not a wrapped reflection failure, for a non-union type``() =
        let act () =
            Union.caseName { X = 1; Y = 2 } |> ignore

        %Assert.Throws<ArgumentException>(act)
        // A second call must not surface a poisoned-static-initializer TypeInitializationException.
        %Assert.Throws<ArgumentException>(act)

    [<Test>]
    member _.``caseNames raises ArgumentException for a non-union type``() =
        let act () =
            Union.caseNames<Coordinates> () |> ignore

        %Assert.Throws<ArgumentException>(act)
        %Assert.Throws<ArgumentException>(act)

    [<Test>]
    member _.``caseNames returns a fresh array each call, so mutating one does not poison caseName or later caseNames calls``
        ()
        =
        let first = Union.caseNames<WeatherReport> ()
        first[0] <- "PWNED"

        equals (Union.caseNames<WeatherReport> ()) [| "Sunny"; "Cloudy"; "Rainy" |]
        equals (Union.caseName Sunny) "Sunny"

    [<Test>]
    member _.``caseName resolves a struct union's case correctly``() =
        equals (Union.caseName ClearSkies) "ClearSkies"
        equals (Union.caseName Overcast) "Overcast"

    [<Test>]
    member _.``caseName does not box a struct union value``() =
        // Warm the per-'T cache (reflection, expression-tree compilation) before measuring.
        Union.caseName Overcast |> ignore

        let before = GC.GetAllocatedBytesForCurrentThread()

        for _ in 1..1000 do
            Union.caseName Overcast |> ignore

        let after = GC.GetAllocatedBytesForCurrentThread()
        equals (after - before) 0L
