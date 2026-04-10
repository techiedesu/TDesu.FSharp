namespace TDesu.FSharp

/// <summary>
/// Helpers for building state machines as plain F# match expressions.
/// Define transitions as <c>let apply state event = match state, event with ...</c>
/// and use <c>goto</c>/<c>stay</c>/<c>fail</c> to build results.
/// </summary>
/// <example>
/// <code>
/// type State = Idle | Running
/// type Event = Start | Stop
/// type Effect = Log of string
///
/// let apply state event =
///     match state, event with
///     | Idle, Start -> StateMachine.goto Running [ Log "started" ]
///     | Running, Stop -> StateMachine.goto Idle [ Log "stopped" ]
///     | _ -> StateMachine.fail "invalid transition"
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module StateMachine =

    /// Result of a state machine transition.
    type TransitionResult<'TState, 'TEffect> = {
        NewState: 'TState
        Effects: 'TEffect list
    }

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

    /// Apply a transition result, keeping state unchanged on error.
    /// Returns (newState, Ok effects) or (unchangedState, Error msg).
    /// <param name="state">The current state (returned unchanged on error).</param>
    /// <param name="result">The transition result from your apply function.</param>
    let tryApply (state: 'TState) (result: Result<TransitionResult<'TState, 'TEffect>, string>)
        : 'TState * Result<'TEffect list, string> =
        match result with
        | Ok r -> r.NewState, Ok r.Effects
        | Error msg -> state, Error msg
