namespace TDesu.FSharp.IO

open System
open System.IO
open System.Threading
open System.Threading.Tasks

/// A Stream backed by a temporary file that is deleted on Dispose (unless opted out).
/// <param name="tempFileName">Optional path for the temporary file; defaults to a system-generated temp file name.</param>
/// <param name="doNotDeleteFileAfterDispose">When true, the backing file is kept on disk after Dispose.</param>
type TemporaryFileStream(?tempFileName, ?doNotDeleteFileAfterDispose) =
    inherit Stream()

    let tempFileName = tempFileName |> Option.defaultWith Path.GetTempFileName

    // FileOptions.DeleteOnClose makes the OS delete the backing file the instant the handle closes,
    // by any route: an explicit Dispose/DisposeAsync, or the finalizer that runs if the caller
    // forgets both — the inner FileStream's SafeFileHandle is a CriticalFinalizerObject and releases
    // the handle (triggering the delete) even when nothing here ran. That replaces a separate
    // File.Delete call entirely, so there is no leftover delete whose failure needs swallowing.
    let fileOptions =
        if doNotDeleteFileAfterDispose = Some true then
            FileOptions.None
        else
            FileOptions.DeleteOnClose

    let file =
        new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, fileOptions)

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

    override this.Read(buffer: Span<byte>) = file.Read(buffer)

    override this.ReadAsync(buffer, offset, count, ct) =
        file.ReadAsync(buffer, offset, count, ct)

    override this.ReadAsync(buffer: Memory<byte>, ct: CancellationToken) = file.ReadAsync(buffer, ct)

    override this.Seek(offset, origin) = file.Seek(offset, origin)

    override this.SetLength(value) = file.SetLength(value)

    override this.Write(buffer, offset, count) = file.Write(buffer, offset, count)

    override this.Write(buffer: ReadOnlySpan<byte>) = file.Write(buffer)

    override this.WriteAsync(buffer, offset, count, ct) =
        file.WriteAsync(buffer, offset, count, ct)

    override this.WriteAsync(buffer: ReadOnlyMemory<byte>, ct: CancellationToken) = file.WriteAsync(buffer, ct)

    override this.Dispose(disposing) =
        if disposing then
            file.Dispose()

        base.Dispose(disposing)

    override this.DisposeAsync() : ValueTask = file.DisposeAsync()
