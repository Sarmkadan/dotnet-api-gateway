#nullable enable

namespace DotNetApiGateway.Tests.Utilities;

using System.Text;
using DotNetApiGateway.Utilities;
using FluentAssertions;

public class CryptoUtilityTests
{
    public class GenerateSha256Hash
    {
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

    public class GenerateHmacSha256
    {
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

    public class VerifyHmacSha256
    {
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

    public class ConstantTimeCompare
    {
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

    public class GenerateRandomString
    {
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

        [Fact]
        public void GenerateRandomString_WithDefaultLength_ReturnsStringOfLength32()
        {
            // Arrange

            // Act
            var result = CryptoUtility.GenerateRandomString();

            // Assert
            result.Should().HaveLength(32);
        }

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

    public class GenerateRandomBytes
    {
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
