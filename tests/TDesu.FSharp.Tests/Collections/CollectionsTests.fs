namespace TDesu.FSharp.Tests

open System
open System.Collections.Generic
open System.IO
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Collections

[<TestFixture>]
type DictionaryHardeningTests() =

    [<Test>]
    member _.``getValue on null dictionary throws KeyNotFoundException, not NullReferenceException``() =
        // ARRANGE
        let d: Dictionary<string, int> = null
        // ACT
        let act () = Dictionary.getValue "missing" d |> ignore
        // ASSERT
        % Assert.Throws<KeyNotFoundException>(act)

    [<Test>]
    member _.``getValue on empty dictionary throws KeyNotFoundException``() =
        // ARRANGE
        let d = Dictionary<string, int>()
        // ACT
        let act () = Dictionary.getValue "missing" d |> ignore
        // ASSERT
        % Assert.Throws<KeyNotFoundException>(act)

    [<Test>]
    member _.``tryGetValue on null dictionary returns None``() =
        // ARRANGE
        let d: Dictionary<string, int> = null
        // ACT
        let result = Dictionary.tryGetValue "x" d
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryGetValue on empty dictionary returns None``() =
        // ARRANGE
        let d = Dictionary<string, int>()
        // ACT
        let result = Dictionary.tryGetValue "x" d
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryGetValueV on null dictionary returns ValueNone``() =
        // ARRANGE
        let d: Dictionary<string, int> = null
        // ACT
        let result = Dictionary.tryGetValueV "x" d
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``getOrDefault on null dictionary returns the default``() =
        // ARRANGE
        let d: Dictionary<string, int> = null
        // ACT
        let result = Dictionary.getOrDefault "x" 99 d
        // ASSERT
        equals result 99

    [<Test>]
    member _.``getOrDefault on empty dictionary returns the default``() =
        // ARRANGE
        let d = Dictionary<string, int>()
        // ACT
        let result = Dictionary.getOrDefault "x" 99 d
        // ASSERT
        equals result 99


[<TestFixture>]
type StackHardeningTests() =

    [<Test>]
    member _.``tryPeek on null stack returns None``() =
        // ARRANGE
        let stack: Stack<int> = null
        // ACT
        let result = Stack.tryPeek stack
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryPeek on empty stack returns None``() =
        // ARRANGE
        let stack = Stack<int>()
        // ACT
        let result = Stack.tryPeek stack
        // ASSERT
        isNone result

    [<Test>]
    member _.``pop on null stack throws InvalidOperationException, not NullReferenceException``() =
        // ARRANGE
        let stack: Stack<int> = null
        // ACT
        let act () = Stack.pop stack |> ignore
        // ASSERT
        % Assert.Throws<InvalidOperationException>(act)

    [<Test>]
    member _.``pop on empty stack throws InvalidOperationException``() =
        // ARRANGE
        let stack = Stack<int>()
        // ACT
        let act () = Stack.pop stack |> ignore
        // ASSERT
        % Assert.Throws<InvalidOperationException>(act)

    [<Test>]
    member _.``push onto null stack throws ArgumentNullException``() =
        // ARRANGE
        let stack: Stack<int> = null
        // ACT
        let act () = Stack.push 1 stack
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``reverse of null stack yields an empty stack``() =
        // ARRANGE
        let stack: Stack<int> = null
        // ACT
        let result = Stack.reverse stack
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``reverse preserves push order as pop order``() =
        // ARRANGE
        let stack = Stack<int>()
        stack.Push(1)
        stack.Push(2)
        stack.Push(3)
        // ACT
        let result = Stack.reverse stack
        // ASSERT
        equals (result |> Seq.toList) [ 1; 2; 3 ]


