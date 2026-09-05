namespace TDesu.FSharp

open Microsoft.FSharp.Reflection

/// <summary>
/// Reflection helpers for F# discriminated unions: the case name a value was built with, and the
/// full set of case names for a union type.
/// </summary>
/// <example>
/// <code>
/// type Status =
///     | Active
///     | Suspended of reason: string
///
/// Union.caseName Active                 // "Active"
/// Union.caseName (Suspended "abuse")    // "Suspended"
/// Union.caseNames&lt;Status&gt; ()      // [| "Active"; "Suspended" |]
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Union =

    // Per-'T reflection, computed once and cached in this closed generic type's static fields —
    // including the "not a union" outcome, so a bad 'T also costs one check, not one per call.
    //
    // The check happens here, in the accessor, rather than by raising straight out of the static
    // initializer above: a static initializer that throws poisons the type for the rest of the
    // process — verified empirically: every later access re-throws TypeInitializationException,
    // never the original exception — so callers would see that instead of the ArgumentException
    // this module documents. Storing the outcome as a Result and raising from caseName/caseNames
    // themselves keeps the promised exception on every call, not just the first.
    [<AbstractClass; Sealed>]
    type private UnionInfo<'T> =
        static let info =
            let t = typeof<'T>

            if FSharpType.IsUnion t then
                Ok(FSharpType.GetUnionCases t |> Array.map (fun c -> c.Name), FSharpValue.PreComputeUnionTagReader t)
            else
                Error t.FullName

        static member Info: Result<string[] * (obj -> int), string> = info

    let private raiseNotUnion (paramName: string) (typeName: string) : 'a =
        invalidArg paramName $"'{typeName}' is not an F# union type."

    /// <summary>
    /// The name of the case <paramref name="value"/> was built with.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <c>'T</c> is not an F# union.</exception>
    /// <param name="value">The union value to inspect.</param>
    let caseName<'T> (value: 'T) : string =
        match UnionInfo<'T>.Info with
        | Ok(names, tagReader) -> names[tagReader (box value)]
        | Error typeName -> raiseNotUnion "value" typeName

    /// <summary>
    /// Every case name of <c>'T</c>, in declaration order.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <c>'T</c> is not an F# union.</exception>
    let caseNames<'T> () : string[] =
        match UnionInfo<'T>.Info with
        | Ok(names, _) -> names
        | Error typeName -> raiseNotUnion "T" typeName
