namespace TDesu.FSharp.Tests

open System
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators
open TDesu.FSharp.Tasks
open TDesu.FSharp.Concurrency

[<TestFixture>]
type AtomicIntTests() =

    [<Test>]
    member _.``initial value``() =
        let a = AtomicInt(42)
        equals a.Value 42

    [<Test>]
    member _.``default is zero``() =
        let a = AtomicInt()
        equals a.Value 0

    [<Test>]
    member _.``increment``() =
        let a = AtomicInt(0)
        equals (a.Increment()) 1
        equals (a.Increment()) 2
        equals a.Value 2

    [<Test>]
    member _.``decrement``() =
        let a = AtomicInt(5)
        equals (a.Decrement()) 4

    [<Test>]
    member _.``add``() =
        let a = AtomicInt(10)
        equals (a.Add(5)) 15

    [<Test>]
    member _.``exchange``() =
        let a = AtomicInt(10)
        let old = a.Exchange(20)
        equals old 10
        equals a.Value 20

    [<Test>]
    member _.``compare exchange``() =
        let a = AtomicInt(10)
        isTrue (a.CompareExchange(20, 10)) // current=10, matches
        equals a.Value 20
        isFalse (a.CompareExchange(30, 10)) // current=20, no match
        equals a.Value 20

    [<Test>]
    member _.``reset``() =
        let a = AtomicInt(42)
        let old = a.Reset()
        equals old 42
        equals a.Value 0

[<TestFixture>]
type AtomicInt64Tests() =

    [<Test>]
    member _.``increment and value``() =
        let a = AtomicInt64(0L)
        %a.Increment()
        %a.Increment()
        equals a.Value 2L

[<TestFixture>]
type BoundedQueueTests() =

    [<Test>]
    member _.``respects capacity``() =
        let q = BoundedQueue<int>(3)
        q.Enqueue(1)
        q.Enqueue(2)
        q.Enqueue(3)
        q.Enqueue(4)
        equals q.Count 3
        equals (q.Dequeue()) 2 // 1 was evicted

    [<Test>]
    member _.``empty queue``() =
        let q = BoundedQueue<int>(5)
        equals q.Count 0

    [<Test>]
    member _.``enqueue under capacity``() =
        let q = BoundedQueue<int>(5)
        q.Enqueue(1)
        q.Enqueue(2)
        equals q.Count 2
        equals (q.Peek()) 1

    [<Test>]
    member _.``toSeq returns all``() =
        let q = BoundedQueue<int>(5)
        q.Enqueue(1)
        q.Enqueue(2)
        q.Enqueue(3)
        equals (q.ToSeq() |> Seq.toArray) [| 1; 2; 3 |]

[<TestFixture>]
type BoundedDictTests() =

    [<Test>]
    member _.``respects capacity``() =
        let d = BoundedDict<string, int>(2)
        d.Set("a", 1)
        d.Set("b", 2)
        d.Set("c", 3)
        equals d.Count 2
        isFalse (d.ContainsKey "a") // evicted
        isSome 2 (d.TryGet "b")
        isSome 3 (d.TryGet "c")

    [<Test>]
    member _.``update existing key does not evict``() =
        let d = BoundedDict<string, int>(2)
        d.Set("a", 1)
        d.Set("b", 2)
        d.Set("a", 10) // update, not new key
        equals d.Count 2
        isSome 10 (d.TryGet "a")

    [<Test>]
    member _.``tryGet returns None for missing``() =
        let d = BoundedDict<string, int>(5)
        isNone (d.TryGet "x")

    [<Test>]
    member _.``remove works``() =
        let d = BoundedDict<string, int>(5)
        d.Set("a", 1)
        isTrue (d.Remove "a")
        equals d.Count 0

[<TestFixture>]
type PeriodicTimerTests() =

    [<Test>]
    member _.``runs action periodically``() =
        task {
            let mutable count = 0
            use cts = new CancellationTokenSource()

            let action () =
                task {
                    count <- count + 1

                    if count >= 3 then
                        cts.Cancel()
                }

            let _ = PeriodicTimer.start (TimeSpan.FromMilliseconds 20.) action cts.Token ignore
            let! reached = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> count >= 3)
            isTrue reached
        }

[<TestFixture>]
type SignalTests() =

    [<Test>]
    member _.``set releases waiter``() =
        task {
            let signal = Signal()
            isFalse signal.IsSet
            signal.Set()
            isTrue signal.IsSet
            do! signal.Wait()
        }

    [<Test>]
    member _.``wait with timeout returns true when signaled``() =
        task {
            let signal = Signal()

            Task.fireAndForget
                ignore
                (fun () ->
                    task {
                        do! Task.Delay 20
                        signal.Set()
                    }
                )

            let! result = signal.Wait(TimeSpan.FromSeconds 2.)
            isTrue result
        }

    [<Test>]
    member _.``wait with timeout returns false on timeout``() =
        task {
            let signal = Signal()
            let! result = signal.Wait(TimeSpan.FromMilliseconds 30.)
            isFalse result
        }

    [<Test>]
    member _.``set is idempotent``() =
        let signal = Signal()
        signal.Set()
        signal.Set()
        isTrue signal.IsSet

[<TestFixture>]
type TaskWaitUntilTests() =

    [<Test>]
    member _.``returns true when condition met``() =
        task {
            let mutable ready = false

            Task.fireAndForget
                ignore
                (fun () ->
                    task {
                        do! Task.Delay 20
                        ready <- true
                    }
                )

            let! result = Task.waitUntil (TimeSpan.FromSeconds 2.) (fun () -> ready)
            isTrue result
        }

    [<Test>]
    member _.``returns false on timeout``() =
        task {
            let! result = Task.waitUntil (TimeSpan.FromMilliseconds 30.) (fun () -> false)
            isFalse result
        }

[<TestFixture>]
type SnapshotThrottleTests() =

    [<Test>]
    member _.``triggers at threshold``() =
        let t = SnapshotThrottle(3)
        isFalse (t.Record())
        isFalse (t.Record())
        isTrue (t.Record()) // 3rd message
        isFalse (t.Record()) // reset, starts over

    [<Test>]
    member _.``reset clears counter``() =
        let t = SnapshotThrottle(5)
        %t.Record()
        %t.Record()
        t.Reset()
        equals t.Count 0
