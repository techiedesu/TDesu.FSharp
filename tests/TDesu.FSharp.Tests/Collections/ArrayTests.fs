namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp.Collections

[<TestFixture>]
type ArrayValueOptionTests() =

    [<Test>]
    member _.``valueTryFind on a null array returns ValueNone``() =
        // ARRANGE
        let source: int[] = null
        // ACT
        let result = Array.valueTryFind (fun x -> x > 0) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueTryFind on an empty array returns ValueNone``() =
        // ARRANGE
        let source: int[] = [||]
        // ACT
        let result = Array.valueTryFind (fun x -> x > 0) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueTryFind returns ValueNone when nothing matches``() =
        // ARRANGE
        let source = [| 1; 2; 3 |]
        // ACT
        let result = Array.valueTryFind (fun x -> x > 100) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueTryFind returns the first matching element``() =
        // ARRANGE
        let source = [| 1; 4; 9; 16 |]
        // ACT
        let result = Array.valueTryFind (fun x -> x % 2 = 0) source
        // ASSERT
        equals result (ValueSome 4)

    [<Test>]
    member _.``valueTryFindLast on a null array returns ValueNone``() =
        // ARRANGE
        let source: int[] = null
        // ACT
        let result = Array.valueTryFindLast (fun x -> x > 0) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueTryFindLast on an empty array returns ValueNone``() =
        // ARRANGE
        let source: int[] = [||]
        // ACT
        let result = Array.valueTryFindLast (fun x -> x > 0) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueTryFindLast returns ValueNone when nothing matches``() =
        // ARRANGE
        let source = [| 1; 2; 3 |]
        // ACT
        let result = Array.valueTryFindLast (fun x -> x > 100) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueTryFindLast returns the last matching element, scanning from the end``() =
        // ARRANGE
        let source = [| 1; 4; 9; 16 |]
        // ACT
        let result = Array.valueTryFindLast (fun x -> x % 2 = 0) source
        // ASSERT
        equals result (ValueSome 16)

    [<Test>]
    member _.``valueChooseFirst on a null array returns ValueNone``() =
        // ARRANGE
        let source: string[] = null
        // ACT
        let result = Array.valueChooseFirst (fun (s: string) -> if s.Length > 2 then ValueSome s.Length else ValueNone) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueChooseFirst on an empty array returns ValueNone``() =
        // ARRANGE
        let source: string[] = [||]
        // ACT
        let result = Array.valueChooseFirst (fun (s: string) -> ValueSome s.Length) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueChooseFirst returns ValueNone when the chooser never returns ValueSome``() =
        // ARRANGE
        let source = [| "a"; "bb"; "ccc" |]
        // ACT
        let result = Array.valueChooseFirst (fun (s: string) -> if s.Length > 10 then ValueSome s else ValueNone) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueChooseFirst returns the first ValueSome result``() =
        // ARRANGE
        let source = [| "a"; "bb"; "ccc"; "dddd" |]
        // ACT
        let result = Array.valueChooseFirst (fun (s: string) -> if s.Length >= 2 then ValueSome s else ValueNone) source
        // ASSERT
        equals result (ValueSome "bb")

    [<Test>]
    member _.``valueChooseLast on a null array returns ValueNone``() =
        // ARRANGE
        let source: string[] = null
        // ACT
        let result = Array.valueChooseLast (fun (s: string) -> ValueSome s.Length) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueChooseLast on an empty array returns ValueNone``() =
        // ARRANGE
        let source: string[] = [||]
        // ACT
        let result = Array.valueChooseLast (fun (s: string) -> ValueSome s.Length) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueChooseLast returns ValueNone when the chooser never returns ValueSome``() =
        // ARRANGE
        let source = [| "a"; "bb"; "ccc" |]
        // ACT
        let result = Array.valueChooseLast (fun (s: string) -> if s.Length > 10 then ValueSome s else ValueNone) source
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``valueChooseLast returns the last ValueSome result, scanning from the end``() =
        // ARRANGE
        let source = [| "a"; "bb"; "ccc"; "dddd" |]
        // ACT
        let result = Array.valueChooseLast (fun (s: string) -> if s.Length >= 2 then ValueSome s else ValueNone) source
        // ASSERT
        equals result (ValueSome "dddd")
