module Services

open System
open System.Net.Http
open System.Text
open System.Threading
open System.Threading.Tasks
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.IO
open TDesu.FSharp.Concurrency
open TDesu.FSharp.Resilience
open TDesu.FSharp.Hashing
open TDesu.FSharp.Buffers
open Domain

/// Demonstrates: AtomicInt64, BoundedDict, SnapshotThrottle, Memoize.withTtlAsync,
/// Task.parallelThrottle, Seq.tryAverage, ContentHash.sha256Hex, Saga.run/step,
/// TemporaryFileStream, ArrayPool.useBytes

// ── Stats ──

let requestCount = AtomicInt64()
let cacheHits = AtomicInt64()
let errorCount = AtomicInt64()
let recentQueries = BoundedDict<string, int64>(50)
let statsThrottle = SnapshotThrottle(100)

type Stats = {
    Requests: int64
    CacheHits: int64
    Errors: int64
    RecentQueryCount: int
}

let getStats () : Stats = {
    Requests = requestCount.Value
    CacheHits = cacheHits.Value
    Errors = errorCount.Value
    RecentQueryCount = recentQueries.Count
}

// ── City Info Service ──

module CityInfoService =

    /// Demonstrates: ContentHash.sha256Hex for cache key
    let private cacheKey (city: string) =
        ContentHash.sha256Hex ^ String.toLowerInv city

    /// Core lookup — not cached
    let private fetchCityInfo
        (http: HttpClient)
        (city: string)
        (ct: CancellationToken)
        : Task<Domain.CityReport> =
        task {
            %requestCount.Increment()
            recentQueries.Set(city, UnixTime.seconds ())

            // Step 1: geocode
            let! geo = Providers.GeoProvider.search http city ct

            // Step 2: parallel fetch weather + currency + timezone
            // Demonstrates: Task.parallelThrottle
            let providers: (string * (CancellationToken -> Task<obj>)) array = [|
                "weather", fun ct -> task {
                    let! w = Providers.WeatherProvider.current http geo.Latitude geo.Longitude ct
                    return w :> obj
                }
                "currency", fun ct -> task {
                    let! r = Providers.CurrencyProvider.latest http "USD" ct
                    return r :> obj
                }
                "timezone", fun ct -> task {
                    let! t = Providers.TimezoneProvider.get http geo.Timezone ct
                    return t :> obj
                }
            |]

            let! results =
                Task.parallelThrottle 3 providers (fun (name, f) -> task {
                    try
                        let! r = f ct
                        return Some (name, r)
                    with ex ->
                        Log.warnf "Provider %s failed: %s" name ex.Message
                        %errorCount.Increment()
                        return None
                })

            let lookup =
                results
                |> Array.choose id
                |> Map.ofArray

            let weather =
                lookup
                |> Map.tryFind "weather"
                |> Option.map (fun o -> o :?> Domain.WeatherInfo)

            let rates =
                lookup
                |> Map.tryFind "currency"
                |> Option.map (fun o -> o :?> Domain.ExchangeRates)

            let time =
                lookup
                |> Map.tryFind "timezone"
                |> Option.map (fun o -> o :?> Domain.TimezoneInfo)

            return {
                Location = geo
                Weather = weather
                Rates = rates
                Time = time
                GeneratedAt = UnixTime.seconds ()
            }
        }

    /// Demonstrates: Memoize.withTtlAsync — 5 min TTL cache
    let private cachedFetch (http: HttpClient) =
        Memoize.withTtlAsync (TimeSpan.FromMinutes(5.0)) (fun (city: string) ->
            task {
                let! report = fetchCityInfo http city CancellationToken.None
                return report
            })

    let mutable private getCached: (string -> Task<Domain.CityReport>) option = None

    let init (http: HttpClient) =
        getCached <- Some ^ cachedFetch http

    let getCityInfo (city: string) (_ct: CancellationToken) : Task<Result<Domain.CityReport, Domain.ServiceError>> =
        task {
            try
                let fetch = getCached |> Option.get
                let _key = cacheKey city
                // Note: Memoize handles caching internally; we track hits via BoundedDict
                match recentQueries.TryGet(city) with
                | Some _ -> %cacheHits.Increment()
                | None -> ()
                let! report = fetch city
                recentQueries.Set(city, UnixTime.seconds ())

                // Demonstrates: SnapshotThrottle — log stats every 100 requests
                if statsThrottle.Record() then
                    Log.infof "Stats snapshot: %d requests, %d hits, %d errors"
                        requestCount.Value cacheHits.Value errorCount.Value

                return Ok report
            with ex ->
                %errorCount.Increment()
                return Error ^ Domain.ServiceError.ProviderUnavailable ("city-info", ex.Message)
        }

// ── Export Service ──

module ExportService =

    /// Demonstrates: TemporaryFileStream, ArrayPool.useBytes
    let exportCsv (report: Domain.CityReport) : Task<TDesu.FSharp.IO.TemporaryFileStream> =
        task {
            let tfs = new TDesu.FSharp.IO.TemporaryFileStream()

            let header = "Field,Value\n"
            let lines = [
                $"City,%s{report.Location.City}"
                $"Country,%s{report.Location.Country}"
                $"Latitude,%f{report.Location.Latitude}"
                $"Longitude,%f{report.Location.Longitude}"
                $"Timezone,%s{report.Location.Timezone}"
            ]

            let weatherLines =
                match report.Weather with
                | Some w -> [
                    $"Temperature,%f{w.Temperature}"
                    $"Humidity,%f{w.Humidity}"
                    $"WindSpeed,%f{w.WindSpeed}"
                    $"WeatherCode,%d{w.WeatherCode}"
                  ]
                | None -> []

            let rateLines =
                match report.Rates with
                | Some r ->
                    r.Rates |> Map.toList |> List.map (fun (k, v) -> $"Rate_%s{k},%f{v}")
                | None -> []

            let timeLines =
                match report.Time with
                | Some t -> [
                    $"LocalTime,%s{t.DateTime}"
                    $"UtcOffset,%s{t.UtcOffset}"
                  ]
                | None -> []

            let allLines = lines @ weatherLines @ rateLines @ timeLines
            let csv = header + (allLines |> String.join "\n") + "\n"

            // Demonstrates: ArrayPool.useBytes
            ArrayPool.useBytes 4096 (fun _buf ->
                let bytes = Encoding.UTF8.GetBytes(csv)
                tfs.Write(bytes, 0, bytes.Length))

            tfs.Position <- 0L
            return tfs
        }

// ── Provisioning Saga (demo) ──

module ProvisioningSaga =

    type SagaCtx = {
        City: string
        AlertCreated: bool
        WebhookCreated: bool
    }

    /// Demonstrates: Saga.run, Saga.step — transactional orchestration with compensation
    let provision (city: string) : Task<Result<SagaCtx, exn>> =
        let steps = [
            Saga.step
                "create-alert"
                (fun ctx -> task {
                    Log.infof "Saga: creating alert for %s" ctx.City
                    return { ctx with AlertCreated = true }
                })
                (fun ctx -> task {
                    Log.warnf "Saga: compensating alert for %s" ctx.City
                    return ()
                })

            Saga.step
                "create-webhook"
                (fun ctx -> task {
                    Log.infof "Saga: creating webhook for %s" ctx.City
                    return { ctx with WebhookCreated = true }
                })
                (fun ctx -> task {
                    Log.warnf "Saga: compensating webhook for %s" ctx.City
                    return ()
                })
        ]

        Saga.run steps { City = city; AlertCreated = false; WebhookCreated = false }
