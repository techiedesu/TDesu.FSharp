namespace TDesu.FSharp

open System

/// Abstraction over system clock for testable time-dependent code.
/// <example>
/// <code>
/// let clock = SystemClock.Instance
/// let now = clock.UtcNow
///
/// // In tests:
/// let fake = FakeClock(DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero))
/// fake.Advance(TimeSpan.FromHours 1.)
/// </code>
/// </example>
[<Interface>]
type IClock =
    /// Gets the current UTC date and time.
    abstract UtcNow: DateTimeOffset

/// Default clock implementation using the real system clock.
[<Sealed>]
type SystemClock private () =
    static let instance = SystemClock()

    /// Singleton instance.
    static member Instance = instance :> IClock

    interface IClock with
        member _.UtcNow = DateTimeOffset.UtcNow

/// <summary>
/// Fake clock for testing. Allows manual time control.
/// Not thread-safe.
/// </summary>
/// <param name="startTime">The initial time for the fake clock.</param>
[<Sealed>]
type FakeClock(startTime: DateTimeOffset) =
    let mutable now = startTime

    /// Creates a FakeClock starting at the current UTC time.
    new() = FakeClock(DateTimeOffset.UtcNow)

    /// Advance the clock by the given duration.
    /// <param name="duration">The amount of time to advance.</param>
    member _.Advance(duration: TimeSpan) = now <- now + duration

    /// Set the clock to a specific time.
    /// <param name="time">The time to set.</param>
    member _.Set(time: DateTimeOffset) = now <- time

    /// Current fake time.
    member _.Current = now

    interface IClock with
        member _.UtcNow = now
