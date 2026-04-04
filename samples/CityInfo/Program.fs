module Program

open System
open System.Net.Http
open System.Threading
open Microsoft.AspNetCore.Builder
open Falco
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.IO
open TDesu.FSharp.Concurrency

/// Demonstrates: PeriodicTimer.start, Disposable.deferStack, Task.fireAndForget,
/// ^ operator, % operator

[<EntryPoint>]
let main _ =

    // Demonstrates: Disposable.deferStack — cleanup on shutdown
    use cleanup = Disposable.deferStack ()

    let cts = new CancellationTokenSource()
    cleanup.AddDisposable(cts)

    // Shared HttpClient — one instance for all providers
    let http = new HttpClient()
    http.Timeout <- TimeSpan.FromSeconds(30.0)
    cleanup.AddDisposable(http)

    // Initialize cached city service
    Services.CityInfoService.init http

    Log.info "Starting TDesu.FSharp.Sample — City Info API"

    // Demonstrates: PeriodicTimer.start — background stats logging every 60s
    %PeriodicTimer.start (TimeSpan.FromSeconds(60.0)) (fun () -> task {
        let stats = Services.getStats ()
        Log.infof "Periodic stats: requests=%d, hits=%d, errors=%d"
            stats.Requests stats.CacheHits stats.Errors
    }) cts.Token

    // Demonstrates: Task.fireAndForget — run provisioning saga demo in background
    Task.fireAndForget
        (fun ex -> Log.errorf "Saga error: %s" ex.Message)
        (fun () -> task {
            let! result = Services.ProvisioningSaga.provision "demo-city"
            match result with
            | Ok ctx -> Log.infof "Saga completed: alert=%b, webhook=%b" ctx.AlertCreated ctx.WebhookCreated
            | Error ex -> Log.errorf "Saga failed: %s" ex.Message
        })

    // Build and run web app
    let wapp =
        WebApplication
            .CreateSlimBuilder()
            .Build()
            .UseRouting()
            .UseFalco(Endpoints.all)

    Console.CancelKeyPress.Add(fun e ->
        e.Cancel <- true
        cts.Cancel()
        Log.info "Shutting down...")

    Log.info "Listening on http://localhost:5000"
    wapp.Run("http://localhost:5000")
    0
