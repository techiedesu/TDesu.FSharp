namespace TDesu.FSharp

open System
open System.Linq.Expressions
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
    //
    // The tag reader is cached as `'T -> int`, not as the `obj -> int` FSharpValue.PreComputeUnionTagReader
    // hands out. For a reference-typed union `box value` is a free upcast, so wrapping the `obj -> int`
    // reader costs nothing extra; for a `[<Struct>]` union the same call would box the value on every
    // invocation, so that case instead compiles its own `Tag`-reading accessor once per 'T from an
    // expression tree and never boxes.
    [<AbstractClass; Sealed>]
    type private UnionInfo<'T> =
        static let info =
            let t = typeof<'T>

            if FSharpType.IsUnion t then
                let names = FSharpType.GetUnionCases t |> Array.map (fun c -> c.Name)

                let tagReader: 'T -> int =
                    if t.IsValueType then
                        let param = Expression.Parameter(t, "value")
                        let tag = Expression.Property(param, "Tag")
                        Expression.Lambda<Func<'T, int>>(tag, param).Compile().Invoke
                    else
                        let boxedReader = FSharpValue.PreComputeUnionTagReader t
                        fun value -> boxedReader (box value)

                Ok(names, tagReader)
            else
                Error t.FullName

        static member Info: Result<string[] * ('T -> int), string> = info

    let private raiseNotUnion (paramName: string) (typeName: string) : 'a =
        invalidArg paramName $"'{typeName}' is not an F# union type."

    /// <summary>
    /// The name of the case <paramref name="value"/> was built with.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <c>'T</c> is not an F# union.</exception>
    /// <param name="value">The union value to inspect.</param>
    let caseName<'T> (value: 'T) : string =
        match UnionInfo<'T>.Info with
        | Ok(names, tagReader) -> names[tagReader value]
        | Error typeName -> raiseNotUnion "value" typeName

    /// <summary>
    /// Every case name of <c>'T</c>, in declaration order. Returns a fresh copy on every call: the
    /// names are cached internally for <see cref="caseName"/> to index into, and handing out that
    /// same array would let a caller who mutates the result (sorting it in place, say) corrupt the
    /// cache for every later call, <see cref="caseName"/> included.
    /// </summary>
    /// <exception cref="System.ArgumentException">When <c>'T</c> is not an F# union.</exception>
    let caseNames<'T> () : string[] =
        match UnionInfo<'T>.Info with
        | Ok(names, _) -> Array.copy names
        | Error typeName -> raiseNotUnion "T" typeName
