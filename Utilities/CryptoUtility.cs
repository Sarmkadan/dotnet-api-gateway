#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
    /// <param name="input">Input string to hash.</param>
    /// <returns>Hex-encoded SHA256 hash, or empty string if input is null or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
    public static string GenerateSha256Hash(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Generate SHA256 hash of byte array.
    /// </summary>
    /// <param name="data">Byte array to hash.</param>
    /// <returns>Hex-encoded SHA256 hash, or empty string if data is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    public static string GenerateSha256Hash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
            return string.Empty;

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashedBytes);
    }

    /// <summary>
    /// Generate HMAC-SHA256 signature for webhook verification.
    /// Secret is used as the key for HMAC generation.
    /// </summary>
    /// <param name="data">Data to sign.</param>
    /// <param name="secret">Secret key for HMAC.</param>
    /// <returns>Hex-encoded HMAC-SHA256 signature, or empty string if data or secret is null or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or secret is null.</exception>
    public static string GenerateHmacSha256(string data, string secret)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(secret);

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
    /// <param name="data">Data to sign as byte array.</param>
    /// <param name="secret">Secret key for HMAC.</param>
    /// <returns>Hex-encoded HMAC-SHA256 signature, or empty string if data is null or empty, or secret is null or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or secret is null.</exception>
    public static string GenerateHmacSha256(byte[] data, string secret)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(secret);

        if (data.Length == 0 || string.IsNullOrWhiteSpace(secret))
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
    /// <param name="data">Data to verify.</param>
    /// <param name="signature">Expected signature to compare against.</param>
    /// <param name="secret">Secret key used for HMAC generation.</param>
    /// <returns>True if signature matches, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data, signature, or secret is null.</exception>
    public static bool VerifyHmacSha256(string data, string signature, string secret)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(secret);

        if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(signature))
            return false;

        var computedSignature = GenerateHmacSha256(data, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(computedSignature));
    }

    /// <summary>
    /// Verify HMAC signature for byte array data.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="data">Data to verify as byte array.</param>
    /// <param name="signature">Expected signature to compare against.</param>
    /// <param name="secret">Secret key used for HMAC generation.</param>
    /// <returns>True if signature matches, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data, signature, or secret is null.</exception>
    public static bool VerifyHmacSha256(byte[] data, string signature, string secret)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(secret);

        if (data.Length == 0 || string.IsNullOrWhiteSpace(signature))
            return false;

        var computedSignature = GenerateHmacSha256(data, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(computedSignature));
    }

    /// <summary>
    /// Generate cryptographically secure random string of specified length.
    /// Useful for generating secrets, API keys, tokens.
    /// </summary>
    /// <param name="length">Length of the random string to generate. Default is 32.</param>
    /// <returns>Random alphanumeric string of specified length.</returns>
    /// <exception cref="ArgumentException">Thrown if length is less than or equal to 0.</exception>
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
    /// <param name="length">Number of random bytes to generate.</param>
    /// <returns>Byte array containing cryptographically secure random bytes.</returns>
    /// <exception cref="ArgumentException">Thrown if length is less than or equal to 0.</exception>
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
