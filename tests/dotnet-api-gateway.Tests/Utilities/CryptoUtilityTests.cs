#nullable enable

namespace DotNetApiGateway.Tests.Utilities;

using System.Text;
using DotNetApiGateway.Utilities;
using FluentAssertions;

/// <summary>
/// Test class for CryptoUtility utilities.
/// </summary>
public class CryptoUtilityTests
{
    /// <summary>
    /// Tests for the GenerateSha256Hash method.
    /// </summary>
    public class GenerateSha256Hash
    {
        /// <summary>
        /// Verifies that GenerateSha256Hash returns the correct hash for a valid non-empty string input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithValidString_ReturnsCorrectHash()
        {
            // Arrange
            var input = "Hello, World!";
            var expectedHash = "DFFD6021BB2BD5B0AF676290809EC3A53191DD81C7F70A4B28688A362182986F";

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().Be(expectedHash, "SHA256 hash should match expected value for given input");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash returns an empty string when given an empty string input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().BeEmpty("empty string input should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash returns an empty string when given a whitespace-only string input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithWhitespaceString_ReturnsEmptyString()
        {
            // Arrange
            var input = "   ";

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().BeEmpty("whitespace-only string input should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash returns an empty string when given a null string input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithNullString_ReturnsEmptyString()
        {
            // Arrange
            string? input = null;

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().BeEmpty("null string input should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash returns the correct hash for a valid byte array input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithValidByteArray_ReturnsCorrectHash()
        {
            // Arrange
            var input = Encoding.UTF8.GetBytes("Hello, World!");
            var expectedHash = "DFFD6021BB2BD5B0AF676290809EC3A53191DD81C7F70A4B28688A362182986F";

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().Be(expectedHash, "SHA256 hash should match expected value for given byte array");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash returns an empty string when given an empty byte array input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithEmptyByteArray_ReturnsEmptyString()
        {
            // Arrange
            var input = Array.Empty<byte>();

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().BeEmpty("empty byte array input should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash returns an empty string when given a null byte array input.
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_WithNullByteArray_ReturnsEmptyString()
        {
            // Arrange
            byte[]? input = null;

            // Act
            var result = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result.Should().BeEmpty("null byte array input should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateSha256Hash produces the same hash for the same input across multiple calls (deterministic).
        /// </summary>
        [Fact]
        public void GenerateSha256Hash_Deterministic_ReturnsSameHashForSameInput()
        {
            // Arrange
            var input = "Test input for determinism";

            // Act
            var result1 = CryptoUtility.GenerateSha256Hash(input);
            var result2 = CryptoUtility.GenerateSha256Hash(input);
            var result3 = CryptoUtility.GenerateSha256Hash(input);

            // Assert
            result1.Should().Be(result2, "multiple calls should produce same hash for same input");
            result2.Should().Be(result3, "multiple calls should produce same hash for same input");
        }
    }

    /// <summary>
    /// Tests for the GenerateHmacSha256 method.
    /// </summary>
    public class GenerateHmacSha256
    {
        /// <summary>
        /// Verifies that GenerateHmacSha256 returns the correct signature for valid data and secret.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithValidDataAndSecret_ReturnsCorrectSignature()
        {
            // Arrange
            var data = "test data";
            var secret = "secret key";

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().NotBeEmpty("HMAC-SHA256 should produce non-empty signature");
            result.Should().HaveLength(64);
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 returns an empty string when given empty data.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithEmptyData_ReturnsEmptyString()
        {
            // Arrange
            var data = string.Empty;
            var secret = "secret key";

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().BeEmpty("empty data should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 returns an empty string when given whitespace-only data.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithWhitespaceData_ReturnsEmptyString()
        {
            // Arrange
            var data = "   ";
            var secret = "secret key";

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().BeEmpty("whitespace-only data should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 returns an empty string when given null data.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithNullData_ReturnsEmptyString()
        {
            // Arrange
            string? data = null;
            var secret = "secret key";

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().BeEmpty("null data should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 returns an empty string when given an empty secret.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithEmptySecret_ReturnsEmptyString()
        {
            // Arrange
            var data = "test data";
            var secret = string.Empty;

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().BeEmpty("empty secret should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 returns an empty string when given a null secret.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithNullSecret_ReturnsEmptyString()
        {
            // Arrange
            var data = "test data";
            string? secret = null;

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().BeEmpty("null secret should return empty string");
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 returns the correct signature for valid byte array data and secret.
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_WithValidByteArrayData_ReturnsCorrectSignature()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("test data");
            var secret = "secret key";

            // Act
            var result = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result.Should().NotBeEmpty("HMAC-SHA256 with byte array should produce non-empty signature");
            result.Should().HaveLength(64);
        }

        /// <summary>
        /// Verifies that GenerateHmacSha256 produces the same signature for the same input across multiple calls (deterministic).
        /// </summary>
        [Fact]
        public void GenerateHmacSha256_Deterministic_ReturnsSameSignatureForSameInput()
        {
            // Arrange
            var data = "test data";
            var secret = "secret key";

            // Act
            var result1 = CryptoUtility.GenerateHmacSha256(data, secret);
            var result2 = CryptoUtility.GenerateHmacSha256(data, secret);
            var result3 = CryptoUtility.GenerateHmacSha256(data, secret);

            // Assert
            result1.Should().Be(result2, "multiple calls should produce same signature for same input");
            result2.Should().Be(result3, "multiple calls should produce same signature for same input");
        }
    }

    /// <summary>
    /// Tests for the VerifyHmacSha256 method.
    /// </summary>
    public class VerifyHmacSha256
    {
        /// <summary>
        /// Verifies that VerifyHmacSha256 returns true when given a correct signature.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithCorrectSignature_ReturnsTrue()
        {
            // Arrange
            var data = "webhook payload";
            var secret = "my secret key";
            var computedSignature = CryptoUtility.GenerateHmacSha256(data, secret);

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, computedSignature, secret);

            // Assert
            result.Should().BeTrue("correct signature should verify successfully");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given an incorrect signature.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithIncorrectSignature_ReturnsFalse()
        {
            // Arrange
            var data = "webhook payload";
            var secret = "my secret key";
            var wrongSignature = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, wrongSignature, secret);

            // Assert
            result.Should().BeFalse("incorrect signature should fail verification");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given empty data.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithEmptyData_ReturnsFalse()
        {
            // Arrange
            var data = string.Empty;
            var signature = "signature";
            var secret = "secret";

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeFalse("empty data should return false");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given null data.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithNullData_ReturnsFalse()
        {
            // Arrange
            string? data = null;
            var signature = "signature";
            var secret = "secret";

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeFalse("null data should return false");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given an empty signature.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithEmptySignature_ReturnsFalse()
        {
            // Arrange
            var data = "data";
            var signature = string.Empty;
            var secret = "secret";

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeFalse("empty signature should return false");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given a null signature.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithNullSignature_ReturnsFalse()
        {
            // Arrange
            var data = "data";
            string? signature = null;
            var secret = "secret";

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeFalse("null signature should return false");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given an empty secret.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithEmptySecret_ReturnsFalse()
        {
            // Arrange
            var data = "data";
            var signature = "signature";
            var secret = string.Empty;

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeFalse("empty secret should return false");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given a null secret.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithNullSecret_ReturnsFalse()
        {
            // Arrange
            var data = "data";
            var signature = "signature";
            string? secret = null;

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeFalse("null secret should return false");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns true when given byte array data and correct signature.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithByteArrayDataAndCorrectSignature_ReturnsTrue()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("webhook payload");
            var secret = "my secret key";
            var computedSignature = CryptoUtility.GenerateHmacSha256(data, secret);

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, computedSignature, secret);

            // Assert
            result.Should().BeTrue("correct signature with byte array should verify successfully");
        }

        /// <summary>
        /// Verifies that VerifyHmacSha256 returns false when given byte array data and incorrect signature.
        /// </summary>
        [Fact]
        public void VerifyHmacSha256_WithByteArrayDataAndIncorrectSignature_ReturnsFalse()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("webhook payload");
            var secret = "my secret key";
            var wrongSignature = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, wrongSignature, secret);

            // Assert
            result.Should().BeFalse("incorrect signature with byte array should fail verification");
        }
    }

    /// <summary>
    /// Tests for the ConstantTimeCompare method (via VerifyHmacSha256).
    /// </summary>
    public class ConstantTimeCompare
    {
        /// <summary>
        /// Verifies that ConstantTimeCompare returns true when comparing identical strings.
        /// </summary>
        [Fact]
        public void ConstantTimeCompare_WithSameStrings_ReturnsTrue()
        {
            // Arrange
            var data = "test";
            var secret = "secret";
            var signature = CryptoUtility.GenerateHmacSha256(data, secret);

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data, signature, secret);

            // Assert
            result.Should().BeTrue("same strings should compare as equal");
        }

        /// <summary>
        /// Verifies that ConstantTimeCompare returns false when comparing different strings.
        /// </summary>
        [Fact]
        public void ConstantTimeCompare_WithDifferentStrings_ReturnsFalse()
        {
            // Arrange
            var data1 = "test data";
            var data2 = "different data";
            var secret = "secret";
            var signature1 = CryptoUtility.GenerateHmacSha256(data1, secret);
            var signature2 = CryptoUtility.GenerateHmacSha256(data2, secret);

            // Act
            var result = CryptoUtility.VerifyHmacSha256(data1, signature2, secret);

            // Assert
            result.Should().BeFalse("different strings should compare as not equal");
        }

        /// <summary>
        /// Verifies that ConstantTimeCompare returns false when comparing null and non-null strings.
        /// </summary>
        [Fact]
        public void ConstantTimeCompare_WithNullAndNonNull_ReturnsFalse()
        {
            // Arrange
            var secret = "secret";

            // Act
            var result = CryptoUtility.VerifyHmacSha256("data", "signature", secret);

            // Assert
            result.Should().BeFalse("invalid signature should return false");
        }
    }

    /// <summary>
    /// Tests for the GenerateRandomString method.
    /// </summary>
    public class GenerateRandomString
    {
        /// <summary>
        /// Verifies that GenerateRandomString returns a string of the specified length.
        /// </summary>
        [Fact]
        public void GenerateRandomString_WithValidLength_ReturnsStringOfCorrectLength()
        {
            // Arrange
            var length = 32;

            // Act
            var result = CryptoUtility.GenerateRandomString(length);

            // Assert
            result.Should().HaveLength(length);
            result.Should().MatchRegex("^[A-Za-z0-9]+");
        }

        /// <summary>
        /// Verifies that GenerateRandomString with default length returns a string of length 32.
        /// </summary>
        [Fact]
        public void GenerateRandomString_WithDefaultLength_ReturnsStringOfLength32()
        {
            // Arrange

            // Act
            var result = CryptoUtility.GenerateRandomString();

            // Assert
            result.Should().HaveLength(32);
        }

        /// <summary>
        /// Verifies that GenerateRandomString with length 1 returns a single character string.
        /// </summary>
        [Fact]
        public void GenerateRandomString_WithLength1_ReturnsSingleCharacter()
        {
            // Arrange
            var length = 1;

            // Act
            var result = CryptoUtility.GenerateRandomString(length);

            // Assert
            result.Should().HaveLength(1);
        }

        /// <summary>
        /// Verifies that GenerateRandomString throws ArgumentException when length is zero.
        /// </summary>
        [Fact]
        public void GenerateRandomString_WithLength0_ThrowsArgumentException()
        {
            // Arrange
            var length = 0;

            // Act
            Action act = () => CryptoUtility.GenerateRandomString(length);

            // Assert
            act.Should().Throw<ArgumentException>("length must be greater than 0");
        }

        /// <summary>
        /// Verifies that GenerateRandomString throws ArgumentException when length is negative.
        /// </summary>
        [Fact]
        public void GenerateRandomString_WithNegativeLength_ThrowsArgumentException()
        {
            // Arrange
            var length = -1;

            // Act
            Action act = () => CryptoUtility.GenerateRandomString(length);

            // Assert
            act.Should().Throw<ArgumentException>("length must be greater than 0");
        }

        /// <summary>
        /// Verifies that GenerateRandomString returns different values on multiple calls.
        /// </summary>
        [Fact]
        public void GenerateRandomString_ReturnsDifferentValuesOnMultipleCalls()
        {
            // Arrange

            // Act
            var result1 = CryptoUtility.GenerateRandomString();
            var result2 = CryptoUtility.GenerateRandomString();
            var result3 = CryptoUtility.GenerateRandomString();

            // Assert
            result1.Should().NotBe(result2, "different calls should produce different random strings");
            result2.Should().NotBe(result3, "different calls should produce different random strings");
        }
    }

    /// <summary>
    /// Tests for the GenerateRandomBytes method.
    /// </summary>
    public class GenerateRandomBytes
    {
        /// <summary>
        /// Verifies that GenerateRandomBytes returns a byte array of the specified length.
        /// </summary>
        [Fact]
        public void GenerateRandomBytes_WithValidLength_ReturnsByteArrayOfCorrectLength()
        {
            // Arrange
            var length = 32;

            // Act
            var result = CryptoUtility.GenerateRandomBytes(length);

            // Assert
            result.Should().HaveCount(length, "generated byte array should have requested length");
        }

        /// <summary>
        /// Verifies that GenerateRandomBytes returns a single byte when length is 1.
        /// </summary>
        [Fact]
        public void GenerateRandomBytes_WithLength1_ReturnsSingleByte()
        {
            // Arrange
            var length = 1;

            // Act
            var result = CryptoUtility.GenerateRandomBytes(length);

            // Assert
            result.Should().HaveCount(1, "length 1 should produce single byte");
        }

        /// <summary>
        /// Verifies that GenerateRandomBytes throws ArgumentException when length is zero.
        /// </summary>
        [Fact]
        public void GenerateRandomBytes_WithLength0_ThrowsArgumentException()
        {
            // Arrange
            var length = 0;

            // Act
            Action act = () => CryptoUtility.GenerateRandomBytes(length);

            // Assert
            act.Should().Throw<ArgumentException>("length must be greater than 0");
        }

        /// <summary>
        /// Verifies that GenerateRandomBytes throws ArgumentException when length is negative.
        /// </summary>
        [Fact]
        public void GenerateRandomBytes_WithNegativeLength_ThrowsArgumentException()
        {
            // Arrange
            var length = -1;

            // Act
            Action act = () => CryptoUtility.GenerateRandomBytes(length);

            // Assert
            act.Should().Throw<ArgumentException>("length must be greater than 0");
        }

        /// <summary>
        /// Verifies that GenerateRandomBytes returns different values on multiple calls.
        /// </summary>
        [Fact]
        public void GenerateRandomBytes_ReturnsDifferentValuesOnMultipleCalls()
        {
            // Arrange

            // Act
            var result1 = CryptoUtility.GenerateRandomBytes(32);
            var result2 = CryptoUtility.GenerateRandomBytes(32);
            var result3 = CryptoUtility.GenerateRandomBytes(32);

            // Assert
            result1.Should().NotBeEquivalentTo(result2, "different calls should produce different random byte arrays");
            result2.Should().NotBeEquivalentTo(result3, "different calls should produce different random byte arrays");
        }
    }
}