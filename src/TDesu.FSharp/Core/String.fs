namespace TDesu.FSharp

open TDesu.FSharp.Operators

/// <summary>
/// String utility functions — functional wrappers over <see cref="System.String"/> methods.
/// </summary>
[<RequireQualifiedAccess>]
module String =
    /// Returns true if the string starts with the given prefix.
    /// <param name="value">The prefix to look for.</param>
    /// <param name="str">The string to check.</param>
    let inline startsWith (value: string) (str: string) = str.StartsWith(value)

    /// Returns true if string is null, empty, or only whitespace.
    let isNullOrWhiteSpace = System.String.IsNullOrWhiteSpace
    /// Returns true if string is not null/empty/whitespace.
    let isNotNullOrWhiteSpace = System.String.IsNullOrWhiteSpace >> not

    /// <summary>
    /// Counts the number of non-overlapping occurrences of <paramref name="substr"/> in <paramref name="str"/>.
    /// </summary>
    /// <returns>Number of occurrences; 0 for empty input or empty substr.</returns>
    /// <example>
    /// <code>
    /// "abcabc" |> String.countOccurrences "abc" // 2
    /// </code>
    /// </example>
    /// <param name="substr">The substring to count.</param>
    /// <param name="str">The string to search in.</param>
    let countOccurrences (substr: string) (str: string) =
        if str.Length = 0 || substr.Length = 0 then 0
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
    /// <param name="values">The prefixes to test against.</param>
    /// <param name="str">The string to check.</param>
    let startsWithAny (values: string[]) (str: string) =
        values |> Array.exists str.StartsWith

    /// Replaces all line-ending sequences with the given replacement text.
    /// <param name="replacementText">The text to substitute for line endings.</param>
    /// <param name="str">The input string.</param>
    let replaceEndLines (replacementText: string) (str: string) =
        str.Replace("\r\n", replacementText).Replace("\r", replacementText).Replace("\n", replacementText)

    /// Converts a string to uppercase using invariant culture.
    /// <param name="str">The string to convert.</param>
    let toUpperInv (str: string) =
        str.ToUpperInvariant()

    /// Converts a string to uppercase using the current culture.
    /// <param name="str">The string to convert.</param>
    let toUpper (str: string) =
        str.ToUpper()

    /// Converts a string to lowercase using invariant culture.
    /// <param name="str">The string to convert.</param>
    let toLowerInv (str: string) =
        str.ToLowerInvariant()

    /// Converts a string to lowercase using the current culture.
    /// <param name="str">The string to convert.</param>
    let toLower (str: string) =
        str.ToLower()

    /// Returns the substring starting at the given index.
    /// <param name="idx">The zero-based start index.</param>
    /// <param name="str">The input string.</param>
    let inline substring (idx: int) (str: string) =
        str.Substring(idx)

    /// Returns the substring starting at idx with the given length.
    /// <param name="idx">The zero-based start index.</param>
    /// <param name="length">The number of characters to extract.</param>
    /// <param name="str">The input string.</param>
    let inline substringLen (idx: int) (length: int) (str: string) =
        str.Substring(idx, length)

    /// Returns true if str contains value (ordinal).
    /// <param name="value">The substring to search for.</param>
    /// <param name="str">The string to search in.</param>
    let inline contains (value: string) (str: string) =
        str.Contains(value)

    /// Returns true if str ends with value.
    /// <param name="value">The suffix to look for.</param>
    /// <param name="str">The string to check.</param>
    let inline endsWith (value: string) (str: string) =
        str.EndsWith(value)

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
    let inline split (separator: string) (str: string) =
        str.Split(separator)

    /// Splits a string by the given char separator.
    /// <param name="separator">The delimiter character.</param>
    /// <param name="str">The string to split.</param>
    let inline splitChar (separator: char) (str: string) =
        str.Split(separator)

    /// Replaces all occurrences of oldValue with newValue.
    /// <param name="oldValue">The substring to find.</param>
    /// <param name="newValue">The replacement string.</param>
    /// <param name="str">The input string.</param>
    let inline replace (oldValue: string) (newValue: string) (str: string) =
        str.Replace(oldValue, newValue)

    /// Removes all occurrences of value from the string.
    /// <param name="value">The substring to remove.</param>
    /// <param name="str">The input string.</param>
    let inline remove (value: string) (str: string) =
        str.Replace(value, "")

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
        if isNull str || str.Length <= maxLen then str
        else str.Substring(0, maxLen)

    /// Joins a sequence of strings with the given separator.
    /// <param name="separator">The delimiter placed between elements.</param>
    /// <param name="values">The strings to join.</param>
    let inline join (separator: string) (values: string seq) =
        System.String.Join(separator, values)

    /// <summary>
    /// Returns <c>None</c> if the string is null/empty/whitespace, otherwise <c>Some(s)</c>.
    /// </summary>
    /// <param name="s">The string to evaluate.</param>
    let inline toOption (s: string) =
        if System.String.IsNullOrWhiteSpace(s) then None
        else Some s

    /// Returns the default value if the string is null or empty.
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="str">The string to test.</param>
    let inline defaultIfEmpty (defaultValue: string) (str: string) =
        if System.String.IsNullOrEmpty(str) then defaultValue else str

    /// Pads the string on the left to the given total width.
    /// <param name="totalWidth">The desired total length after padding.</param>
    /// <param name="str">The string to pad.</param>
    let inline padLeft (totalWidth: int) (str: string) =
        str.PadLeft(totalWidth)

    /// Pads the string on the right to the given total width.
    /// <param name="totalWidth">The desired total length after padding.</param>
    /// <param name="str">The string to pad.</param>
    let inline padRight (totalWidth: int) (str: string) =
        str.PadRight(totalWidth)
