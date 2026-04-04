module Providers

open System
open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open TDesu.FSharp.Operators
open TDesu.FSharp.Resilience
open Domain

/// Demonstrates: CircuitBreaker.create, Retry.withBackoff, Timeout.afterLinked
/// Each provider wraps HTTP calls with: Timeout -> CircuitBreaker -> Retry

// ── Shared resilience config ──

let private defaultCbConfig: CircuitBreaker.Config = {
    Threshold = 3
    Cooldown = TimeSpan.FromSeconds(30.0)
}

let private retryCount = 2
let private retryDelay = TimeSpan.FromMilliseconds(500.0)
let private callTimeout = TimeSpan.FromSeconds(10.0)

/// Wraps an HTTP call with timeout + circuit breaker + retry
let private resilient
    (cb: (unit -> Task<'T>) -> Task<'T>)
    (parentCt: CancellationToken)
    (f: CancellationToken -> Task<'T>)
    : Task<'T> =
    Retry.withBackoff retryCount retryDelay parentCt (fun () ->
        cb (fun () ->
            Timeout.afterLinked callTimeout parentCt f))

// ── JSON helpers ──

let private getJson (http: HttpClient) (url: string) (ct: CancellationToken) : Task<JsonDocument> =
    task {
        let! response = http.GetAsync(url, ct)
        %response.EnsureSuccessStatusCode()
        let! stream = response.Content.ReadAsStreamAsync()
        return! JsonDocument.ParseAsync(stream, cancellationToken = ct)
    }

// ── GeoProvider ──

module GeoProvider =
    let private cb = CircuitBreaker.create defaultCbConfig

    let search (http: HttpClient) (city: string) (ct: CancellationToken) : Task<GeoLocation> =
        resilient cb ct (fun ct -> task {
            let url = $"https://geocoding-api.open-meteo.com/v1/search?name=%s{Uri.EscapeDataString(city)}&count=1"
            use! doc = getJson http url ct

            let results = doc.RootElement.GetProperty("results")
            let r =
                if results.GetArrayLength() = 0 then
                    invalidOpf "City not found: %s" city
                else
                    results[0]

            return {
                City = r.GetProperty("name").GetString()
                Country = r.GetProperty("country").GetString()
                Latitude = r.GetProperty("latitude").GetDouble()
                Longitude = r.GetProperty("longitude").GetDouble()
                Timezone = r.GetProperty("timezone").GetString()
            }
        })

// ── WeatherProvider ──

module WeatherProvider =
    let private cb = CircuitBreaker.create defaultCbConfig

    let current (http: HttpClient) (lat: float) (lon: float) (ct: CancellationToken) : Task<WeatherInfo> =
        resilient cb ct (fun ct -> task {
            let url =
                $"https://api.open-meteo.com/v1/forecast?latitude=%f{lat}&longitude=%f{lon}" +
                "&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m"
            use! doc = getJson http url ct

            let c = doc.RootElement.GetProperty("current")
            return {
                Temperature = c.GetProperty("temperature_2m").GetDouble()
                Humidity = c.GetProperty("relative_humidity_2m").GetDouble()
                WindSpeed = c.GetProperty("wind_speed_10m").GetDouble()
                WeatherCode = c.GetProperty("weather_code").GetInt32()
            }
        })

// ── CurrencyProvider ──

module CurrencyProvider =
    let private cb = CircuitBreaker.create defaultCbConfig

    let latest (http: HttpClient) (baseCurrency: string) (ct: CancellationToken) : Task<ExchangeRates> =
        resilient cb ct (fun ct -> task {
            let url = $"https://api.frankfurter.dev/v1/latest?base=%s{baseCurrency}"
            use! doc = getJson http url ct

            let rates = doc.RootElement.GetProperty("rates")
            let rateMap =
                rates.EnumerateObject()
                |> Seq.map (fun p -> p.Name, p.Value.GetDouble())
                |> Map.ofSeq

            return {
                Base = baseCurrency
                Rates = rateMap
            }
        })

// ── TimezoneProvider ──

module TimezoneProvider =
    let private cb = CircuitBreaker.create defaultCbConfig

    let get (http: HttpClient) (timezone: string) (ct: CancellationToken) : Task<TimezoneInfo> =
        resilient cb ct (fun ct -> task {
            let url = $"http://worldtimeapi.org/api/timezone/%s{Uri.EscapeDataString(timezone)}"
            use! doc = getJson http url ct

            let r = doc.RootElement
            return {
                TimezoneInfo.Timezone = r.GetProperty("timezone").GetString()
                DateTime = r.GetProperty("datetime").GetString()
                UtcOffset = r.GetProperty("utc_offset").GetString()
            }
        })
