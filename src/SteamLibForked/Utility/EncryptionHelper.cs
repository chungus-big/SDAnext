using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace SteamLib.Utility;

public static class EncryptionHelper
{
    public static byte[] HexStringToByteArray(string hex)
    {
        return Convert.FromHexString(hex);
    }

    public static string ToBase64EncryptedPassword(string keyExp, string keyMod, string password)
    {
        using var rsa = RSA.Create();
        var expBytes = HexStringToByteArray(keyExp);
        var modBytes = HexStringToByteArray(keyMod);

        try
        {
            RSAParameters rsaParameters = new()
            {
                Exponent = expBytes,
                Modulus = modBytes
            };
            rsa.ImportParameters(rsaParameters);

            int maxBytes = Encoding.ASCII.GetMaxByteCount(password.Length);
            Span<byte> bytePassword = maxBytes <= 256 ? stackalloc byte[maxBytes] : new byte[maxBytes];

            try
            {
                int bytesWritten = Encoding.ASCII.GetBytes(password, bytePassword);
                int cipherBufferSize = (rsa.KeySize + 7) / 8;
                Span<byte> cipherBuffer = cipherBufferSize <= 512 ? stackalloc byte[cipherBufferSize] : new byte[cipherBufferSize];
                int cipherBytesWritten = rsa.Encrypt(bytePassword[..bytesWritten], cipherBuffer, RSAEncryptionPadding.Pkcs1);
                return Convert.ToBase64String(cipherBuffer[..cipherBytesWritten]);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytePassword);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expBytes);
            CryptographicOperations.ZeroMemory(modBytes);
        }
    }

    public static string GenerateConfirmationHash(long time, ReadOnlySpan<byte> identitySecret, string tag = "conf")
    {
        Span<byte> hashBuffer = stackalloc byte[20];
        try
        {
            GenerateConfirmationHashBytes(time, identitySecret, tag, hashBuffer);
            return Convert.ToBase64String(hashBuffer);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hashBuffer);
        }
    }

    public static string GenerateConfirmationHash(long time, string identitySecret, string tag = "conf")
    {
        ArgumentNullException.ThrowIfNull(identitySecret);

        Span<byte> secretBuffer = stackalloc byte[64];
        if (Convert.TryFromBase64Chars(identitySecret.AsSpan(), secretBuffer, out var bytesWritten))
        {
            try
            {
                return GenerateConfirmationHash(time, secretBuffer[..bytesWritten], tag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretBuffer[..bytesWritten]);
            }
        }

        var decode = Convert.FromBase64String(identitySecret);
        try
        {
            return GenerateConfirmationHash(time, (ReadOnlySpan<byte>)decode, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decode);
        }
    }

    public static void GenerateConfirmationHashBytes(long time, ReadOnlySpan<byte> identitySecret, string tag, Span<byte> destination)
    {
        ReadOnlySpan<char> tagSpan = string.IsNullOrEmpty(tag) ? [] : tag.AsSpan(0, Math.Min(tag.Length, 32));
        int tagByteCount = Encoding.UTF8.GetByteCount(tagSpan);
        Span<byte> array = stackalloc byte[8 + tagByteCount];
        BinaryPrimitives.WriteInt64BigEndian(array[..8], time);
        if (tagSpan.Length > 0)
        {
            Encoding.UTF8.GetBytes(tagSpan, array[8..]);
        }

        try
        {
            HMACSHA1.HashData(identitySecret, array, destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(array);
        }
    }

    public static byte[] GenerateConfirmationHashBytes(long time, string identitySecret, string tag = "conf")
    {
        ArgumentNullException.ThrowIfNull(identitySecret);
        var result = new byte[20];

        Span<byte> secretBuffer = stackalloc byte[64];
        if (Convert.TryFromBase64Chars(identitySecret.AsSpan(), secretBuffer, out var bytesWritten))
        {
            try
            {
                GenerateConfirmationHashBytes(time, secretBuffer[..bytesWritten], tag, result);
                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretBuffer[..bytesWritten]);
            }
        }

        var decode = Convert.FromBase64String(identitySecret);
        try
        {
            GenerateConfirmationHashBytes(time, (ReadOnlySpan<byte>)decode, tag, result);
            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decode);
        }
    }
}