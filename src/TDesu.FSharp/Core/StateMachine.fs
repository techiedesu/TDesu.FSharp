namespace TDesu.FSharp

/// <summary>
/// Generic finite state machine: state + event → new state + effects.
/// </summary>
/// <example>
/// <code>
/// type State = Idle | Running
/// type Event = Start | Stop
/// type Effect = Log of string
///
/// let b = StateMachine.Builder&lt;State, Event, Effect&gt;()
/// b.StateTag(fun s -> match s with Idle -> 0 | Running -> 1)
/// b.EventTag(fun e -> match e with Start -> 0 | Stop -> 1)
/// b.On(0, 0, fun _ _ -> StateMachine.goto Running [ Log "started" ])
/// b.On(1, 1, fun _ _ -> StateMachine.goto Idle [ Log "stopped" ])
/// let machine = b.Build()
///
/// match StateMachine.apply machine Idle Start with
/// | Ok r -> r.NewState, r.Effects
/// | Error msg -> failwith msg
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module StateMachine =

    /// Result of a state machine transition.
    type TransitionResult<'TState, 'TEffect> = {
        NewState: 'TState
        Effects: 'TEffect list
    }

    /// Compiled state machine definition.
    [<NoComparison; NoEquality>]
    type Definition<'TState, 'TEvent, 'TEffect> = {
        /// Transition handlers indexed by (stateTag, eventTag).
        Transitions: Map<int * int, 'TState -> 'TEvent -> Result<TransitionResult<'TState, 'TEffect>, string>>
        /// Handler for undefined transitions.
        InvalidTransition: 'TState -> 'TEvent -> string
        /// Extract discriminator tag from state (e.g., DU case index).
        StateTag: 'TState -> int
        /// Extract discriminator tag from event (e.g., DU case index).
        EventTag: 'TEvent -> int
    }

    /// Apply a transition: look up handler by (stateTag, eventTag) and execute it.
    /// <param name="def">The state machine definition.</param>
    /// <param name="state">The current state.</param>
    /// <param name="event">The event to process.</param>
    let apply (def: Definition<'TState, 'TEvent, 'TEffect>) (state: 'TState) (event: 'TEvent)
        : Result<TransitionResult<'TState, 'TEffect>, string> =
        let key = (def.StateTag state, def.EventTag event)
        match def.Transitions |> Map.tryFind key with
        | Some handler -> handler state event
        | None -> Error(def.InvalidTransition state event)

    /// Apply transition. On error, state is unchanged.
    /// Returns (newState, Ok effects) or (unchangedState, Error msg).
    /// <param name="def">The state machine definition.</param>
    /// <param name="state">The current state.</param>
    /// <param name="event">The event to process.</param>
    let tryApply (def: Definition<'TState, 'TEvent, 'TEffect>) (state: 'TState) (event: 'TEvent)
        : 'TState * Result<'TEffect list, string> =
        match apply def state event with
        | Ok r -> r.NewState, Ok r.Effects
        | Error msg -> state, Error msg

    /// Create a successful transition to a new state with effects.
    /// <param name="newState">The state to transition to.</param>
    /// <param name="effects">Side effects to produce.</param>
    let inline goto newState effects =
        Ok { NewState = newState; Effects = effects }

    /// Stay in the current state, producing effects (pass <c>[]</c> for no effects).
    /// <param name="state">The current state to remain in.</param>
    /// <param name="effects">Side effects to produce.</param>
    let inline stay state effects =
        Ok { NewState = state; Effects = effects }

    /// Fail the transition with an error message.
    /// <param name="msg">The error message.</param>
    let inline fail (msg: string) : Result<TransitionResult<'TState, 'TEffect>, string> =
        Error msg

    /// Declarative builder for constructing state machine definitions.
    type Builder<'TState, 'TEvent, 'TEffect>() =
        let mutable transitions: (int * int * ('TState -> 'TEvent -> Result<TransitionResult<'TState, 'TEffect>, string>)) list = []
        let mutable invalidHandler: ('TState -> 'TEvent -> string) option = None
        let mutable stateTagFn: ('TState -> int) option = None
        let mutable eventTagFn: ('TEvent -> int) option = None

        /// <summary>
        /// Add a transition handler for the given (stateTag, eventTag) pair.
        /// If the same pair is registered multiple times, only the last handler is kept.
        /// </summary>
        /// <param name="sTag">The state discriminator tag.</param>
        /// <param name="eTag">The event discriminator tag.</param>
        /// <param name="handler">The transition handler function.</param>
        member _.On(sTag: int, eTag: int, handler: 'TState -> 'TEvent -> Result<TransitionResult<'TState, 'TEffect>, string>) =
            transitions <- (sTag, eTag, handler) :: transitions

        /// Set the handler for transitions not in the map.
        /// <param name="handler">Function that returns an error message for invalid transitions.</param>
        member _.Otherwise(handler: 'TState -> 'TEvent -> string) =
            invalidHandler <- Some handler

        /// Set the state tag extractor (discriminated union case index).
        /// <param name="f">Function that returns the tag for a given state.</param>
        member _.StateTag(f: 'TState -> int) =
            stateTagFn <- Some f

        /// Set the event tag extractor (discriminated union case index).
        /// <param name="f">Function that returns the tag for a given event.</param>
        member _.EventTag(f: 'TEvent -> int) =
            eventTagFn <- Some f

        /// Build the state machine definition.
        /// <exception cref="System.InvalidOperationException">When StateTag or EventTag is not set.</exception>
        member _.Build() : Definition<'TState, 'TEvent, 'TEffect> =
            let stTag = match stateTagFn with Some f -> f | None -> invalidOp "StateTag must be set before Build()"
            let evTag = match eventTagFn with Some f -> f | None -> invalidOp "EventTag must be set before Build()"
            {
                Transitions =
                    transitions
                    |> List.map (fun (s, e, h) -> (s, e), h)
                    |> Map.ofList
                InvalidTransition = defaultArg invalidHandler (fun _ _ -> "INVALID_TRANSITION")
                StateTag = stTag
                EventTag = evTag
            }
