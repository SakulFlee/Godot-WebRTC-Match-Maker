using System;
using System.IO;
using System.IO.Compression;

public class GZIP
{
    public static byte[] Decompress(byte[] data)
    {
        using var compressedStream = new MemoryStream(data);
        using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();

        zipStream.CopyTo(resultStream);
        return resultStream.ToArray();
    }

    public static byte[] Compress(byte[] data)
    {
        using var result = new MemoryStream();
        using (var compressionStream = new GZipStream(result, CompressionMode.Compress))
        {
            compressionStream.Write(data, 0, data.Length);
        }
        return result.ToArray();
    }
}