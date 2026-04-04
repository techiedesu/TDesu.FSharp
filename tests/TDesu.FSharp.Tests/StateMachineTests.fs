namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp

// ── Test domain ──

type DoorState = Locked | Closed | Open
type DoorEvent = Lock | Unlock | OpenDoor | CloseDoor
type DoorEffect = PlaySound of string | Log of string

[<TestFixture>]
type StateMachineTests() =

    let doorMachine =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.StateTag(fun s -> match s with Locked -> 0 | Closed -> 1 | Open -> 2)
        b.EventTag(fun e -> match e with Lock -> 0 | Unlock -> 1 | OpenDoor -> 2 | CloseDoor -> 3)

        // Locked + Unlock → Closed
        b.On(0, 1, fun _ _ -> StateMachine.goto Closed [ PlaySound "click" ])
        // Closed + Lock → Locked
        b.On(1, 0, fun _ _ -> StateMachine.goto Locked [ PlaySound "clack" ])
        // Closed + Open → Open
        b.On(1, 2, fun _ _ -> StateMachine.goto Open [ Log "door opened" ])
        // Open + Close → Closed
        b.On(2, 3, fun _ _ -> StateMachine.goto Closed [ Log "door closed" ])

        b.Otherwise(fun s e -> $"Cannot {e} when {s}")
        b.Build()

    [<Test>]
    member _.``valid transition changes state``() =
        let result = StateMachine.apply doorMachine Locked Unlock
        match result with
        | Ok r ->
            equals r.NewState Closed
            equals r.Effects [ PlaySound "click" ]
        | Error msg -> Assert.Fail(msg)

    [<Test>]
    member _.``chain of transitions``() =
        let mutable state = Locked
        let mutable allEffects = []

        let step event =
            match StateMachine.apply doorMachine state event with
            | Ok r ->
                state <- r.NewState
                allEffects <- allEffects @ r.Effects
            | Error msg -> Assert.Fail(msg)

        step Unlock      // Locked → Closed
        step OpenDoor    // Closed → Open
        step CloseDoor   // Open → Closed
        step Lock        // Closed → Locked

        equals state Locked
        equals allEffects [
            PlaySound "click"
            Log "door opened"
            Log "door closed"
            PlaySound "clack"
        ]

    [<Test>]
    member _.``invalid transition returns error``() =
        let result = StateMachine.apply doorMachine Locked OpenDoor
        isError result

    [<Test>]
    member _.``invalid transition uses Otherwise message``() =
        match StateMachine.apply doorMachine Locked OpenDoor with
        | Error msg -> equals msg "Cannot OpenDoor when Locked"
        | Ok _ -> Assert.Fail("Expected error")

    [<Test>]
    member _.``default invalid transition message``() =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.StateTag(fun s -> match s with Locked -> 0 | Closed -> 1 | Open -> 2)
        b.EventTag(fun e -> match e with Lock -> 0 | Unlock -> 1 | OpenDoor -> 2 | CloseDoor -> 3)
        let machine = b.Build()

        match StateMachine.apply machine Locked Lock with
        | Error msg -> equals msg "INVALID_TRANSITION"
        | Ok _ -> Assert.Fail("Expected error")

    [<Test>]
    member _.``tryApply keeps state on error``() =
        let newState, result = StateMachine.tryApply doorMachine Locked OpenDoor
        equals newState Locked
        isError result

    [<Test>]
    member _.``tryApply returns effects on success``() =
        let newState, result = StateMachine.tryApply doorMachine Locked Unlock
        equals newState Closed
        isOk [ PlaySound "click" ] result

    [<Test>]
    member _.``stay keeps state with effects``() =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.StateTag(fun s -> match s with Locked -> 0 | Closed -> 1 | Open -> 2)
        b.EventTag(fun e -> match e with Lock -> 0 | Unlock -> 1 | OpenDoor -> 2 | CloseDoor -> 3)
        b.On(0, 0, fun s _ -> StateMachine.stay s [ Log "already locked" ])
        let machine = b.Build()

        let result = StateMachine.apply machine Locked Lock
        match result with
        | Ok r ->
            equals r.NewState Locked
            equals r.Effects [ Log "already locked" ]
        | Error msg -> Assert.Fail(msg)

    [<Test>]
    member _.``stay with empty list for no effects``() =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.StateTag(fun s -> match s with Locked -> 0 | Closed -> 1 | Open -> 2)
        b.EventTag(fun e -> match e with Lock -> 0 | Unlock -> 1 | OpenDoor -> 2 | CloseDoor -> 3)
        b.On(0, 0, fun s _ -> StateMachine.stay s [])
        let machine = b.Build()

        match StateMachine.apply machine Locked Lock with
        | Ok r ->
            equals r.NewState Locked
            equals r.Effects []
        | Error msg -> Assert.Fail(msg)

    [<Test>]
    member _.``fail returns error``() =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.StateTag(fun s -> match s with Locked -> 0 | Closed -> 1 | Open -> 2)
        b.EventTag(fun e -> match e with Lock -> 0 | Unlock -> 1 | OpenDoor -> 2 | CloseDoor -> 3)
        b.On(0, 2, fun _ _ -> StateMachine.fail "door is locked!")
        let machine = b.Build()

        match StateMachine.apply machine Locked OpenDoor with
        | Error msg -> equals msg "door is locked!"
        | Ok _ -> Assert.Fail("Expected error")

    [<Test>]
    member _.``Build throws when StateTag not set``() =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.EventTag(fun e -> match e with Lock -> 0 | Unlock -> 1 | OpenDoor -> 2 | CloseDoor -> 3)

        Assert.Throws<InvalidOperationException>(fun () ->
            b.Build() |> ignore)
        |> ignore

    [<Test>]
    member _.``Build throws when EventTag not set``() =
        let b = StateMachine.Builder<DoorState, DoorEvent, DoorEffect>()
        b.StateTag(fun s -> match s with Locked -> 0 | Closed -> 1 | Open -> 2)

        Assert.Throws<InvalidOperationException>(fun () ->
            b.Build() |> ignore)
        |> ignore
