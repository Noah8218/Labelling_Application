using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MvcVisionSystem
{
    public static class HashingService
    {
        public static string ComputeFileSha256(string path, bool lowerCase = false)
        {
            using FileStream stream = File.OpenRead(path);
            return ComputeStreamSha256(stream, lowerCase);
        }

        public static string ComputeStreamSha256(Stream stream, bool lowerCase = false)
        {
            if (stream == null)
            {
                throw new System.ArgumentNullException(nameof(stream));
            }

            return FormatHash(SHA256.HashData(stream), lowerCase);
        }

        public static string ComputeUtf8TextSha256(string text, bool lowerCase = false)
            => FormatHash(
                SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty)),
                lowerCase);

        private static string FormatHash(byte[] hash, bool lowerCase)
        {
            string value = Convert.ToHexString(hash ?? System.Array.Empty<byte>());
            return lowerCase ? value.ToLowerInvariant() : value;
        }
    }
}