[<TestFixture>]
type SeqHardeningTests() =

    [<Test>]
    member _.``tryMax on null sequence returns None``() =
        // ARRANGE
        let source: int seq = null
        // ACT
        let result = Seq.tryMax source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryMin on null sequence returns None``() =
        // ARRANGE
        let source: int seq = null
        // ACT
        let result = Seq.tryMin source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryAverage on null sequence returns None``() =
        // ARRANGE
        let source: float seq = null
        // ACT
        let result = Seq.tryAverage source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryMaxBy on null sequence returns None``() =
        // ARRANGE
        let source: int seq = null
        // ACT
        let result = Seq.tryMaxBy id source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryMaxBy on empty sequence returns None``() =
        // ARRANGE
        let source = Seq.empty<int>
        // ACT
        let result = Seq.tryMaxBy id source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryMaxBy on single-element sequence returns that element``() =
        // ARRANGE
        let source = [ 42 ]
        // ACT
        let result = Seq.tryMaxBy id source
        // ASSERT
        isSome 42 result

    [<Test>]
    member _.``tryMaxBy with tied keys returns the first element achieving the maximum``() =
        // ARRANGE
        let source = [ (1, "z"); (2, "z"); (3, "a") ]
        // ACT
        let result = Seq.tryMaxBy snd source
        // ASSERT
        isSome (1, "z") result

    [<Test>]
    member _.``tryMaxBy enumerates the source exactly once``() =
        // ARRANGE
        let mutable enumerations = 0
        let source =
            seq {
                for x in [ 3; 1; 4; 1; 5; 9; 2; 6 ] do
                    enumerations <- enumerations + 1
                    yield x
            }
        // ACT
        let result = Seq.tryMaxBy id source
        // ASSERT
        isSome 9 result
        equals enumerations 8

    [<Test>]
    member _.``tryMaxBy propagates an exception thrown by the projection``() =
        // ARRANGE
        let source = [ 1; 2; 3 ]
        // ACT
        let act () = Seq.tryMaxBy (fun x -> if x = 2 then failwith "boom" else x) source |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)

    [<Test>]
    member _.``tryMaxBy disposes the enumerator even when the projection throws``() =
        // ARRANGE
        let mutable disposed = false
        let source: int seq =
            seq {
                try
                    yield! [ 1; 2; 3 ]
                finally
                    disposed <- true
            }
        // ACT
        let act () = Seq.tryMaxBy (fun x -> if x = 2 then failwith "boom" else x) source |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)
        isTrue disposed

    [<Test>]
    member _.``tryMinBy on null sequence returns None``() =
        // ARRANGE
        let source: int seq = null
        // ACT
        let result = Seq.tryMinBy id source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryMinBy on empty sequence returns None``() =
        // ARRANGE
        let source = Seq.empty<int>
        // ACT
        let result = Seq.tryMinBy id source
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryMinBy on single-element sequence returns that element``() =
        // ARRANGE
        let source = [ 42 ]
        // ACT
        let result = Seq.tryMinBy id source
        // ASSERT
        isSome 42 result

    [<Test>]
    member _.``tryMinBy with tied keys returns the first element achieving the minimum``() =
        // ARRANGE
        let source = [ (1, "a"); (2, "a"); (3, "z") ]
        // ACT
        let result = Seq.tryMinBy snd source
        // ASSERT
        isSome (1, "a") result

    [<Test>]
    member _.``tryMinBy propagates an exception thrown by the projection``() =
        // ARRANGE
        let source = [ 1; 2; 3 ]
        // ACT
        let act () = Seq.tryMinBy (fun x -> if x = 2 then failwith "boom" else x) source |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)


[<TestFixture>]
type ArrayMemoryStreamHardeningTests() =

    [<Test>]
    member _.``Array.ofMemoryStream on null stream returns an empty array``() =
        // ARRANGE
        let ms: MemoryStream = null
        // ACT
        let result = Array.ofMemoryStream ms
        // ASSERT
        equals result [||]

    [<Test>]
    member _.``Array.ofMemoryStream on empty stream returns an empty array``() =
        // ARRANGE
        use ms = new MemoryStream()
        // ACT
        let result = Array.ofMemoryStream ms
        // ASSERT
        equals result [||]

    [<Test>]
    member _.``MemoryStream.reset on null stream throws ArgumentNullException``() =
        // ARRANGE
        let ms: MemoryStream = null
        // ACT
        let act () = MemoryStream.reset ms
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``MemoryStream.reset moves the position back to zero``() =
        // ARRANGE
        use ms = new MemoryStream([| 1uy; 2uy; 3uy |])
        ms.Position <- 2L
        // ACT
        MemoryStream.reset ms
        // ASSERT
        equals ms.Position 0L


[<TestFixture>]
type ListHardeningTests() =

    [<Test>]
    member _.``ofStack on null stack returns an empty list``() =
        // ARRANGE
        let stack: Stack<int> = null
        // ACT
        let result = List.ofStack stack
        // ASSERT
        equals result []

    [<Test>]
    member _.``List.tryMax on a null list reference returns None``() =
        // ARRANGE
        let xs: int list = Unchecked.defaultof<int list>
        // ACT
        let result = List.tryMax xs
        // ASSERT
        isNone result

    [<Test>]
    member _.``List.tryMax on an empty list returns None``() =
        // ARRANGE
        let xs: int list = []
        // ACT
        let result = List.tryMax xs
        // ASSERT
        isNone result

    [<Test>]
    member _.``List.tryMin on a null list reference returns None``() =
        // ARRANGE
        let xs: int list = Unchecked.defaultof<int list>
        // ACT
        let result = List.tryMin xs
        // ASSERT
        isNone result


