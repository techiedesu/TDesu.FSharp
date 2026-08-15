namespace TDesu.FSharp.Tests

open NUnit.Framework
open TDesu.FSharp

[<TestFixture>]
type StringMatchingTests() =

    // ---- startsWithAny (string[]) ----

    [<Test>]
    member _.``startsWithAny null subject string returns false``() =
        // ARRANGE
        let values = [| "foo"; "bar" |]
        // ACT
        let result = String.startsWithAny values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``startsWithAny null values array returns false``() =
        // ARRANGE
        let values: string[] = null
        // ACT
        let result = String.startsWithAny values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``startsWithAny empty values array returns false``() =
        // ARRANGE
        let values: string[] = [||]
        // ACT
        let result = String.startsWithAny values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``startsWithAny values array containing null element is skipped not thrown``() =
        // ARRANGE
        let values = [| null; "foo" |]
        // ACT
        let result = String.startsWithAny values "foobar"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``startsWithAny values array containing only a null element never matches``() =
        // ARRANGE
        let values = [| null |]
        // ACT
        let result = String.startsWithAny values "foobar"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``startsWithAny empty-string subject returns false for non-empty prefix``() =
        // ARRANGE
        let values = [| "foo" |]
        // ACT
        let result = String.startsWithAny values ""
        // ASSERT
        isFalse result

    // ---- equalsAny (string[], mandatory StringComparison) ----

    [<Test>]
    member _.``equalsAny null subject string returns false``() =
        // ARRANGE
        let values = [| "a"; "b" |]
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAny null values array returns false``() =
        // ARRANGE
        let values: string[] = null
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values "a"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAny empty values array returns false``() =
        // ARRANGE
        let values: string[] = [||]
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values "a"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAny values array containing null element does not throw and finds real match``() =
        // ARRANGE
        let values = [| null; "a" |]
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values "a"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``equalsAny null element in values never matches a null subject``() =
        // ARRANGE
        let values = [| null |]
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAny empty-string subject matches only an empty candidate``() =
        // ARRANGE
        let values = [| ""; "x" |]
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values ""
        // ASSERT
        isTrue result

    [<Test>]
    member _.``equalsAny is case-sensitive under Ordinal comparison``() =
        // ARRANGE
        let values = [| "B" |]
        // ACT
        let result = String.equalsAny System.StringComparison.Ordinal values "b"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAny honors case-insensitive comparison``() =
        // ARRANGE
        let values = [| "B" |]
        // ACT
        let result = String.equalsAny System.StringComparison.OrdinalIgnoreCase values "b"
        // ASSERT
        isTrue result

    // ---- containsAny (string[], mandatory StringComparison) ----

    [<Test>]
    member _.``containsAny null subject string returns false``() =
        // ARRANGE
        let values = [| "a" |]
        // ACT
        let result = String.containsAny System.StringComparison.Ordinal values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAny null values array returns false``() =
        // ARRANGE
        let values: string[] = null
        // ACT
        let result = String.containsAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAny empty values array returns false``() =
        // ARRANGE
        let values: string[] = [||]
        // ACT
        let result = String.containsAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAny values array containing null element is skipped not thrown``() =
        // ARRANGE
        let values = [| null; "b" |]
        // ACT
        let result = String.containsAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``containsAny empty-string subject returns false for non-empty candidate``() =
        // ARRANGE
        let values = [| "a" |]
        // ACT
        let result = String.containsAny System.StringComparison.Ordinal values ""
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAny matches substring anywhere in subject``() =
        // ARRANGE
        let values = [| "x"; "b" |]
        // ACT
        let result = String.containsAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``containsAny honors case-insensitive comparison``() =
        // ARRANGE
        let values = [| "B" |]
        // ACT
        let result = String.containsAny System.StringComparison.OrdinalIgnoreCase values "abc"
        // ASSERT
        isTrue result

    // ---- endsWithAny (string[], mandatory StringComparison) ----

    [<Test>]
    member _.``endsWithAny null subject string returns false``() =
        // ARRANGE
        let values = [| "c" |]
        // ACT
        let result = String.endsWithAny System.StringComparison.Ordinal values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAny null values array returns false``() =
        // ARRANGE
        let values: string[] = null
        // ACT
        let result = String.endsWithAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAny empty values array returns false``() =
        // ARRANGE
        let values: string[] = [||]
        // ACT
        let result = String.endsWithAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAny values array containing null element is skipped not thrown``() =
        // ARRANGE
        let values = [| null; "bc" |]
        // ACT
        let result = String.endsWithAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``endsWithAny empty-string subject returns false for non-empty suffix``() =
        // ARRANGE
        let values = [| "c" |]
        // ACT
        let result = String.endsWithAny System.StringComparison.Ordinal values ""
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAny matches suffix``() =
        // ARRANGE
        let values = [| "x"; "bc" |]
        // ACT
        let result = String.endsWithAny System.StringComparison.Ordinal values "abc"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``endsWithAny honors case-insensitive comparison``() =
        // ARRANGE
        let values = [| "BC" |]
        // ACT
        let result = String.endsWithAny System.StringComparison.OrdinalIgnoreCase values "abc"
        // ASSERT
        isTrue result

    // ---- equalsAnyChar / containsAnyChar / endsWithAnyChar (char[]) ----

    [<Test>]
    member _.``equalsAnyChar null subject string returns false``() =
        // ARRANGE
        let values = [| 'a'; 'b' |]
        // ACT
        let result = String.equalsAnyChar values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAnyChar null values array returns false``() =
        // ARRANGE
        let values: char[] = null
        // ACT
        let result = String.equalsAnyChar values "a"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAnyChar empty values array returns false``() =
        // ARRANGE
        let values: char[] = [||]
        // ACT
        let result = String.equalsAnyChar values "a"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAnyChar empty-string subject returns false``() =
        // ARRANGE
        let values = [| 'a' |]
        // ACT
        let result = String.equalsAnyChar values ""
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAnyChar multi-char subject never matches``() =
        // ARRANGE
        let values = [| 'a' |]
        // ACT
        let result = String.equalsAnyChar values "ab"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``equalsAnyChar matches single-char subject``() =
        // ARRANGE
        let values = [| 'a'; 'b' |]
        // ACT
        let result = String.equalsAnyChar values "a"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``containsAnyChar null subject string returns false``() =
        // ARRANGE
        let values = [| 'a' |]
        // ACT
        let result = String.containsAnyChar values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAnyChar null values array returns false``() =
        // ARRANGE
        let values: char[] = null
        // ACT
        let result = String.containsAnyChar values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAnyChar empty-string subject returns false``() =
        // ARRANGE
        let values = [| 'a' |]
        // ACT
        let result = String.containsAnyChar values ""
        // ASSERT
        isFalse result

    [<Test>]
    member _.``containsAnyChar matches char anywhere in subject``() =
        // ARRANGE
        let values = [| 'x'; 'b' |]
        // ACT
        let result = String.containsAnyChar values "abc"
        // ASSERT
        isTrue result

    [<Test>]
    member _.``endsWithAnyChar null subject string returns false``() =
        // ARRANGE
        let values = [| 'a' |]
        // ACT
        let result = String.endsWithAnyChar values null
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAnyChar null values array returns false``() =
        // ARRANGE
        let values: char[] = null
        // ACT
        let result = String.endsWithAnyChar values "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAnyChar empty-string subject returns false``() =
        // ARRANGE
        let values = [| 'a' |]
        // ACT
        let result = String.endsWithAnyChar values ""
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWithAnyChar matches last char``() =
        // ARRANGE
        let values = [| 'x'; 'c' |]
        // ACT
        let result = String.endsWithAnyChar values "abc"
        // ASSERT
        isTrue result

    // ---- countOccurrences null-hardening ----

    [<Test>]
    member _.``countOccurrences null subject string returns zero``() =
        // ARRANGE
        let subject: string = null
        // ACT
        let result = String.countOccurrences "a" subject
        // ASSERT
        equals result 0

    [<Test>]
    member _.``countOccurrences null substr returns zero``() =
        // ARRANGE
        let substr: string = null
        // ACT
        let result = String.countOccurrences substr "abc"
        // ASSERT
        equals result 0

    [<Test>]
    member _.``countOccurrences empty subject returns zero``() =
        // ARRANGE
        let subject = ""
        // ACT
        let result = String.countOccurrences "a" subject
        // ASSERT
        equals result 0

    [<Test>]
    member _.``countOccurrences empty substr returns zero``() =
        // ARRANGE
        let substr = ""
        // ACT
        let result = String.countOccurrences substr "abc"
        // ASSERT
        equals result 0

    // ---- replaceEndLines null-hardening ----

    [<Test>]
    member _.``replaceEndLines null subject string returns null``() =
        // ARRANGE
        let subject: string = null
        // ACT
        let result = String.replaceEndLines "|" subject
        // ASSERT
        equals result null

    [<Test>]
    member _.``replaceEndLines null replacement text removes line endings``() =
        // ARRANGE
        let subject = "a\nb"
        // ACT
        let result = String.replaceEndLines null subject
        // ASSERT
        equals result "ab"

    [<Test>]
    member _.``replaceEndLines normalizes mixed line endings``() =
        // ARRANGE
        let subject = "a\r\nb\nc\rd"
        // ACT
        let result = String.replaceEndLines "|" subject
        // ASSERT
        equals result "a|b|c|d"

    // ---- join null-hardening ----

    [<Test>]
    member _.``join null values sequence returns empty string``() =
        // ARRANGE
        let values: string[] = null
        // ACT
        let result = String.join "," values
        // ASSERT
        equals result ""

    [<Test>]
    member _.``join empty sequence returns empty string``() =
        // ARRANGE
        let values: string seq = Seq.empty
        // ACT
        let result = String.join "," values
        // ASSERT
        equals result ""

    [<Test>]
    member _.``join positive case joins with separator``() =
        // ARRANGE
        let values = [ "a"; "b"; "c" ]
        // ACT
        let result = String.join "," values
        // ASSERT
        equals result "a,b,c"

    // ---- startsWith / contains / endsWith (singular) null-hardening ----

    [<Test>]
    member _.``startsWith null subject string returns false``() =
        // ARRANGE
        let subject: string = null
        // ACT
        let result = String.startsWith "a" subject
        // ASSERT
        isFalse result

    [<Test>]
    member _.``startsWith null value returns false``() =
        // ARRANGE
        let value: string = null
        // ACT
        let result = String.startsWith value "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``startsWith matches positive case``() =
        // ARRANGE
        let subject = "abc"
        // ACT
        let result = String.startsWith "ab" subject
        // ASSERT
        isTrue result

    [<Test>]
    member _.``contains null subject string returns false``() =
        // ARRANGE
        let subject: string = null
        // ACT
        let result = String.contains "a" subject
        // ASSERT
        isFalse result

    [<Test>]
    member _.``contains null value returns false``() =
        // ARRANGE
        let value: string = null
        // ACT
        let result = String.contains value "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``contains matches positive case``() =
        // ARRANGE
        let subject = "abc"
        // ACT
        let result = String.contains "b" subject
        // ASSERT
        isTrue result

    [<Test>]
    member _.``endsWith null subject string returns false``() =
        // ARRANGE
        let subject: string = null
        // ACT
        let result = String.endsWith "a" subject
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWith null value returns false``() =
        // ARRANGE
        let value: string = null
        // ACT
        let result = String.endsWith value "abc"
        // ASSERT
        isFalse result

    [<Test>]
    member _.``endsWith matches positive case``() =
        // ARRANGE
        let subject = "abc"
        // ACT
        let result = String.endsWith "bc" subject
        // ASSERT
        isTrue result

