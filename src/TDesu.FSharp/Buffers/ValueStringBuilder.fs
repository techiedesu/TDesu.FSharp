namespace TDesu.FSharp.Buffers

open System
open System.Buffers
open System.Runtime.CompilerServices

/// <summary>
/// A stack-first string builder: writes into a caller-supplied <see cref="System.Span{T}"/>
/// and only rents from the shared <see cref="System.Buffers.ArrayPool{T}"/> once that buffer
/// overflows. Call <c>Dispose()</c> once to return the rented array.
/// </summary>
/// <example>
/// The one supported shape — a local <c>let mutable</c>, plain statement calls, disposed in a
/// <c>finally</c>:
/// <code>
/// let describe (name: string) (count: int) =
///     let buffer = Array.zeroCreate&lt;char&gt; 64
///     let mutable sb = ValueStringBuilder(Span&lt;char&gt;(buffer))
///     try
///         sb.Append("name=")
///         sb.Append(name)
///         sb.ToString()
///     finally
///         sb.Dispose()
/// </code>
/// </example>
/// <remarks>
/// <para>
/// This is a <c>ref struct</c>, the same tradeoff that keeps the BCL's own
/// <c>System.Text.ValueStringBuilder</c> internal. Three ways it bites:
/// </para>
/// <list type="bullet">
///   <item><description>
///   <b>Caught by the compiler:</b> storing it in a field, capturing it in a closure, or using
///   it across a <c>let!</c>/<c>do!</c>. There is therefore no closure-taking helper API — a
///   parameter typed <c>ValueStringBuilder byref -&gt; 'R</c> does not compile at all.
///   </description></item>
///   <item><description>
///   <b>Not caught:</b> passing it by value. A copy shares the same backing array, so if either
///   side grows or disposes, the other is left pointing at memory the pool may have handed to
///   somebody else. This is why no member returns <c>this</c>: fluent chaining would copy.
///   </description></item>
///   <item><description>
///   <b>Not caught:</b> a stack-allocated span outliving its frame. F# has no <c>stackalloc</c>
///   expression, and a span built by hand from <c>NativePtr.stackalloc</c> is invisible to
///   escape analysis — returning one compiles silently. Allocate in the function that uses it,
///   never in a loop.
///   </description></item>
/// </list>
/// <para>
/// It has a plain <c>Dispose()</c> and does not implement <see cref="System.IDisposable"/>, so
/// <c>use</c> does not apply — dispose from a <c>try</c>/<c>finally</c>. Dispose is idempotent.
/// </para>
/// <para>
/// <b>When to use it:</b> a hot path that builds one string per call and consumes it
/// immediately. Measured against <see cref="System.Text.StringBuilder"/> from 16 to 1,048,576
/// characters (<c>ValueStringBuilderBenchmarks</c>): never slower, and a third to a half of the
/// allocations. <b>When not to use it:</b> anywhere the value must escape the building function
/// — which is most code, since it cannot be returned, stored, or passed — or anywhere not hot
/// enough to be worth the danger above. When in doubt use <c>StringBuilder</c>.
/// </para>
/// </remarks>
[<Struct; IsByRefLike; NoComparison; NoEquality>]
type ValueStringBuilder =
    val mutable private _chars: Span<char>
    val mutable private _pos: int
    val mutable private _arrayToReturnToPool: char[]

    /// <summary>
    /// Creates a builder that writes into <paramref name="initialBuffer"/> first — for example
    /// a <c>stackalloc</c>'d span (see the type-level remarks for the exact, verified pattern
    /// and its dangers) — and only rents from the shared <see cref="System.Buffers.ArrayPool{T}"/>
    /// once <paramref name="initialBuffer"/> is full.
    /// </summary>
    /// <param name="initialBuffer">The buffer to write into before growing into the pool.</param>
    new(initialBuffer: Span<char>) =
        { _chars = initialBuffer
          _pos = 0
          _arrayToReturnToPool = null }

    /// <summary>
    /// Creates a builder backed entirely by a pooled array of at least
    /// <paramref name="initialCapacity"/> characters, rented immediately from the shared
    /// <see cref="System.Buffers.ArrayPool{T}"/>. Use this overload when there is no
    /// convenient stack buffer to hand in.
    /// </summary>
    /// <param name="initialCapacity">The minimum initial capacity to rent.</param>
    new(initialCapacity: int) =
        let rented = ArrayPool<char>.Shared.Rent(initialCapacity)
        { _chars = Span<char>(rented)
          _pos = 0
          _arrayToReturnToPool = rented }

    /// The number of characters written so far.
    member this.Length = this._pos

    /// <summary>Appends a single character, growing into the shared pool if the buffer is full.</summary>
    /// <param name="c">The character to append.</param>
    member this.Append(c: char) =
        let pos = this._pos
        if uint32 pos < uint32 this._chars.Length then
            this._chars[pos] <- c
            this._pos <- pos + 1
        else
            this.Grow(1)
            this._chars[this._pos] <- c
            this._pos <- this._pos + 1

    /// <summary>Appends every character of <paramref name="value"/>, growing into the shared pool if needed.</summary>
    /// <param name="value">The characters to append.</param>
    member this.Append(value: ReadOnlySpan<char>) =
        if value.Length > 0 then
            let required = this._pos + value.Length
            if required > this._chars.Length then
                this.Grow(value.Length)
            value.CopyTo(this._chars.Slice(this._pos))
            this._pos <- required

    /// <summary>Appends <paramref name="s"/>. A null string is a no-op, matching <see cref="System.Text.StringBuilder"/>.</summary>
    /// <param name="s">The string to append; null is ignored.</param>
    member this.Append(s: string) =
        if not (isNull s) then
            this.Append(s.AsSpan())

    /// Appends the platform newline sequence (<see cref="System.Environment.NewLine"/>).
    member this.AppendLine() =
        this.Append(Environment.NewLine.AsSpan())

    /// <summary>
    /// Resets the write position to zero without releasing the current buffer, so the same
    /// instance — and, if it already grew, the same rented array — can be reused to build
    /// another string.
    /// </summary>
    member this.Clear() =
        this._pos <- 0

    /// The characters written so far, as a read-only view over the live buffer (no copy).
    /// Only valid until the next <see cref="Append"/>, <see cref="Clear"/>, or <see cref="Dispose"/> call.
    member this.AsSpan() : ReadOnlySpan<char> =
        let written = this._chars.Slice(0, this._pos)
        Span<char>.op_Implicit(written)

    /// <summary>
    /// Attempts to copy the characters written so far into <paramref name="destination"/>,
    /// without allocating a string.
    /// </summary>
    /// <param name="destination">The buffer to copy into.</param>
    /// <param name="charsWritten">Receives the number of characters copied, or 0 if the copy failed.</param>
    /// <returns><c>true</c> if <paramref name="destination"/> was large enough; otherwise <c>false</c>.</returns>
    member this.TryCopyTo(destination: Span<char>, [<System.Runtime.InteropServices.Out>] charsWritten: int byref) : bool =
        let written = this._chars.Slice(0, this._pos)
        if written.TryCopyTo(destination) then
            charsWritten <- this._pos
            true
        else
            charsWritten <- 0
            false

    /// <summary>
    /// Returns the built string, then disposes this builder (matching
    /// <c>System.Text.ValueStringBuilder</c>'s behavior) — do not <see cref="Append"/> after
    /// calling this. A further <see cref="Dispose"/> call remains safe (idempotent).
    /// </summary>
    override this.ToString() : string =
        let written = this._chars.Slice(0, this._pos)
        let result = written.ToString()
        this.Dispose()
        result

    /// <summary>
    /// Returns the rented array (if any) to the shared pool. Safe to call more than once —
    /// every call after the first is a no-op. This type is not <see cref="System.IDisposable"/>
    /// (see the type-level remarks) — call this explicitly from a <c>try</c>/<c>finally</c>,
    /// never via <c>use</c>.
    /// </summary>
    member this.Dispose() =
        let toReturn = this._arrayToReturnToPool
        this._chars <- Span<char>.Empty
        this._pos <- 0
        this._arrayToReturnToPool <- null
        if not (isNull toReturn) then
            ArrayPool<char>.Shared.Return(toReturn)

    /// Grows into a new pooled array at least large enough for <paramref name="additionalCapacityBeyondPos"/>
    /// more characters, copies the written prefix across, and returns the previous pooled
    /// array (if any) exactly once — never the buffer <see cref="_chars"/> still points at
    /// after this call, so a later <see cref="Dispose"/> or <see cref="Grow"/> cannot return
    /// the same array twice.
    /// <param name="additionalCapacityBeyondPos">The minimum extra capacity required beyond the current write position.</param>
    member private this.Grow(additionalCapacityBeyondPos: int) =
        let newCapacity = max (this._pos + additionalCapacityBeyondPos) (this._chars.Length * 2)
        let newArray = ArrayPool<char>.Shared.Rent(newCapacity)
        this._chars.Slice(0, this._pos).CopyTo(Span<char>(newArray))
        let toReturn = this._arrayToReturnToPool
        this._chars <- Span<char>(newArray)
        this._arrayToReturnToPool <- newArray
        if not (isNull toReturn) then
            ArrayPool<char>.Shared.Return(toReturn)
