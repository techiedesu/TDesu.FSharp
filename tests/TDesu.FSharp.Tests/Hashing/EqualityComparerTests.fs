namespace TDesu.FSharp.Tests

open System
open System.Collections.Generic
open NUnit.Framework
open TDesu.FSharp.Operators
open TDesu.FSharp.Hashing

type private Person = { Id: int; Name: string }

[<TestFixture>]
type EqualityComparerTests() =

    [<Test>]
    member _.``create builds a comparer that uses the supplied equality and hash functions``() =
        // ARRANGE
        let comparer =
            EqualityComparer.create
                (fun (a: string) b -> a.ToLowerInvariant() = b.ToLowerInvariant())
                (fun s -> s.ToLowerInvariant().GetHashCode())
        // ACT
        let equalResult = comparer.Equals("Hello", "HELLO")
        let hashA = comparer.GetHashCode "Hello"
        let hashB = comparer.GetHashCode "HELLO"
        // ASSERT
        isTrue equalResult
        equals hashA hashB

    [<Test>]
    member _.``create enables a dictionary lookup that would fail with default equality``() =
        // ARRANGE
        let comparer =
            EqualityComparer.create
                (fun (a: string) b -> a.ToLowerInvariant() = b.ToLowerInvariant())
                (fun s -> s.ToLowerInvariant().GetHashCode())

        let dict = Dictionary<string, int>(comparer)
        dict["Hello"] <- 1
        // ACT
        let found, value = dict.TryGetValue "HELLO"
        // ASSERT
        isTrue found
        equals value 1

    [<Test>]
    member _.``create on an empty dictionary reports every key as not found``() =
        // ARRANGE
        let comparer =
            EqualityComparer.create (fun (a: string) b -> a = b) (fun s -> s.GetHashCode())

        let dict = Dictionary<string, int>(comparer)
        // ACT
        let found, _ = dict.TryGetValue "anything"
        // ASSERT
        isFalse found

    [<Test>]
    member _.``create reports an absent key as not found without disturbing present keys``() =
        // ARRANGE
        let comparer =
            EqualityComparer.create (fun (a: string) b -> a = b) (fun s -> s.GetHashCode())

        let dict = Dictionary<string, int>(comparer)
        dict["a"] <- 1
        // ACT
        let found, _ = dict.TryGetValue "b"
        // ASSERT
        isFalse found
        isTrue (dict.ContainsKey "a")

    [<Test>]
    member _.``create with a null equality function throws when the comparer is actually used``() =
        // ARRANGE
        let comparer =
            EqualityComparer.create (Unchecked.defaultof<string -> string -> bool>) (fun s -> s.GetHashCode())
        // ACT
        let act () = comparer.Equals("a", "b") |> ignore
        // ASSERT
        %Assert.Throws<NullReferenceException>(act)

    [<Test>]
    member _.``createBy treats elements as equal when their projected keys match``() =
        // ARRANGE
        let comparer = EqualityComparer.createBy (fun (p: Person) -> p.Id)
        let a = { Id = 1; Name = "Alice" }
        let b = { Id = 1; Name = "Alicia" }
        // ACT
        let result = comparer.Equals(a, b)
        // ASSERT
        isTrue result

    [<Test>]
    member _.``createBy distinguishes elements with different projected keys``() =
        // ARRANGE
        let comparer = EqualityComparer.createBy (fun (p: Person) -> p.Id)
        let a = { Id = 1; Name = "Alice" }
        let b = { Id = 2; Name = "Alice" }
        // ACT
        let result = comparer.Equals(a, b)
        // ASSERT
        isFalse result

    [<Test>]
    member _.``createBy hashes elements by the projected key alone``() =
        // ARRANGE
        let comparer = EqualityComparer.createBy (fun (p: Person) -> p.Id)
        let a = { Id = 7; Name = "Alice" }
        let b = { Id = 7; Name = "Bob" }
        // ACT
        let hashA = comparer.GetHashCode a
        let hashB = comparer.GetHashCode b
        // ASSERT
        equals hashA hashB

    [<Test>]
    member _.``createBy enables a HashSet dedupe by key, ignoring the rest of the record``() =
        // ARRANGE
        let comparer = EqualityComparer.createBy (fun (p: Person) -> p.Id)
        let set = HashSet<Person>(comparer)
        // ACT
        let addedFirst = set.Add { Id = 1; Name = "Alice" }

        let addedSecond =
            set.Add {
                Id = 1
                Name = "Different name, same id"
            }
        // ASSERT
        isTrue addedFirst
        isFalse addedSecond
        equals set.Count 1

    [<Test>]
    member _.``createBy on an empty HashSet reports every element as not found``() =
        // ARRANGE
        let comparer = EqualityComparer.createBy (fun (p: Person) -> p.Id)
        let set = HashSet<Person>(comparer)
        // ACT
        let found = set.Contains { Id = 1; Name = "Alice" }
        // ASSERT
        isFalse found
