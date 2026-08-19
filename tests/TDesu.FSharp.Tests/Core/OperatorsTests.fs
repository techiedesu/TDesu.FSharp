namespace TDesu.FSharp.Tests

open System
open NUnit.Framework
open TDesu.FSharp
open TDesu.FSharp.Operators

[<TestFixture>]
type OperatorTests() =

    [<Test>]
    member _.``snakeCaseToCamelCase converts``() =
        equals (snakeCaseToCamelCase "hello_world") "HelloWorld"
        equals (snakeCaseToCamelCase "foo") "Foo"

    [<Test>]
    member _.``camelCaseToSnakeCase converts``() =
        equals (camelCaseToSnakeCase "HelloWorld") "hello_world"
        equals (camelCaseToSnakeCase "fooBar") "foo_bar"

    [<Test>]
    member _.``UnixTime.seconds returns reasonable value``() =
        let now = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        let fast = UnixTime.seconds ()
        isTrue (abs (now - fast) < 2L)

    [<Test>]
    member _.``UnixTime.milliseconds returns reasonable value``() =
        let now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        let fast = UnixTime.milliseconds ()
        isTrue (abs (now - fast) < 100L)
