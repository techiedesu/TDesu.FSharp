namespace TDesu.FSharp.IO

open System

open System.IO
open TDesu.FSharp.Operators

/// <namespacedoc>
///   <summary>I/O utilities: Env, File, Directory, Stream (copyUpTo), Disposable (deferStack), TemporaryFileStream.</summary>
/// </namespacedoc>
[<RequireQualifiedAccess>]
module File =
    /// <summary>
    /// Deletes a file, returning <c>Ok(())</c> on success or <c>Error(exn)</c> on failure.
    /// </summary>
    /// <param name="filePath">The path of the file to delete.</param>
    let delete filePath =
        try
            File.Delete(filePath)
            Ok()
        with e ->
            Error e

    /// Deletes a file, ignoring the result.
    let deleteIgnore = delete >> ignore

    /// Returns true if the file does not exist at the given path.
    /// <param name="path">The file path to check.</param>
    let inline notExists path = File.Exists(path) |> not

module Directory =
    /// Returns true if the directory does not exist at the given path.
    /// <param name="path">The directory path to check.</param>
    let notExists path = Directory.Exists(path) |> not

    /// Creates a directory (and parents) at the given path.
    /// <param name="path">The directory path to create.</param>
    let create path = %Directory.CreateDirectory(path)

[<RequireQualifiedAccess>]
module Path =
    /// Generates a random file name from a GUID (no dashes or extension).
    let getRandomFileName () =
        let g = Guid.NewGuid()
        g.ToString("N")

/// Static helper for path utilities.
[<Sealed; AbstractClass>]
type Path private () =
    /// Gets a new random file name (GUID-based, no dashes).
    static member RandomFileName = Path.getRandomFileName ()
