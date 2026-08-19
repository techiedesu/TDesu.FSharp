namespace TDesu.FSharp.Tests

open System
open FsCheck
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Buffers
open TDesu.FSharp.Hashing
open TDesu.FSharp.Concurrency

[<AutoOpen>]
module private PropHelpers =
    let check (p: 'a -> bool) = Check.QuickThrowOnFailure p
    let check2 (p: 'a -> 'b -> bool) = Check.QuickThrowOnFailure p

[<TestFixture>]
type ResultPropertyTests() =

    [<Test>]
    member _.``Result.map id = id``() =
        check (fun (x: Result<int, string>) -> Result.map id x = x)

    [<Test>]
    member _.``Result.map composition``() =
        check (fun (x: Result<int, string>) ->
            let f = (+) 1 in
            let g = (*) 2
            Result.map (f >> g) x = (Result.map f >> Result.map g) x
        )

    [<Test>]
    member _.``Result.bind Ok = id``() =
        check (fun (x: Result<int, string>) -> Result.bind Ok x = x)

    [<Test>]
    member _.``Result.defaultValue on Ok ignores default``() =
        check2 (fun (v: int) (def: int) -> Result.defaultValue def (Ok v) = v)

    [<Test>]
    member _.``Result.defaultValue on Error returns default``() =
        check2 (fun (e: string) (def: int) -> Result.defaultValue def (Error e) = def)

    [<Test>]
    member _.``Result.toOption isSome = isOk``() =
        check (fun (x: Result<int, string>) -> (Result.toOption x).IsSome = Result.isOk x)

    [<Test>]
    member _.``Result.catch never throws``() =
        check (fun (v: int) -> Result.catch (fun () -> v) |> Result.isOk)

[<TestFixture>]
type OptionPropertyTests() =

    [<Test>]
    member _.``Option.map id = id``() =
        check (fun (x: int option) -> Option.map id x = x)

    [<Test>]
    member _.``Option.zip symmetry``() =
        check2 (fun (a: int option) (b: string option) ->
            let z = Option.zip a b

            match a, b with
            | Some _, Some _ -> z.IsSome
            | _ -> z.IsNone
        )

    [<Test>]
    member _.``Option.toResult roundtrip for Some``() =
        check (fun (v: int) -> Result.toOption (Option.toResult "err" (Some v)) = Some v)

    [<Test>]
    member _.``Option.ofBool matches bool``() =
        check (fun (b: bool) -> (Option.ofBool b).IsSome = b)

[<TestFixture>]
type StringPropertyTests() =

    [<Test>]
    member _.``String.trim is idempotent``() =
        check (fun (NonNull s) -> let t = String.trim s in String.trim t = t)

    [<Test>]
    member _.``String.truncate never exceeds n``() =
        check2 (fun (PositiveInt n) (NonNull s) -> (String.truncate n s).Length <= n)

    [<Test>]
    member _.``String.split then join roundtrip``() =
        check (fun (NonNull s) ->
            let parts = s |> String.split ","
            String.join "," parts = s
        )

    [<Test>]
    member _.``String.toOption None for empty``() =
        isTrue (
            String.toOption "" = None
            && String.toOption null = None
            && String.toOption "  " = None
        )

[<TestFixture>]
type BytesPropertyTests() =

    [<Test>]
    member _.``Bytes.concat2 length = sum``() =
        check2 (fun (a: byte[]) (b: byte[]) ->
            if isNull a || isNull b then
                true // skip nulls
            else
                (Bytes.concat2 a b).Length = a.Length + b.Length
        )

    [<Test>]
    member _.``Bytes.xor self = zeros``() =
        check (fun (a: byte[]) -> Bytes.xor a a |> Array.forall ((=) 0uy))

    [<Test>]
    member _.``Bytes.xor is commutative``() =
        check2 (fun (a: byte[]) (b: byte[]) ->
            let n = min a.Length b.Length

            if n > 0 then
                Bytes.xor a[.. n - 1] b[.. n - 1] = Bytes.xor b[.. n - 1] a[.. n - 1]
            else
                true
        )

    [<Test>]
    member _.``Bytes.constantTimeEquals is reflexive``() =
        check (fun (a: byte[]) -> Bytes.constantTimeEquals a a)

    [<Test>]
    member _.``Bytes.slice full array = identity``() =
        check (fun (a: byte[]) -> if a.Length > 0 then Bytes.slice a 0 a.Length = a else true)

[<TestFixture>]
type HashPropertyTests() =

    [<Test>]
    member _.``ContentHash.sha256 is deterministic``() =
        check (fun (a: byte[]) -> ContentHash.sha256 a = ContentHash.sha256 a)

    [<Test>]
    member _.``ContentHash.sha256 is 32 bytes``() =
        check (fun (a: byte[]) -> (ContentHash.sha256 a).Length = 32)

    [<Test>]
    member _.``Hash.ofSeq is deterministic``() =
        check (fun (xs: int list) -> Hash.ofSeq xs = Hash.ofSeq xs)

[<TestFixture>]
type TryParsePropertyTests() =

    [<Test>]
    member _.``Int32.tryParse roundtrip``() =
        check (fun (n: int) -> Int32.tryParse (string n) = Some n)

    [<Test>]
    member _.``Int64.tryParse roundtrip``() =
        check (fun (n: int64) -> Int64.tryParse (string n) = Some n)

    [<Test>]
    member _.``Boolean.tryParse roundtrip``() =
        check (fun (b: bool) -> Boolean.tryParse (string b) = Some b)

    [<Test>]
    member _.``Guid.tryParse roundtrip``() =
        check (fun (g: Guid) -> Guid.tryParse (string g) = Some g)

[<TestFixture>]
type BoundedCollectionPropertyTests() =

    [<Test>]
    member _.``BoundedQueue never exceeds capacity``() =
        check2 (fun (PositiveInt cap) (items: int list) ->
            let cap = min cap 100
            let q = BoundedQueue<int>(cap)

            for item in items do
                q.Enqueue(item)

            q.Count <= cap
        )

    [<Test>]
    member _.``BoundedDict never exceeds capacity``() =
        check2 (fun (PositiveInt cap) (items: (int * int) list) ->
            let cap = min cap 100
            let d = BoundedDict<int, int>(cap)

            for (k, v) in items do
                d.Set(k, v)

            d.Count <= cap
        )

    [<Test>]
    member _.``AtomicInt increment decrement cancel``() =
        check (fun (PositiveInt n) ->
            let n = min n 1000
            let a = AtomicInt(0)

            for _ in 1..n do
                a.Increment() |> ignore

            for _ in 1..n do
                a.Decrement() |> ignore

            a.Value = 0
        )

    [<Test>]
    member _.``SnapshotThrottle triggers at threshold``() =
        check (fun (PositiveInt threshold) ->
            let t = min threshold 100
            let snap = SnapshotThrottle(t)
            let mutable triggered = false

            for _ in 1..t do
                if snap.Record() then
                    triggered <- true

            triggered
        )
