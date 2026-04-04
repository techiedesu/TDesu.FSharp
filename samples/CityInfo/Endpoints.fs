module Endpoints

open System
open System.Text.Json
open Falco
open Falco.Routing
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Types
open TDesu.FSharp.Builders
open Domain

/// Demonstrates: taskResult {}, Result.tee, Result.teeError, Result.mapError,
/// ApiResponse.ofResult, Request.getQuery, Response.ofJson

let private jsonOptions =
    let o = JsonSerializerOptions(JsonSerializerDefaults.Web)
    o.WriteIndented <- true
    o

let private ofJson (v: 'a) : HttpHandler = Response.ofJsonOptions jsonOptions v

// ── GET /api/city?name=London ──

let cityHandler: HttpHandler = fun ctx -> task {
    let q = Request.getQuery ctx
    let name = q.GetString("name", "")

    let! result = taskResult {
        let! city =
            Validate.city name
            |> Result.mapError ServiceError.Validation

        let cityStr = NonEmptyString.value city
        return! Services.CityInfoService.getCityInfo cityStr ctx.RequestAborted
    }

    let response =
        result
        |> Result.tee (fun (r: CityReport) -> Log.infof "City report: %s (%s)" r.Location.City r.Location.Country)
        |> Result.teeError (fun e -> Log.errorf "City error: %A" e)
        |> ApiResponse.ofResult

    return! ofJson response ctx
}

// ── GET /api/city/export?name=London ──

let exportHandler: HttpHandler = fun ctx -> task {
    let q = Request.getQuery ctx
    let name = q.GetString("name", "")

    let validationResult =
        Validate.city name
        |> Result.mapError ServiceError.Validation

    match validationResult with
    | Error e ->
        Log.errorf "Export validation error: %A" e
        return! ofJson (ApiResponse.error e) ctx
    | Ok city ->
        let cityStr = NonEmptyString.value city
        match! Services.CityInfoService.getCityInfo cityStr ctx.RequestAborted with
        | Error e ->
            return! ofJson (ApiResponse.error e) ctx
        | Ok report ->
            use! csvStream = Services.ExportService.exportCsv report
            ctx.Response.ContentType <- "text/csv"
            ctx.Response.Headers["Content-Disposition"] <- icast $"attachment; filename=\"%s{cityStr}.csv\""
            do! csvStream.CopyToAsync(ctx.Response.Body)
}

// ── GET /api/rates?base=USD&to=EUR,GBP ──

let ratesHandler: HttpHandler = fun ctx -> task {
    let query = Request.getQuery ctx
    let baseCurrency = query.GetString("base", "USD")
    let toCurrencies = query.GetString("to", "")

    let! result = taskResult {
        let! validBase =
            Validate.currency baseCurrency
            |> Result.mapError ServiceError.Validation

        let! (rates: ExchangeRates) = task {
            try
                let http = ctx.RequestServices.GetService(typeof<System.Net.Http.HttpClient>) :?> System.Net.Http.HttpClient
                let! r = Providers.CurrencyProvider.latest http validBase ctx.RequestAborted
                return Ok r
            with ex ->
                return Error ^ ServiceError.ProviderUnavailable ("currency", ex.Message)
        }

        let filtered =
            if String.isNullOrWhiteSpace toCurrencies then
                rates
            else
                let targets =
                    toCurrencies
                    |> String.split ","
                    |> Array.map String.trim
                    |> Array.map String.toUpperInv

                { rates with
                    Rates =
                        rates.Rates
                        |> Map.filter (fun k _ -> targets |> Array.contains k) }

        return filtered
    }

    return! ofJson (ApiResponse.ofResult result) ctx
}

// ── GET /api/stats ──

let statsHandler: HttpHandler = fun ctx -> task {
    let stats = Services.getStats ()
    return! ofJson (ApiResponse.ok stats) ctx
}

// ── GET /api/health ──

type HealthResponse = {
    Status: string
    Uptime: int64
    Timestamp: int64
}

let private startTime = UnixTime.seconds ()

let healthHandler: HttpHandler = fun ctx -> task {
    let now = UnixTime.seconds ()
    let response = {
        Status = "healthy"
        Uptime = now - startTime
        Timestamp = now
    }
    return! ofJson response ctx
}

// ── Route table ──

let all = [
    get "/api/city" cityHandler
    get "/api/city/export" exportHandler
    get "/api/rates" ratesHandler
    get "/api/stats" statsHandler
    get "/api/health" healthHandler
]
