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
