namespace TDesu.FSharp.IO

open System.IO
open System.Threading

/// A Stream backed by a temporary file that is deleted on Dispose (unless opted out).
/// <param name="tempFileName">Optional path for the temporary file; defaults to a system-generated temp file name.</param>
/// <param name="doNotDeleteFileAfterDispose">When true, the backing file is kept on disk after Dispose.</param>
type TemporaryFileStream(?tempFileName, ?doNotDeleteFileAfterDispose) =
    inherit Stream()

    let tempFileName = tempFileName |> Option.defaultWith Path.GetTempFileName

    let file = File.Open(tempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)

    override this.CanRead = file.CanRead

    override this.CanSeek = file.CanSeek

    override this.CanWrite = file.CanWrite

    override this.Length = file.Length

    override this.Position
        with get () = file.Position
        and set value = file.Position <- value

    /// Gets the full path of the temporary file backing this stream.
    member this.FileName = tempFileName

    override this.Flush() = file.Flush()

    override this.FlushAsync(ct: CancellationToken) = file.FlushAsync(ct)

    override this.Read(buffer, offset, count) = file.Read(buffer, offset, count)

    override this.ReadAsync(buffer, offset, count, ct) =
        file.ReadAsync(buffer, offset, count, ct)

    override this.Seek(offset, origin) = file.Seek(offset, origin)

    override this.SetLength(value) = file.SetLength(value)

    override this.Write(buffer, offset, count) = file.Write(buffer, offset, count)

    override this.WriteAsync(buffer, offset, count, ct) =
        file.WriteAsync(buffer, offset, count, ct)

    override this.Dispose(disposing) =
        if disposing then
            try
                file.Dispose()
            finally
                match doNotDeleteFileAfterDispose with
                | Some true -> () // caller requested to keep file
                | _ ->
                    try
                        File.Delete(tempFileName)
                    with _ ->
                        ()

        base.Dispose(disposing)
