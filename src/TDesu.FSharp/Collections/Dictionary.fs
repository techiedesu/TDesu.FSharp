namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

/// <namespacedoc>
///   <summary>Collection extensions: Dictionary, ResizeArray, Seq, List, Stack helpers.</summary>
/// </namespacedoc>
[<RequireQualifiedAccess>]
module Dictionary =
    /// Gets the value for the given key; throws if key is missing.
    /// A null <paramref name="d"/> is treated as an empty dictionary, so a missing key still raises
    /// <see cref="System.Collections.Generic.KeyNotFoundException"/> rather than a null-reference error.
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">When the key is not present, including when <paramref name="d"/> is null.</exception>
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline getValue key (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then
            raise (KeyNotFoundException($"The given key '%O{key}' was not present in the dictionary."))
        else
            d[key]

    /// Tries to get a value, returning <c>Some(value)</c> or <c>None</c>. A null <paramref name="d"/> is
    /// treated as an empty dictionary and also returns <c>None</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline tryGetValue key (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then None
        else d.TryGetValue key |> Option.ofCSharpTryPattern

    /// Tries to get a value, returning <c>ValueSome(value)</c> or <c>ValueNone</c>. A null
    /// <paramref name="d"/> is treated as an empty dictionary and also returns <c>ValueNone</c>.
    /// <param name="key">The key to look up.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline tryGetValueV key (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then ValueNone
        else d.TryGetValue key |> ValueOption.ofCSharpTryPattern

    /// Get value or default — replaces <c>match d.TryGetValue(k) with true, v -> v | _ -> def</c>.
    /// A null <paramref name="d"/> is treated as an empty dictionary and also returns
    /// <paramref name="defaultValue"/>.
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <param name="d">The dictionary to search.</param>
    let inline getOrDefault key defaultValue (d: #IDictionary<'TKey, 'TValue>) =
        if isNull d then
            defaultValue
        else
            match d.TryGetValue key with
            | true, v -> v
            | false, _ -> defaultValue