[<TestFixture>]
type OptionConstructorTests() =

    // ---- Option.tryCast ----

    [<Test>]
    member _.``Option tryCast returns Some for a matching type``() =
        // ARRANGE
        let boxed: obj = box "hello"
        // ACT
        let result = Option.tryCast<string> boxed
        // ASSERT
        isSome "hello" result

    [<Test>]
    member _.``Option tryCast returns None for a mismatched type``() =
        // ARRANGE
        let boxed: obj = box "hello"
        // ACT
        let result = Option.tryCast<int> boxed
        // ASSERT
        isNone result

    [<Test>]
    member _.``Option tryCast returns None for null input``() =
        // ARRANGE
        let boxed: obj = null
        // ACT
        let result = Option.tryCast<string> boxed
        // ASSERT
        isNone result

    [<Test>]
    member _.``Option tryCast returns None for null input against a value-type target``() =
        // ARRANGE
        let boxed: obj = null
        // ACT
        let result = Option.tryCast<int> boxed
        // ASSERT
        isNone result

    [<Test>]
    member _.``Option tryCast preserves a boxed value type``() =
        // ARRANGE
        let boxed: obj = box 42
        // ACT
        let result = Option.tryCast<int> boxed
        // ASSERT
        isSome 42 result

    // ---- Option.ofPredicate ----

    [<Test>]
    member _.``Option ofPredicate returns Some when predicate is true``() =
        // ARRANGE
        let predicate x = x > 0
        // ACT
        let result = Option.ofPredicate predicate 5
        // ASSERT
        isSome 5 result

    [<Test>]
    member _.``Option ofPredicate returns None when predicate is false``() =
        // ARRANGE
        let predicate x = x > 0
        // ACT
        let result = Option.ofPredicate predicate -5
        // ASSERT
        isNone result

    [<Test>]
    member _.``Option ofPredicate propagates exception thrown by predicate``() =
        // ARRANGE
        let throwing (_: int) : bool = raise (System.InvalidOperationException("boom"))
        // ACT
        let ex = Assert.Throws<System.InvalidOperationException>(fun () -> Option.ofPredicate throwing 5 |> ignore)
        // ASSERT
        equals ex.Message "boom"

    [<Test>]
    member _.``Option ofPredicate throws NullReferenceException when predicate itself is null``() =
        // ARRANGE
        let nullPredicate: int -> bool = Unchecked.defaultof<_>
        // ACT
        let ex = Assert.Throws<System.NullReferenceException>(fun () -> Option.ofPredicate nullPredicate 5 |> ignore)
        // ASSERT
        equals (ex.GetType()) typeof<System.NullReferenceException>

    // ---- ValueOption.tryCast ----

    [<Test>]
    member _.``ValueOption tryCast returns ValueSome for a matching type``() =
        // ARRANGE
        let boxed: obj = box "hello"
        // ACT
        let result = ValueOption.tryCast<string> boxed
        // ASSERT
        equals result (ValueSome "hello")

    [<Test>]
    member _.``ValueOption tryCast returns ValueNone for a mismatched type``() =
        // ARRANGE
        let boxed: obj = box "hello"
        // ACT
        let result = ValueOption.tryCast<int> boxed
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``ValueOption tryCast returns ValueNone for null input``() =
        // ARRANGE
        let boxed: obj = null
        // ACT
        let result = ValueOption.tryCast<string> boxed
        // ASSERT
        equals result ValueNone

    // ---- ValueOption.ofPredicate ----

    [<Test>]
    member _.``ValueOption ofPredicate returns ValueSome when predicate is true``() =
        // ARRANGE
        let predicate x = x > 0
        // ACT
        let result = ValueOption.ofPredicate predicate 5
        // ASSERT
        equals result (ValueSome 5)

    [<Test>]
    member _.``ValueOption ofPredicate returns ValueNone when predicate is false``() =
        // ARRANGE
        let predicate x = x > 0
        // ACT
        let result = ValueOption.ofPredicate predicate -5
        // ASSERT
        equals result ValueNone

    [<Test>]
    member _.``ValueOption ofPredicate propagates exception thrown by predicate``() =
        // ARRANGE
        let throwing (_: int) : bool = raise (System.InvalidOperationException("boom"))
        // ACT
        let ex = Assert.Throws<System.InvalidOperationException>(fun () -> ValueOption.ofPredicate throwing 5 |> ignore)
        // ASSERT
        equals ex.Message "boom"
