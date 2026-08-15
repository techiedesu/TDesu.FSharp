namespace TDesu.FSharp.Collections

open System
open System.Collections.Generic
open TDesu.FSharp
open TDesu.FSharp.Operators

open System.IO

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