[<TestFixture>]
type ResizeArrayExistingHardeningTests() =

    [<Test>]
    member _.``ofSeq on a null sequence returns an empty ResizeArray``() =
        // ARRANGE
        let source: int seq = null
        // ACT
        let result = ResizeArray.ofSeq source
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``ofList on a null list reference returns an empty ResizeArray``() =
        // ARRANGE
        let source: int list = Unchecked.defaultof<int list>
        // ACT
        let result = ResizeArray.ofList source
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``ofArray on a null array returns an empty ResizeArray``() =
        // ARRANGE
        let source: int[] = null
        // ACT
        let result = ResizeArray.ofArray source
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``add on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.add 1 ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``addRange on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.addRange [ 1; 2 ] ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``addRange with a null items source adds nothing``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2 ])
        let items: int seq = null
        // ACT
        let result = ResizeArray.addRange items ra
        // ASSERT
        equals (result |> Seq.toList) [ 1; 2 ]

    [<Test>]
    member _.``map on a null ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.map (fun x -> x * 2) ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``filter on a null ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.filter (fun x -> x > 0) ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``iter on a null ResizeArray does not invoke the action``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        let mutable calls = 0
        // ACT
        ResizeArray.iter (fun _ -> calls <- calls + 1) ra
        // ASSERT
        equals calls 0

    [<Test>]
    member _.``iteri on a null ResizeArray does not invoke the action``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        let mutable calls = 0
        // ACT
        ResizeArray.iteri (fun _ _ -> calls <- calls + 1) ra
        // ASSERT
        equals calls 0

    [<Test>]
    member _.``exists on a null ResizeArray returns false``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.exists (fun x -> x > 0) ra
        // ASSERT
        isFalse result

    [<Test>]
    member _.``tryFind on a null ResizeArray returns None``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.tryFind (fun x -> x > 0) ra
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryItem on a null ResizeArray returns None``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.tryItem 0 ra
        // ASSERT
        isNone result

    [<Test>]
    member _.``toList on a null ResizeArray returns an empty list``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.toList ra
        // ASSERT
        equals result []

    [<Test>]
    member _.``toArray on a null ResizeArray returns an empty array``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.toArray ra
        // ASSERT
        equals result [||]

    [<Test>]
    member _.``count on a null ResizeArray returns zero``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.count ra
        // ASSERT
        equals result 0

    [<Test>]
    member _.``isEmpty on a null ResizeArray returns true``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.isEmpty ra
        // ASSERT
        isTrue result

    [<Test>]
    member _.``sort on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.sort ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``sortWith on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.sortWith compare ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``sortBy on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.sortBy id ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``removeWhere on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.removeWhere (fun x -> x > 0) ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``clear on a null ResizeArray throws ArgumentNullException``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let act () = ResizeArray.clear ra |> ignore
        // ASSERT
        % Assert.Throws<ArgumentNullException>(act)

    [<Test>]
    member _.``fold on a null ResizeArray returns the initial state``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.fold (+) 10 ra
        // ASSERT
        equals result 10

    [<Test>]
    member _.``joinWith on a null ResizeArray returns an empty string``() =
        // ARRANGE
        let ra: ResizeArray<string> = null
        // ACT
        let result = ResizeArray.joinWith ", " ra
        // ASSERT
        equals result ""


