namespace TDesu.FSharp.IO

open System

#if !FABLE_COMPILER
open System.IO
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Bounded stream copy utilities.
/// </summary>
[<RequireQualifiedAccess>]
module Stream =
    /// <summary>
    /// The outcome of <see cref="copyUpTo"/> when the source produced more bytes than the configured limit.
    /// </summary>
    type MaxBytesExceeded =
        { /// The configured maximum number of bytes <see cref="copyUpTo"/> was allowed to read.
          MaxBytes: int64
          /// The number of bytes actually written to the destination before the limit was hit.
          BytesWritten: int64 }

    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="destination"/>, stopping and returning
    /// <c>Error</c> instead of throwing once more than <paramref name="maxBytes"/> would be read from
    /// <paramref name="source"/>. The chunk that would push the total over the limit is discarded
    /// rather than partially written, so <paramref name="destination"/> never receives more than
    /// <paramref name="maxBytes"/> bytes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses an 81920-byte buffer rented from <see cref="System.Buffers.ArrayPool{T}.Shared"/>, always
    /// returned in a <c>finally</c> — including when <paramref name="cancellationToken"/> cancels the
    /// copy or either stream throws. Neither <paramref name="source"/> nor <paramref name="destination"/>
    /// is disposed by this function; the caller owns both. The repo's own <c>ArrayPool</c> helpers in
    /// <c>Buffers.fs</c> are not used here: <c>Buffers.fs</c> compiles after this file in
    /// <c>TDesu.FSharp.fsproj</c>, so this module cannot reference it.
    /// </para>
    /// <para>
    /// Deviates from the usual "null collection is empty" policy: a null stream cannot stand in for an
    /// empty one without silently changing where bytes go (a null <paramref name="destination"/> would
    /// silently discard writes), so a null <paramref name="source"/> or <paramref name="destination"/>
    /// is treated like a null function argument — a programmer error that throws immediately, before
    /// any I/O or task is started.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// match! request.Body |> Stream.copyUpTo (10L * 1024L * 1024L) buffer ct with
    /// | Ok bytesWritten -> ...
    /// | Error e -> failwithf "payload too large: wrote %d of %d allowed bytes" e.BytesWritten e.MaxBytes
    /// </code>
    /// </example>
    /// <exception cref="System.ArgumentNullException">When <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
    /// <param name="maxBytes">The maximum number of bytes allowed to be read from <paramref name="source"/>.</param>
    /// <param name="destination">The stream to write to. Not disposed by this function.</param>
    /// <param name="cancellationToken">Token observed on every read and write.</param>
    /// <param name="source">The stream to read from. Not disposed by this function.</param>
    let copyUpTo (maxBytes: int64) (destination: Stream) (cancellationToken: CancellationToken) (source: Stream) : Task<Result<int64, MaxBytesExceeded>> =
        if isNull source then nullArg (nameof source)
        if isNull destination then nullArg (nameof destination)
        task {
            let buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(81920)
            try
                let mutable written = 0L
                let mutable exceeded = false
                let mutable reading = true
                while reading do
                    let! bytesRead = source.ReadAsync(Memory(buffer), cancellationToken)
                    if bytesRead = 0 then
                        reading <- false
                    elif written + int64 bytesRead > maxBytes then
                        exceeded <- true
                        reading <- false
                    else
                        do! destination.WriteAsync(ReadOnlyMemory(buffer, 0, bytesRead), cancellationToken)
                        written <- written + int64 bytesRead
                return if exceeded then Error { MaxBytes = maxBytes; BytesWritten = written } else Ok written
            finally
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer)
        }
#endif
