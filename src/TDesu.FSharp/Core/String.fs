namespace TDesu.FSharp

open TDesu.FSharp.Operators

/// <summary>
/// String utility functions — functional wrappers over <see cref="System.String"/> methods.
/// </summary>
[<RequireQualifiedAccess>]
module String =
    /// <summary>
    /// Returns <c>true</c> if the string starts with the given prefix.
    /// Null-hardened: returns <c>false</c> if either <paramref name="str"/> or <paramref name="value"/> is
    /// <c>null</c>, per the "null string subject/argument = no match" policy (a null subject never matches).
    /// </summary>
    /// <param name="value">The prefix to look for.</param>
    /// <param name="str">The string to check.</param>
    let inline startsWith (value: string) (str: string) =
        isNotNullRef str && isNotNullRef value && str.StartsWith(value)

    /// Returns true if string is null, empty, or only whitespace.
    let isNullOrWhiteSpace = System.String.IsNullOrWhiteSpace
    /// Returns true if string is not null/empty/whitespace.
    let isNotNullOrWhiteSpace = System.String.IsNullOrWhiteSpace >> not

    /// <summary>
    /// Counts the number of non-overlapping occurrences of <paramref name="substr"/> in <paramref name="str"/>.
    /// Null-hardened: a <c>null</c> <paramref name="str"/> or <paramref name="substr"/> is treated the same as
    /// an empty one — the existing empty-input branch already returns <c>0</c>, so this just extends that
    /// branch to null instead of dereferencing <c>.Length</c> on a null reference.
    /// </summary>
    /// <returns>Number of occurrences; 0 for null/empty input or null/empty substr.</returns>
    /// <example>
    /// <code>
    /// "abcabc" |> String.countOccurrences "abc" // 2
    /// </code>
    /// </example>
    /// <param name="substr">The substring to count.</param>
    /// <param name="str">The string to search in.</param>
    let countOccurrences (substr: string) (str: string) =
        if isNull str || isNull substr || str.Length = 0 || substr.Length = 0 then
            0
        else
            let mutable count = 0
            let mutable idx = str.IndexOf(substr, System.StringComparison.Ordinal)

            while idx >= 0 do
                count <- count + 1
                idx <- str.IndexOf(substr, idx + substr.Length, System.StringComparison.Ordinal)

            count

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/> starts with any of the <paramref name="values"/>.
    /// </summary>
    /// <remarks>
    /// Null policy: a <c>null</c> <paramref name="str"/> never matches (null subject = no match); a
    /// <c>null</c> or empty <paramref name="values"/> is treated as an empty set and never matches (null
    /// collection = empty); a <c>null</c> element inside <paramref name="values"/> is skipped rather than
    /// throwing, since it can never be a prefix of anything.
    /// </remarks>
    /// <example>
    /// <code>
    /// "foobar" |> String.startsWithAny [| "foo"; "bar" |] // true
    /// </code>
    /// </example>
    /// <param name="values">The prefixes to test against.</param>
    /// <param name="str">The string to check.</param>
    let startsWithAny (values: string[]) (str: string) =
        if isNull str || isNull values then
            false
        else
            values |> Array.exists (fun v -> isNotNullRef v && str.StartsWith(v))

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/> is equal to any of the <paramref name="values"/>, using the
    /// given <paramref name="comparison"/>.
    /// </summary>
    /// <remarks>
    /// Null policy matches <see cref="startsWithAny"/>: null subject/collection never match; a null element of
    /// <paramref name="values"/> can equal only a non-null <paramref name="str"/>, so it never spuriously
    /// matches a null subject even though <see cref="System.String.Equals(string,string,StringComparison)"/>
    /// treats two nulls as equal. <paramref name="comparison"/> is a normal required parameter rather than an
    /// overload pair, so the comparison mode is always explicit at the call site.
    /// </remarks>
    /// <param name="comparison">The string comparison mode to use.</param>
    /// <param name="values">The candidates to test against.</param>
    /// <param name="str">The string to check.</param>
    let equalsAny (comparison: System.StringComparison) (values: string[]) (str: string) =
        if isNull str || isNull values then
            false
        else
            values |> Array.exists (fun v -> System.String.Equals(str, v, comparison))

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/> contains any of the <paramref name="values"/>, using the
    /// given <paramref name="comparison"/>. Null policy matches <see cref="startsWithAny"/>.
    /// </summary>
    /// <param name="comparison">The string comparison mode to use.</param>
    /// <param name="values">The substrings to search for.</param>
    /// <param name="str">The string to search in.</param>
    let containsAny (comparison: System.StringComparison) (values: string[]) (str: string) =
        if isNull str || isNull values then
            false
        else
            values |> Array.exists (fun v -> isNotNullRef v && str.Contains(v, comparison))

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/> ends with any of the <paramref name="values"/>, using the
    /// given <paramref name="comparison"/>. Null policy matches <see cref="startsWithAny"/>.
    /// </summary>
    /// <param name="comparison">The string comparison mode to use.</param>
    /// <param name="values">The suffixes to test against.</param>
    /// <param name="str">The string to check.</param>
    let endsWithAny (comparison: System.StringComparison) (values: string[]) (str: string) =
        if isNull str || isNull values then
            false
        else
            values |> Array.exists (fun v -> isNotNullRef v && str.EndsWith(v, comparison))

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/>, taken as a single character, equals any of the
    /// <paramref name="values"/>. A <paramref name="str"/> whose length is not exactly 1 (including empty)
    /// never matches, since it cannot represent a single character.
    /// </summary>
    /// <remarks>
    /// Null policy: a <c>null</c> <paramref name="str"/> or <paramref name="values"/> never matches. Unlike the
    /// <c>string[]</c> overloads, <c>char</c> is a non-nullable value type, so no element of
    /// <paramref name="values"/> can itself be null.
    /// </remarks>
    /// <param name="values">The characters to test against.</param>
    /// <param name="str">The string to check.</param>
    let equalsAnyChar (values: char[]) (str: string) =
        if isNull str || isNull values || str.Length <> 1 then
            false
        else
            values |> Array.exists (fun c -> c = str[0])

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/> contains any of the <paramref name="values"/> characters,
    /// anywhere in the string. Null policy matches <see cref="equalsAnyChar"/>.
    /// </summary>
    /// <param name="values">The characters to search for.</param>
    /// <param name="str">The string to search in.</param>
    let containsAnyChar (values: char[]) (str: string) =
        if isNull str || isNull values then
            false
        else
            values |> Array.exists (fun c -> str.IndexOf(c) >= 0)

    /// <summary>
    /// Returns <c>true</c> if <paramref name="str"/> ends with any of the <paramref name="values"/> characters.
    /// Null policy matches <see cref="equalsAnyChar"/>.
    /// </summary>
    /// <param name="values">The characters to test against.</param>
    /// <param name="str">The string to check.</param>
    let endsWithAnyChar (values: char[]) (str: string) =
        if isNull str || isNull values || str.Length = 0 then
            false
        else
            values |> Array.exists (fun c -> str[str.Length - 1] = c)

    /// <summary>
    /// Replaces all line-ending sequences with the given replacement text.
    /// Null-hardened: returns <c>null</c> unchanged if <paramref name="str"/> is <c>null</c>, matching
    /// <see cref="truncate"/>'s contract instead of throwing. A <c>null</c> <paramref name="replacementText"/>
    /// is already handled gracefully by <see cref="System.String.Replace(string,string)"/>, which removes the
    /// matched line endings instead of substituting them.
    /// </summary>
    /// <param name="replacementText">The text to substitute for line endings.</param>
    /// <param name="str">The input string.</param>
    let replaceEndLines (replacementText: string) (str: string) =
        if isNull str then
            str
        else
            str.Replace("\r\n", replacementText).Replace("\r", replacementText).Replace("\n", replacementText)

    /// Converts a string to uppercase using invariant culture.
    /// <param name="str">The string to convert.</param>
    let toUpperInv (str: string) = str.ToUpperInvariant()

    /// Converts a string to uppercase using the current culture.
    /// <param name="str">The string to convert.</param>
    let toUpper (str: string) = str.ToUpper()

    /// Converts a string to lowercase using invariant culture.
    /// <param name="str">The string to convert.</param>
    let toLowerInv (str: string) = str.ToLowerInvariant()

    /// Converts a string to lowercase using the current culture.
    /// <param name="str">The string to convert.</param>
    let toLower (str: string) = str.ToLower()

    /// Returns the substring starting at the given index.
    /// <param name="idx">The zero-based start index.</param>
    /// <param name="str">The input string.</param>
    let inline substring (idx: int) (str: string) = str.Substring(idx)

    /// Returns the substring starting at idx with the given length.
    /// <param name="idx">The zero-based start index.</param>
    /// <param name="length">The number of characters to extract.</param>
    /// <param name="str">The input string.</param>
    let inline substringLen (idx: int) (length: int) (str: string) = str.Substring(idx, length)

    /// <summary>
    /// Returns <c>true</c> if str contains value (ordinal).
    /// Null-hardened: returns <c>false</c> if either <paramref name="str"/> or <paramref name="value"/> is
    /// <c>null</c>, per the "null subject/argument = no match" policy.
    /// </summary>
    /// <param name="value">The substring to search for.</param>
    /// <param name="str">The string to search in.</param>
    let inline contains (value: string) (str: string) =
        isNotNullRef str && isNotNullRef value && str.Contains(value)

    /// <summary>
    /// Returns <c>true</c> if str ends with value.
    /// Null-hardened: returns <c>false</c> if either <paramref name="str"/> or <paramref name="value"/> is
    /// <c>null</c>, per the "null subject/argument = no match" policy.
    /// </summary>
    /// <param name="value">The suffix to look for.</param>
    /// <param name="str">The string to check.</param>
    let inline endsWith (value: string) (str: string) =
        isNotNullRef str && isNotNullRef value && str.EndsWith(value)

    /// Returns true if the string is null or empty (no whitespace check).
    let isNullOrEmpty = System.String.IsNullOrEmpty

    /// Removes leading and trailing whitespace.
    /// <param name="str">The string to trim.</param>
    let inline trim (str: string) = str.Trim()

    /// Removes leading whitespace.
    /// <param name="str">The string to trim.</param>
    let inline trimStart (str: string) = str.TrimStart()

    /// Removes trailing whitespace.
    /// <param name="str">The string to trim.</param>
    let inline trimEnd (str: string) = str.TrimEnd()

    /// Splits a string by the given separator.
    /// <param name="separator">The delimiter string.</param>
    /// <param name="str">The string to split.</param>
    let inline split (separator: string) (str: string) = str.Split(separator)

    /// Splits a string by the given char separator.
    /// <param name="separator">The delimiter character.</param>
    /// <param name="str">The string to split.</param>
    let inline splitChar (separator: char) (str: string) = str.Split(separator)

    /// Replaces all occurrences of oldValue with newValue.
    /// <param name="oldValue">The substring to find.</param>
    /// <param name="newValue">The replacement string.</param>
    /// <param name="str">The input string.</param>
    let inline replace (oldValue: string) (newValue: string) (str: string) = str.Replace(oldValue, newValue)

    /// Removes all occurrences of value from the string.
    /// <param name="value">The substring to remove.</param>
    /// <param name="str">The input string.</param>
    let inline remove (value: string) (str: string) = str.Replace(value, "")

    /// <summary>
    /// Returns at most <paramref name="maxLen"/> characters from the start of the string.
    /// Null-safe: returns null if input is null.
    /// </summary>
    /// <example>
    /// <code>
    /// String.truncate 5 "hello world" // "hello"
    /// </code>
    /// </example>
    /// <param name="maxLen">Maximum length of the returned string.</param>
    /// <param name="str">The string to truncate.</param>
    let truncate (maxLen: int) (str: string) =
        if isNull str || str.Length <= maxLen then
            str
        else
            str.Substring(0, maxLen)

    /// <summary>
    /// Joins a sequence of strings with the given separator.
    /// Null-hardened: a <c>null</c> <paramref name="values"/> sequence is treated as empty, returning
    /// <see cref="System.String.Empty"/> instead of throwing (per the "null collection = empty" policy).
    /// </summary>
    /// <param name="separator">The delimiter placed between elements.</param>
    /// <param name="values">The strings to join.</param>
    let inline join (separator: string) (values: string seq) =
        if isNull values then
            ""
        else
            System.String.Join(separator, values)

    /// <summary>
    /// Returns <c>None</c> if the string is null/empty/whitespace, otherwise <c>Some(s)</c>.
    /// </summary>
    /// <param name="s">The string to evaluate.</param>
    let inline toOption (s: string) =
        if System.String.IsNullOrWhiteSpace(s) then None else Some s

    /// Returns the default value if the string is null or empty.
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="str">The string to test.</param>
    let inline defaultIfEmpty (defaultValue: string) (str: string) =
        if System.String.IsNullOrEmpty(str) then
            defaultValue
        else
            str

    /// Pads the string on the left to the given total width.
    /// <param name="totalWidth">The desired total length after padding.</param>
    /// <param name="str">The string to pad.</param>
    let inline padLeft (totalWidth: int) (str: string) = str.PadLeft(totalWidth)

    /// Pads the string on the right to the given total width.
    /// <param name="totalWidth">The desired total length after padding.</param>
    /// <param name="str">The string to pad.</param>
    let inline padRight (totalWidth: int) (str: string) = str.PadRight(totalWidth)
