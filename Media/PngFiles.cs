using System;
using System.IO;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Verify a finished PNG signature and IEND trailer before notifying the user.
/// This is a fast completeness hint, not a full PNG chunk/CRC validator.
/// PNG 轻量检查，不解析图片或替代完整的 CRC 校验。
/// </summary>
public static class PngFiles {
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly byte[] EndChunk = { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 };

    public static bool IsComplete(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return false;
        }
        try {
            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            return HasCompleteTrailer(stream);
        } catch (IOException) {
            return false;
        } catch (UnauthorizedAccessException) {
            return false;
        } catch (ArgumentException) {
            return false;
        }
    }

    public static bool HasCompleteTrailer(Stream stream) {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek || stream.Length < Signature.Length + EndChunk.Length) {
            return false;
        }
        Span<byte> header = stackalloc byte[8];
        Span<byte> tail = stackalloc byte[12];
        stream.Seek(0, SeekOrigin.Begin);
        if (!ReadAll(stream, header)) {
            return false;
        }
        stream.Seek(-EndChunk.Length, SeekOrigin.End);
        if (!ReadAll(stream, tail)) {
            return false;
        }
        return header.SequenceEqual(Signature) && tail.SequenceEqual(EndChunk);
    }

    private static bool ReadAll(Stream stream, Span<byte> buffer) {
        while (!buffer.IsEmpty) {
            int read = stream.Read(buffer);
            if (read == 0) {
                return false;
            }
            buffer = buffer.Slice(read);
        }
        return true;
    }
}
