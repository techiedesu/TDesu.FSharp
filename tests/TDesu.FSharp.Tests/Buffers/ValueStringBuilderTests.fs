namespace TDesu.FSharp.Tests

open System
open System.Text
open FSharp.NativeInterop
open FsCheck
open NUnit.Framework
open TDesu.FSharp.Buffers

#nowarn "9" // one test exercises the genuine NativePtr.stackalloc pattern from the type's docs.

[<TestFixture>]
type ValueStringBuilderTests() =

    [<Test>]
    member _.``empty builder ToString is empty string``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        equals (sb.ToString()) ""

    [<Test>]
    member _.``Length reflects characters written``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            equals sb.Length 0
            sb.Append('a')
            equals sb.Length 1
            sb.Append("bc")
            equals sb.Length 3
        finally
            sb.Dispose()

    [<Test>]
    member _.``append within the initial buffer never needs the pool``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("hi")
        equals (sb.ToString()) "hi"

    [<Test>]
    member _.``append past the initial span grows into the pool and keeps all content``() =
        let buffer = Array.zeroCreate<char> 4
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("this is longer than four characters")
        equals (sb.ToString()) "this is longer than four characters"

    [<Test>]
    member _.``growth can happen more than once for the same builder``() =
        let buffer = Array.zeroCreate<char> 2
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            for _ in 1..500 do
                sb.Append('x')

            equals sb.Length 500
            equals (sb.ToString().Length) 500
        finally
            sb.Dispose()

    [<Test>]
    member _.``mixed char, string, and ReadOnlySpan appends concatenate in order``() =
        let buffer = Array.zeroCreate<char> 2 // tiny: forces growth mid-sequence
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append('[')
        sb.Append("middle")
        sb.Append(ReadOnlySpan<char>([| 'X'; 'Y'; 'Z' |]))
        sb.Append(']')
        equals (sb.ToString()) "[middleXYZ]"

    [<Test>]
    member _.``Append(null string) is a no-op, matching StringBuilder``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("a")
        sb.Append(null: string)
        sb.Append("b")
        equals (sb.ToString()) "ab"

    [<Test>]
    member _.``Append(empty ReadOnlySpan) is a no-op``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("a")
        sb.Append(ReadOnlySpan<char>.Empty)
        sb.Append("b")
        equals (sb.ToString()) "ab"

    [<Test>]
    member _.``AppendLine appends the platform newline``() =
        let buffer = Array.zeroCreate<char> 16
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("line1")
        sb.AppendLine()
        sb.Append("line2")
        equals (sb.ToString()) ("line1" + Environment.NewLine + "line2")

    [<Test>]
    member _.``Clear resets position and the same instance can be reused``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            sb.Append("first")
            sb.Clear()
            equals sb.Length 0
            sb.Append("second")
            let secondSpan = sb.AsSpan()
            equals (secondSpan.ToString()) "second"
        finally
            sb.Dispose()

    [<Test>]
    member _.``Clear after growth still allows correct reuse of the grown buffer``() =
        let buffer = Array.zeroCreate<char> 2
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            sb.Append("well past the tiny buffer")
            sb.Clear()
            sb.Append("new content")
            equals (sb.ToString()) "new content"
        finally
            sb.Dispose()

    [<Test>]
    member _.``AsSpan reflects exactly the written prefix``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            sb.Append("hello")
            let span = sb.AsSpan()
            isTrue (span.SequenceEqual("hello".AsSpan()))
        finally
            sb.Dispose()

    [<Test>]
    member _.``TryCopyTo succeeds into a large-enough destination``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            sb.Append("copy me")
            let dest = Array.zeroCreate<char> 16
            let mutable written = 0
            let ok = sb.TryCopyTo(Span<char>(dest), &written)
            isTrue ok
            equals written 7
            equals (String(dest, 0, written)) "copy me"
        finally
            sb.Dispose()

    [<Test>]
    member _.``TryCopyTo fails into a too-small destination and reports zero``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))

        try
            sb.Append("too long for dest")
            let dest = Array.zeroCreate<char> 3
            let mutable written = 99
            let ok = sb.TryCopyTo(Span<char>(dest), &written)
            isFalse ok
            equals written 0
        finally
            sb.Dispose()

    [<Test>]
    member _.``Dispose is idempotent after growing into the pool``() =
        let buffer = Array.zeroCreate<char> 2
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("grows past the tiny buffer, so Dispose has a pooled array to return")
        sb.Dispose()
        sb.Dispose() // must not throw and must not double-return the array to the pool
        sb.Dispose()

    [<Test>]
    member _.``Dispose is idempotent when the builder never grew``() =
        let buffer = Array.zeroCreate<char> 8
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("fits")
        sb.Dispose() // no pooled array to return — must still be a harmless no-op
        sb.Dispose()

    [<Test>]
    member _.``ToString disposes as a side effect, and a further Dispose call remains safe``() =
        let buffer = Array.zeroCreate<char> 2
        let mutable sb = ValueStringBuilder(Span<char>(buffer))
        sb.Append("grows past the tiny buffer before ToString")
        let s = sb.ToString()
        equals s "grows past the tiny buffer before ToString"
        sb.Dispose() // idempotent even though ToString() already disposed internally

    [<Test>]
    member _.``pool-only constructor builds correctly without a caller-supplied buffer``() =
        let mutable sb = ValueStringBuilder(4)

        try
            sb.Append("built entirely from the shared pool")
            equals (sb.ToString()) "built entirely from the shared pool"
        finally
            sb.Dispose()

    [<Test>]
    member _.``a genuinely stack-allocated initial buffer round-trips correctly``() =
        // Exercises the exact NativePtr.stackalloc + Span pattern documented on the type:
        // stackalloc'd directly in this method, used only here, never returned or looped.
        let expected = "hello from the stack, past a 4-char buffer"
        let p = NativePtr.stackalloc<char> 4
        let mutable sb = ValueStringBuilder(Span<char>(NativePtr.toVoidPtr p, 4))

        try
            sb.Append(expected)
            equals (sb.ToString()) expected
        finally
            sb.Dispose()

