namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp

// ── Test domain ──

type DoorState =
    | Locked
    | Closed
    | Open

type DoorEvent =
    | Lock
    | Unlock
    | OpenDoor
    | CloseDoor

type DoorEffect =
    | PlaySound of string
    | Log of string

type TrafficLight =
    | Red
    | Yellow
    | Green

type TrafficEvent =
    | Timer
    | Emergency

type TrafficEffect = | Alert of string

[<TestFixture>]
type StateMachineTests() =

    let applyDoor state event =
        match state, event with
        | Locked, Unlock -> StateMachine.goto Closed [ PlaySound "click" ]
        | Closed, Lock -> StateMachine.goto Locked [ PlaySound "clack" ]
        | Closed, OpenDoor -> StateMachine.goto Open [ Log "door opened" ]
        | Open, CloseDoor -> StateMachine.goto Closed [ Log "door closed" ]
        | s, e -> StateMachine.fail $"Cannot {e} when {s}"

    [<Test>]
    member _.``valid transition changes state``() =
        match applyDoor Locked Unlock with
        | Ok r ->
            equals r.NewState Closed
            equals r.Effects [ PlaySound "click" ]
        | Error msg -> Assert.Fail(msg)

    [<Test>]
    member _.``chain of transitions``() =
        let mutable state = Locked
        let mutable allEffects = []

        let step event =
            match applyDoor state event with
            | Ok r ->
                state <- r.NewState
                allEffects <- allEffects @ r.Effects
            | Error msg -> Assert.Fail(msg)

        step Unlock // Locked -> Closed
        step OpenDoor // Closed -> Open
        step CloseDoor // Open -> Closed
        step Lock // Closed -> Locked

        equals state Locked
        equals allEffects [ PlaySound "click"; Log "door opened"; Log "door closed"; PlaySound "clack" ]

    [<Test>]
    member _.``invalid transition returns error``() =
        let result = applyDoor Locked OpenDoor
        isError result

    [<Test>]
    member _.``invalid transition uses custom message``() =
        match applyDoor Locked OpenDoor with
        | Error msg -> equals msg "Cannot OpenDoor when Locked"
        | Ok _ -> Assert.Fail("Expected error")

    [<Test>]
    member _.``tryApply keeps state on error``() =
        let newState, result = StateMachine.tryApply Locked (applyDoor Locked OpenDoor)
        equals newState Locked
        isError result

    [<Test>]
    member _.``tryApply returns effects on success``() =
        let newState, result = StateMachine.tryApply Locked (applyDoor Locked Unlock)
        equals newState Closed
        isOk [ PlaySound "click" ] result

    [<Test>]
    member _.``stay keeps state with effects``() =
        let apply state event =
            match state, event with
            | Locked, Lock -> StateMachine.stay state [ Log "already locked" ]
            | _ -> StateMachine.fail "unexpected"

        match apply Locked Lock with
        | Ok r ->
            equals r.NewState Locked
            equals r.Effects [ Log "already locked" ]
        | Error msg -> Assert.Fail(msg)

    [<Test>]
    member _.``stay with empty list for no effects``() =
        let apply state event =
            match state, event with
            | Locked, Lock -> StateMachine.stay state []
            | _ -> StateMachine.fail "unexpected"

        match apply Locked Lock with
        | Ok r ->
            equals r.NewState Locked
            equals r.Effects []
        | Error msg -> Assert.Fail(msg)

    [<Test>]
    member _.``fail returns error``() =
        let apply _state _event = StateMachine.fail "door is locked!"

        match apply Locked OpenDoor with
        | Error msg -> equals msg "door is locked!"
        | Ok _ -> Assert.Fail("Expected error")

    [<Test>]
    member _.``multi-state machine with tuple match``() =
        let apply state event =
            match state, event with
            | Red, Timer -> StateMachine.goto Green []
            | Green, Timer -> StateMachine.goto Yellow []
            | Yellow, Timer -> StateMachine.goto Red []
            | _, Emergency -> StateMachine.goto Red [ Alert "emergency stop" ]

        match apply Green Timer with
        | Ok r -> equals r.NewState Yellow
        | Error msg -> Assert.Fail(msg)

        match apply Green Emergency with
        | Ok r ->
            equals r.NewState Red
            equals r.Effects [ Alert "emergency stop" ]
        | Error msg -> Assert.Fail(msg)
