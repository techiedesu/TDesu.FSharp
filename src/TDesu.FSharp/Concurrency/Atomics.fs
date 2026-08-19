namespace TDesu.FSharp.Concurrency

/// <summary>
/// Thread-safe atomic counter using <see cref="System.Threading.Interlocked"/>. Zero-allocation reads.
/// </summary>
/// <param name="initial">The initial counter value.</param>
[<Sealed>]
type AtomicInt(initial: int) =
    let mutable value = initial
    new() = AtomicInt(0)

    /// Gets the current value.
    member _.Value = System.Threading.Volatile.Read(&value)

    /// Increments and returns the new value.
    member _.Increment() =
        System.Threading.Interlocked.Increment(&value)

    /// Decrements and returns the new value.
    member _.Decrement() =
        System.Threading.Interlocked.Decrement(&value)

    /// Adds delta and returns the new value.
    /// <param name="delta">The value to add to the counter.</param>
    member _.Add(delta: int) =
        System.Threading.Interlocked.Add(&value, delta)

    /// Sets to newValue, returns the old value.
    /// <param name="newValue">The value to set.</param>
    member _.Exchange(newValue: int) =
        System.Threading.Interlocked.Exchange(&value, newValue)

    /// Sets to newValue if current equals comparand. Returns true if exchanged.
    /// <param name="newValue">The value to set if the current value matches the comparand.</param>
    /// <param name="comparand">The value to compare against.</param>
    member _.CompareExchange(newValue: int, comparand: int) =
        System.Threading.Interlocked.CompareExchange(&value, newValue, comparand) = comparand

    /// Resets to 0 and returns previous value.
    member this.Reset() = this.Exchange(0)

    override _.ToString() = string value

/// Thread-safe atomic int64 counter.
/// <param name="initial">The initial counter value.</param>
[<Sealed>]
type AtomicInt64(initial: int64) =
    let mutable value = initial
    new() = AtomicInt64(0L)
    member _.Value = System.Threading.Volatile.Read(&value)

    member _.Increment() =
        System.Threading.Interlocked.Increment(&value)

    member _.Decrement() =
        System.Threading.Interlocked.Decrement(&value)

    member _.Add(delta: int64) =
        System.Threading.Interlocked.Add(&value, delta)

    member _.Exchange(newValue: int64) =
        System.Threading.Interlocked.Exchange(&value, newValue)

    member _.Reset() =
        System.Threading.Interlocked.Exchange(&value, 0L)

    override _.ToString() = string value
