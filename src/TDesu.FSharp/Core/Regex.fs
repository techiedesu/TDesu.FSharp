namespace TDesu.FSharp

module Regex =

    open System.Text.RegularExpressions

    module Match =

        /// Gets the capture group at the given index.
        /// <param name="m">The match to extract from.</param>
        /// <param name="idx">Zero-based group index.</param>
        let inline getGroup (m: Match) (idx: int) = m.Groups[idx]

        /// Gets the second capture group (index 1).
        /// <param name="m">The match to extract from.</param>
        let inline getSecondGroup (m: Match) = m.Groups[1]

    module Capture =

        /// Gets the matched string value of a capture.
        /// <param name="c">Capture to get the value from.</param>
        let inline value (c: Capture) = c.Value