[<TestFixture>]
type ResizeArrayNewFunctionsTests() =

    [<Test>]
    member _.``choose keeps only the Some results``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3; 4; 5 ])
        // ACT
        let result = ResizeArray.choose (fun x -> if x % 2 = 0 then Some(x * 10) else None) ra
        // ASSERT
        equals (result |> Seq.toList) [ 20; 40 ]

    [<Test>]
    member _.``choose returns empty when the chooser returns None for everything``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let result = ResizeArray.choose (fun _ -> None) ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``choose on a null ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.choose Some ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``choose on an empty ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra = ResizeArray<int>()
        // ACT
        let result = ResizeArray.choose Some ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``choose propagates an exception thrown by the chooser``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let act () = ResizeArray.choose (fun x -> if x = 2 then failwith "boom" else Some x) ra |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)

    [<Test>]
    member _.``mapi maps each element together with its index``() =
        // ARRANGE
        let ra = ResizeArray<string>([ "a"; "b"; "c" ])
        // ACT
        let result = ResizeArray.mapi (fun i x -> $"{i}:{x}") ra
        // ASSERT
        equals (result |> Seq.toList) [ "0:a"; "1:b"; "2:c" ]

    [<Test>]
    member _.``mapi on a null ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.mapi (fun i x -> i + x) ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``mapi on an empty ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra = ResizeArray<int>()
        // ACT
        let result = ResizeArray.mapi (fun i x -> i + x) ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``rev reverses element order``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3; 4; 5 ])
        // ACT
        let result = ResizeArray.rev ra
        // ASSERT
        equals (result |> Seq.toList) [ 5; 4; 3; 2; 1 ]

    [<Test>]
    member _.``rev on a null ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.rev ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``rev on an empty ResizeArray returns an empty ResizeArray``() =
        // ARRANGE
        let ra = ResizeArray<int>()
        // ACT
        let result = ResizeArray.rev ra
        // ASSERT
        equals (result |> Seq.toList) []

    [<Test>]
    member _.``partition splits elements by the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3; 4; 5 ])
        // ACT
        let matching, rest = ResizeArray.partition (fun x -> x % 2 = 0) ra
        // ASSERT
        equals (matching |> Seq.toList) [ 2; 4 ]
        equals (rest |> Seq.toList) [ 1; 3; 5 ]

    [<Test>]
    member _.``partition puts every element on the matching side when all satisfy the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let matching, rest = ResizeArray.partition (fun _ -> true) ra
        // ASSERT
        equals (matching |> Seq.toList) [ 1; 2; 3 ]
        equals (rest |> Seq.toList) []

    [<Test>]
    member _.``partition puts every element on the non-matching side when none satisfy the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let matching, rest = ResizeArray.partition (fun _ -> false) ra
        // ASSERT
        equals (matching |> Seq.toList) []
        equals (rest |> Seq.toList) [ 1; 2; 3 ]

    [<Test>]
    member _.``partition on a null ResizeArray returns two empty ResizeArrays``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let matching, rest = ResizeArray.partition (fun x -> x > 0) ra
        // ASSERT
        equals (matching |> Seq.toList) []
        equals (rest |> Seq.toList) []

    [<Test>]
    member _.``partition on an empty ResizeArray returns two empty ResizeArrays``() =
        // ARRANGE
        let ra = ResizeArray<int>()
        // ACT
        let matching, rest = ResizeArray.partition (fun x -> x > 0) ra
        // ASSERT
        equals (matching |> Seq.toList) []
        equals (rest |> Seq.toList) []

    [<Test>]
    member _.``partition propagates an exception thrown by the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let act () = ResizeArray.partition (fun x -> if x = 2 then failwith "boom" else true) ra |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)

    [<Test>]
    member _.``tryFindIndex returns the index of the first match``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 5; 3; 9; 3 ])
        // ACT
        let result = ResizeArray.tryFindIndex (fun x -> x = 3) ra
        // ASSERT
        isSome 1 result

    [<Test>]
    member _.``tryFindIndex returns None when nothing matches``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 5; 3; 9 ])
        // ACT
        let result = ResizeArray.tryFindIndex (fun x -> x = 999) ra
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryFindIndex on a null ResizeArray returns None``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.tryFindIndex (fun x -> x > 0) ra
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryFindIndex on an empty ResizeArray returns None``() =
        // ARRANGE
        let ra = ResizeArray<int>()
        // ACT
        let result = ResizeArray.tryFindIndex (fun x -> x > 0) ra
        // ASSERT
        isNone result

    [<Test>]
    member _.``tryFindIndex propagates an exception thrown by the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let act () = ResizeArray.tryFindIndex (fun x -> if x = 2 then failwith "boom" else false) ra |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)

    [<Test>]
    member _.``forall returns true when every element satisfies the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 2; 4; 6 ])
        // ACT
        let result = ResizeArray.forall (fun x -> x % 2 = 0) ra
        // ASSERT
        isTrue result

    [<Test>]
    member _.``forall returns false when one element fails the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 2; 4; 5 ])
        // ACT
        let result = ResizeArray.forall (fun x -> x % 2 = 0) ra
        // ASSERT
        isFalse result

    [<Test>]
    member _.``forall on a null ResizeArray is vacuously true``() =
        // ARRANGE
        let ra: ResizeArray<int> = null
        // ACT
        let result = ResizeArray.forall (fun x -> x > 0) ra
        // ASSERT
        isTrue result

    [<Test>]
    member _.``forall on an empty ResizeArray is vacuously true``() =
        // ARRANGE
        let ra = ResizeArray<int>()
        // ACT
        let result = ResizeArray.forall (fun x -> x > 0) ra
        // ASSERT
        isTrue result

    [<Test>]
    member _.``forall propagates an exception thrown by the predicate``() =
        // ARRANGE
        let ra = ResizeArray<int>([ 1; 2; 3 ])
        // ACT
        let act () = ResizeArray.forall (fun x -> if x = 2 then failwith "boom" else true) ra |> ignore
        // ASSERT
        % Assert.Throws<Exception>(act)
