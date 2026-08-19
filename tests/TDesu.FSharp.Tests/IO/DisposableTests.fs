namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp.IO

[<TestFixture>]
type DisposableTests() =

    [<Test>]
    member _.``combine disposes all in reverse order``() =
        let mutable order = []
        let d1 = Disposable.create (fun () -> order <- 1 :: order)
        let d2 = Disposable.create (fun () -> order <- 2 :: order)
        let d3 = Disposable.create (fun () -> order <- 3 :: order)
        let combined = Disposable.combine [ d1; d2; d3 ]
        combined.Dispose()
        // disposed in reverse: d3(3), d2(2), d1(1); prepend → [1; 2; 3]
        equals (order |> List.toArray) [| 1; 2; 3 |]

    [<Test>]
    member _.``createOnce runs cleanup only once``() =
        let mutable count = 0
        let d = Disposable.createOnce (fun () -> count <- count + 1)
        d.Dispose()
        d.Dispose()
        d.Dispose()
        equals count 1

    [<Test>]
    member _.``DeferStack runs cleanups in LIFO order``() =
        let mutable order = []

        do
            use stack = Disposable.deferStack ()
            stack.Add(fun () -> order <- 1 :: order)
            stack.Add(fun () -> order <- 2 :: order)
            stack.Add(fun () -> order <- 3 :: order)
        // LIFO: 3, 2, 1 — prepend gives [1; 2; 3]
        equals (order |> List.toArray) [| 1; 2; 3 |]
