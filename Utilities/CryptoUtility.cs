#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Utilities;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Utility class for cryptographic operations including hashing and HMAC generation.
/// Provides secure methods for webhook signature verification and data integrity checks.
/// </summary>
public static class CryptoUtility
{
    /// <summary>
    /// Generate SHA256 hash of input string.
    /// Returns hex-encoded hash suitable for comparisons and storage.
    /// </summary>
    public static string GenerateSha256Hash(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Generate SHA256 hash of byte array.
    /// </summary>
    public static string GenerateSha256Hash(byte[] data)
    {
        if (data is null || data.Length == 0)
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Generate HMAC-SHA256 signature for webhook verification.
    /// Secret is used as the key for HMAC generation.
    /// </summary>
    public static string GenerateHmacSha256(string data, string secret)
    {
        if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(secretBytes);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Generate HMAC-SHA256 signature for byte array data.
    /// </summary>
    public static string GenerateHmacSha256(byte[] data, string secret)
    {
        if (data is null || data.Length == 0 || string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(data);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Verify HMAC signature by comparing computed hash with provided signature.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    public static bool VerifyHmacSha256(string data, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(signature))
            return false;

        var computedSignature = GenerateHmacSha256(data, secret);
        return ConstantTimeCompare(signature, computedSignature);
    }

    /// <summary>
    /// Verify HMAC signature for byte array data.
    /// </summary>
    public static bool VerifyHmacSha256(byte[] data, string signature, string secret)
    {
        if (data is null || string.IsNullOrWhiteSpace(signature))
            return false;

        var computedSignature = GenerateHmacSha256(data, secret);
        return ConstantTimeCompare(signature, computedSignature);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// Compares strings without early exit on mismatch.
    /// </summary>
    private static bool ConstantTimeCompare(string a, string b)
    {
        if (a is null || b is null)
            return a == b;

        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Generate cryptographically secure random string of specified length.
    /// Useful for generating secrets, API keys, tokens.
    /// </summary>
    public static string GenerateRandomString(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var data = new byte[length];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(data);

        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[data[i] % chars.Length]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Generate random bytes using cryptographically secure RNG.
    /// </summary>
    public static byte[] GenerateRandomBytes(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));

        var data = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(data);
        return data;
    }
}
