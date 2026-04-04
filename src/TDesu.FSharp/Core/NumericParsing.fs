namespace TDesu.FSharp

open System

[<RequireQualifiedAccess>]
module Int16 =
    /// Parses a string as int16, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Int16.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Int32 =
    /// Parses a string as int32, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Int32.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Int64 =
    /// Parses a string as int64, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Int64.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Double =
    /// Parses a string as double, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Double.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Single =
    /// Parses a string as float32, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Single.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Decimal =
    /// Parses a string as decimal, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Decimal.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Byte =
    /// Parses a string as byte, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Byte.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Boolean =
    /// Parses a string as bool, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Boolean.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module Guid =
    /// Parses a string as Guid, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        Guid.TryParse(str) |> Option.ofCSharpTryPattern

[<RequireQualifiedAccess>]
module DateTimeOffset =
    /// Parses a string as DateTimeOffset, returning <c>Some(value)</c> or <c>None</c>.
    /// <param name="str">The string to parse.</param>
    let inline tryParse (str: string) =
        DateTimeOffset.TryParse(str) |> Option.ofCSharpTryPattern
