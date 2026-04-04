module Domain

open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Types
open TDesu.FSharp.ActivePatterns
open TDesu.FSharp.Builders

/// Demonstrates: [<RequireQualifiedAccess>] on error DUs
[<RequireQualifiedAccess>]
type ValidationError =
    | CityRequired
    | CityTooLong
    | InvalidCurrency of string
    | InvalidLatitude of string
    | InvalidLongitude of string

[<RequireQualifiedAccess>]
type ServiceError =
    | Validation of ValidationError
    | ProviderUnavailable of provider: string * message: string
    | Timeout of provider: string
    | NotFound of what: string

/// Demonstrates: simple F# records for domain models
type GeoLocation = {
    City: string
    Country: string
    Latitude: float
    Longitude: float
    Timezone: string
}

type WeatherInfo = {
    Temperature: float
    Humidity: float
    WindSpeed: float
    WeatherCode: int
}

type ExchangeRates = {
    Base: string
    Rates: Map<string, float>
}

type TimezoneInfo = {
    Timezone: string
    DateTime: string
    UtcOffset: string
}

type CityReport = {
    Location: GeoLocation
    Weather: WeatherInfo option
    Rates: ExchangeRates option
    Time: TimezoneInfo option
    GeneratedAt: int64
}

/// Demonstrates: result {}, NonEmptyString, String.toOption, Result.requireTrue
module Validate =

    let city (raw: string) : Result<NonEmptyString, ValidationError> =
        result {
            let! trimmed =
                raw
                |> String.toOption
                |> Option.toResult ValidationError.CityRequired

            do! Result.requireTrue ValidationError.CityTooLong (trimmed.Length <= 100)
            return NonEmptyString.createOrFail trimmed
        }

    /// Demonstrates: String.toUpperInv, Parse active patterns
    let currency (raw: string) : Result<string, ValidationError> =
        result {
            let! s =
                raw
                |> String.toOption
                |> Option.toResult ^ ValidationError.InvalidCurrency "empty"

            let upper = String.toUpperInv s

            do! Result.requireTrue
                    (ValidationError.InvalidCurrency upper)
                    (upper.Length = 3)

            return upper
        }

    /// Demonstrates: Parse.Double active pattern, Guard.inRange (used indirectly via validation)
    let coordinates (latStr: string) (lonStr: string) : Result<float * float, ValidationError> =
        result {
            let! lat =
                match latStr with
                | Parse.Double lat -> Ok lat
                | _ -> Error ^ ValidationError.InvalidLatitude latStr

            let! lon =
                match lonStr with
                | Parse.Double lon -> Ok lon
                | _ -> Error ^ ValidationError.InvalidLongitude lonStr

            do! Result.requireTrue
                    (ValidationError.InvalidLatitude $"%f{lat}")
                    (lat >= -90.0 && lat <= 90.0)

            do! Result.requireTrue
                    (ValidationError.InvalidLongitude $"%f{lon}")
                    (lon >= -180.0 && lon <= 180.0)

            return (lat, lon)
        }
