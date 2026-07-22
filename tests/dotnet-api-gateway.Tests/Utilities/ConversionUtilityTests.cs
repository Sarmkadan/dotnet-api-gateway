using System;
using System.Globalization;
using DotNetApiGateway.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests.Utilities;

public class ConversionUtilityTests
{
    #region ToString Tests

    [Fact]
    public void ToString_NullObject_ReturnsDefaultValue()
    {
        // Arrange
        object? obj = null;
        string defaultValue = "default";

        // Act
        string result = ConversionUtility.ToString(obj, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToString_NullObject_NoDefault_ReturnsEmptyString()
    {
        // Arrange
        object? obj = null;

        // Act
        string result = ConversionUtility.ToString(obj);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToString_ValidObject_ReturnsToStringValue()
    {
        // Arrange
        object obj = 42;
        string defaultValue = "default";

        // Act
        string result = ConversionUtility.ToString(obj, defaultValue);

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void ToString_ObjectWithNullToString_ReturnsDefaultValue()
    {
        // Arrange
        var obj = new ObjectWithNullToString();
        string defaultValue = "default";

        // Act
        string result = ConversionUtility.ToString(obj, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    #endregion

    #region ToInt Tests

    [Fact]
    public void ToInt_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        int defaultValue = 99;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToInt_EmptyString_ReturnsDefaultValue()
    {
        // Arrange
        string value = string.Empty;
        int defaultValue = 99;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToInt_WhitespaceString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "   ";
        int defaultValue = 99;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToInt_ValidNumberString_ReturnsParsedValue()
    {
        // Arrange
        string value = "123";
        int defaultValue = 0;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public void ToInt_InvalidNumberString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "abc";
        int defaultValue = 99;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToInt_NegativeNumberString_ReturnsParsedValue()
    {
        // Arrange
        string value = "-456";
        int defaultValue = 0;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(-456);
    }

    [Fact]
    public void ToInt_NumberWithDecimal_ReturnsDefaultValue()
    {
        // Arrange
        string value = "12.34";
        int defaultValue = 99;

        // Act
        int result = ConversionUtility.ToInt(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    #endregion

    #region ToLong Tests

    [Fact]
    public void ToLong_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        long defaultValue = 99;

        // Act
        long result = ConversionUtility.ToLong(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToLong_ValidNumberString_ReturnsParsedValue()
    {
        // Arrange
        string value = "123456789012345";
        long defaultValue = 0;

        // Act
        long result = ConversionUtility.ToLong(value, defaultValue);

        // Assert
        result.Should().Be(123456789012345L);
    }

    [Fact]
    public void ToLong_InvalidNumberString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "abc";
        long defaultValue = 99;

        // Act
        long result = ConversionUtility.ToLong(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    #endregion

    #region ToDecimal Tests

    [Fact]
    public void ToDecimal_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        decimal defaultValue = 99.5m;

        // Act
        decimal result = ConversionUtility.ToDecimal(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToDecimal_ValidNumberString_ReturnsParsedValue()
    {
        // Arrange
        string value = "123.45";
        decimal defaultValue = 0m;

        // Act
        decimal result = ConversionUtility.ToDecimal(value, defaultValue);

        // Assert
        result.Should().Be(123.45m);
    }

    [Fact]
    public void ToDecimal_InvalidNumberString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "abc";
        decimal defaultValue = 99.5m;

        // Act
        decimal result = ConversionUtility.ToDecimal(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    #endregion

    #region ToDouble Tests

    [Fact]
    public void ToDouble_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        double defaultValue = 99.5;

        // Act
        double result = ConversionUtility.ToDouble(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToDouble_ValidNumberString_ReturnsParsedValue()
    {
        // Arrange
        string value = "123.45";
        double defaultValue = 0;

        // Act
        double result = ConversionUtility.ToDouble(value, defaultValue);

        // Assert
        result.Should().Be(123.45);
    }

    [Fact]
    public void ToDouble_InvalidNumberString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "abc";
        double defaultValue = 99.5;

        // Act
        double result = ConversionUtility.ToDouble(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    #endregion

    #region ToBoolean Tests

    [Fact]
    public void ToBoolean_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        bool defaultValue = true;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToBoolean_EmptyString_ReturnsDefaultValue()
    {
        // Arrange
        string value = string.Empty;
        bool defaultValue = true;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToBoolean_TrueString_ReturnsTrue()
    {
        // Arrange
        string value = "true";
        bool defaultValue = false;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToBoolean_FalseString_ReturnsFalse()
    {
        // Arrange
        string value = "false";
        bool defaultValue = true;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToBoolean_YesString_ReturnsTrue()
    {
        // Arrange
        string value = "yes";
        bool defaultValue = false;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToBoolean_OneString_ReturnsTrue()
    {
        // Arrange
        string value = "1";
        bool defaultValue = false;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToBoolean_OnString_ReturnsTrue()
    {
        // Arrange
        string value = "on";
        bool defaultValue = false;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ToBoolean_NoString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "no";
        bool defaultValue = true;

        // Act
        bool result = ConversionUtility.ToBoolean(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToBoolean_CaseInsensitiveTrueValues_ReturnTrue()
    {
        // Arrange
        string[] trueValues = { "TRUE", "True", "YES", "Yes", "ON", "On", "1" };
        bool defaultValue = false;

        // Act & Assert
        foreach (var value in trueValues)
        {
            bool result = ConversionUtility.ToBoolean(value, defaultValue);
            result.Should().BeTrue($"because '{value}' should be treated as true");
        }
    }

    #endregion

    #region ToDateTime Tests

    [Fact]
    public void ToDateTime_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        DateTime defaultValue = new DateTime(2023, 1, 1);

        // Act
        DateTime result = ConversionUtility.ToDateTime(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToDateTime_EmptyString_ReturnsDefaultValue()
    {
        // Arrange
        string value = string.Empty;
        DateTime defaultValue = new DateTime(2023, 1, 1);

        // Act
        DateTime result = ConversionUtility.ToDateTime(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToDateTime_ValidIsoString_ReturnsParsedValue()
    {
        // Arrange
        string value = "2023-01-15T10:30:00Z";
        DateTime defaultValue = new DateTime(2020, 1, 1);

        // Act
        DateTime result = ConversionUtility.ToDateTime(value, defaultValue);

        // Assert
        result.Should().Be(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToDateTime_ValidRfcString_ReturnsParsedValue()
    {
        // Arrange
        string value = "Sun, 15 Jan 2023 10:30:00 GMT";
        DateTime defaultValue = new DateTime(2020, 1, 1);

        // Act
        DateTime result = ConversionUtility.ToDateTime(value, defaultValue);

        // Assert
        result.Should().Be(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToDateTime_InvalidString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "not a date";
        DateTime defaultValue = new DateTime(2023, 1, 1);

        // Act
        DateTime result = ConversionUtility.ToDateTime(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToDateTime_NoDefault_ReturnsMinValueOnInvalid()
    {
        // Arrange
        string value = "not a date";

        // Act
        DateTime result = ConversionUtility.ToDateTime(value);

        // Assert
        result.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region ToGuid Tests

    [Fact]
    public void ToGuid_NullString_ReturnsDefaultValue()
    {
        // Arrange
        string? value = null;
        Guid defaultValue = Guid.NewGuid();

        // Act
        Guid result = ConversionUtility.ToGuid(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToGuid_EmptyString_ReturnsDefaultValue()
    {
        // Arrange
        string value = string.Empty;
        Guid defaultValue = Guid.NewGuid();

        // Act
        Guid result = ConversionUtility.ToGuid(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToGuid_ValidGuidString_ReturnsParsedValue()
    {
        // Arrange
        Guid expected = Guid.NewGuid();
        string value = expected.ToString();
        Guid defaultValue = Guid.NewGuid();

        // Act
        Guid result = ConversionUtility.ToGuid(value, defaultValue);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToGuid_InvalidGuidString_ReturnsDefaultValue()
    {
        // Arrange
        string value = "not-a-guid";
        Guid defaultValue = Guid.NewGuid();

        // Act
        Guid result = ConversionUtility.ToGuid(value, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void ToGuid_NoDefault_ReturnsEmptyGuidOnInvalid()
    {
        // Arrange
        string value = "not-a-guid";

        // Act
        Guid result = ConversionUtility.ToGuid(value);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    #endregion

    #region ToBase64 Tests

    [Fact]
    public void ToBase64_NullArray_ReturnsEmptyString()
    {
        // Arrange
        byte[]? data = null;

        // Act
        string result = ConversionUtility.ToBase64(data);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToBase64_EmptyArray_ReturnsEmptyString()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();

        // Act
        string result = ConversionUtility.ToBase64(data);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToBase64_ValidArray_ReturnsBase64String()
    {
        // Arrange
        byte[] data = { 1, 2, 3, 4, 5 };

        // Act
        string result = ConversionUtility.ToBase64(data);

        // Assert
        result.Should().Be("AQIDBAU=");
    }

    #endregion

    #region FromBase64 Tests

    [Fact]
    public void FromBase64_NullString_ReturnsNull()
    {
        // Arrange
        string? value = null;

        // Act
        byte[]? result = ConversionUtility.FromBase64(value);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromBase64_EmptyString_ReturnsNull()
    {
        // Arrange
        string value = string.Empty;

        // Act
        byte[]? result = ConversionUtility.FromBase64(value);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromBase64_WhitespaceString_ReturnsNull()
    {
        // Arrange
        string value = "   ";

        // Act
        byte[]? result = ConversionUtility.FromBase64(value);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromBase64_ValidBase64String_ReturnsByteArray()
    {
        // Arrange
        string value = "AQIDBAU=";
        byte[] expected = { 1, 2, 3, 4, 5 };

        // Act
        byte[]? result = ConversionUtility.FromBase64(value);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void FromBase64_InvalidBase64String_ReturnsNull()
    {
        // Arrange
        string value = "not-base64!!";

        // Act
        byte[]? result = ConversionUtility.FromBase64(value);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ConvertTo Tests

    [Fact]
    public void ConvertTo_NullObject_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        string? result = ConversionUtility.ConvertTo<string>(value);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertTo_SameType_ReturnsSameObject()
    {
        // Arrange
        string value = "test";

        // Act
        string? result = ConversionUtility.ConvertTo<string>(value);

        // Assert
        result.Should().BeSameAs(value);
    }

    [Fact]
    public void ConvertTo_CompatibleType_ReturnsConvertedObject()
    {
        // Arrange
        string value = "123";

        // Act
        string? result = ConversionUtility.ConvertTo<string>(value);

        // Assert
        result.Should().Be("123");
    }

    [Fact]
    public void ConvertTo_IncompatibleType_ReturnsNull()
    {
        // Arrange
        object value = new object();

        // Act
        string? result = ConversionUtility.ConvertTo<string>(value);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertTo_StringFromObject_ReturnsConvertedValue()
    {
        // Arrange
        object value = "42.5";

        // Act
        string? result = ConversionUtility.ConvertTo<string>(value);

        // Assert
        result.Should().Be("42.5");
    }

    #endregion

    // Helper class for testing ToString with null return
    private class ObjectWithNullToString
    {
        public override string? ToString()
        {
            return null!;
        }
    }
}