[<TestFixture>]
type ValueStringBuilderPropertyTests() =

    [<Test>]
    member _.``ToString matches StringBuilder for randomized char+string fragments``() =
        let prop (fragments: (char * string) list) =
            let expected = StringBuilder()
            let buffer = Array.zeroCreate<char> 4 // small: forces repeated growth for most inputs
            let mutable sb = ValueStringBuilder(Span<char>(buffer))

            for c, s in fragments do
                expected.Append(c) |> ignore
                sb.Append(c)
                expected.Append(s) |> ignore // StringBuilder.Append(null: string) is a no-op too
                sb.Append(s)

            sb.ToString() = expected.ToString()

        Check.QuickThrowOnFailure prop

    [<Test>]
    member _.``Length always equals the total number of characters appended``() =
        let prop (fragments: string list) =
            let fragments = fragments |> List.map (fun s -> if isNull s then "" else s)
            let buffer = Array.zeroCreate<char> 4
            let mutable sb = ValueStringBuilder(Span<char>(buffer))

            try
                for s in fragments do
                    sb.Append(s)

                sb.Length = (fragments |> List.sumBy String.length)
            finally
                sb.Dispose()

        Check.QuickThrowOnFailure prop

    [<Test>]
    member _.``Clear then append matches building fresh with only the later appends``() =
        let prop (before: string list) (after: string list) =
            let buffer1 = Array.zeroCreate<char> 4
            let mutable cleared = ValueStringBuilder(Span<char>(buffer1))

            for s in before do
                cleared.Append(s)

            cleared.Clear()

            for s in after do
                cleared.Append(s)

            let clearedResult = cleared.ToString()

            let buffer2 = Array.zeroCreate<char> 4
            let mutable fresh = ValueStringBuilder(Span<char>(buffer2))

            for s in after do
                fresh.Append(s)

            let freshResult = fresh.ToString()

            clearedResult = freshResult

        Check.QuickThrowOnFailure prop
