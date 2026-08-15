namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

#if !FABLE_COMPILER
open System.IO

[<RequireQualifiedAccess>]
module Array =
    /// Converts a <see cref="System.IO.MemoryStream"/> to a byte array. A null
    /// <paramref name="memoryStream"/> is treated as empty and yields <c>[||]</c>.
    /// <param name="memoryStream">The memory stream to convert.</param>
    let inline ofMemoryStream (memoryStream: MemoryStream) =
        if isNull memoryStream then [||] else memoryStream.ToArray()

[<RequireQualifiedAccess>]
module MemoryStream =
    /// Resets the stream position to the beginning.
    /// <paramref name="memoryStream"/> is the mutation target: a null <paramref name="memoryStream"/> has
    /// no instance whose position can be reset, so it is a programmer error.
    /// <exception cref="System.ArgumentNullException">When <paramref name="memoryStream"/> is null.</exception>
    /// <param name="memoryStream">The memory stream to reset.</param>
    let inline reset (memoryStream: MemoryStream) =
        Guard.notNull "memoryStream" memoryStream
        memoryStream.Position <- 0
#endif
