#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetApiGateway.Models;
using DotNetApiGateway.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Provides unit tests for the <see cref="DotNetApiGateway.Utilities.JsonUtility"/> class.
/// Tests various JSON serialization, deserialization, and utility methods.
/// </summary>
public sealed class JsonUtilityTests
{
    /// <summary>
    /// Gets or sets the name property used for testing serialization.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the age property used for testing serialization.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets an optional field used for testing nullable field handling.
    /// </summary>
    public string? OptionalField { get; set; }

    /// <summary>
    /// Represents a test object used for JSON serialization/deserialization testing.
    /// </summary>
    private class TestObject
    {
        /// <summary>
        /// Gets or sets the name property used for testing serialization.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the age property used for testing serialization.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets an optional field used for testing nullable field handling.
        /// </summary>
        public string? OptionalField { get; set; }
    }

    /// <summary>
    /// Tests that serializing a valid object returns a non-empty JSON string.
    /// </summary>
    [Fact]
    public void Serialize_ValidObject_ReturnsJsonString()
    {
        // Arrange
        var obj = new TestObject { Name = "John", Age = 30 };

        // Act
        var json = JsonUtility.Serialize(obj);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"name\""); // camelCase property name
        json.Should().Contain("John");
        json.Should().Contain("30");
    }

    /// <summary>
    /// Tests that serializing an object with a null nullable field omits that field from the JSON output.
    /// </summary>
    [Fact]
    public void Serialize_WithNullableFieldNull_OmitsField()
    {
        // Arrange
        var obj = new TestObject { Name = "Jane", Age = 25, OptionalField = null };

        // Act
        var json = JsonUtility.Serialize(obj);

        // Assert
        json.Should().NotContain("optionalField");
    }

    /// <summary>
    /// Tests that serializing a valid object with the pretty printer returns formatted JSON with newlines.
    /// </summary>
    [Fact]
    public void SerializePretty_ValidObject_ReturnsFormattedJson()
    {
        // Arrange
        var obj = new TestObject { Name = "Bob", Age = 40 };

        // Act
        var json = JsonUtility.SerializePretty(obj);

        // Assert
        json.Should().Contain("\n"); // Contains newlines (formatted)
        json.Should().Contain("\"name\""); // camelCase property name
        json.Should().Contain("Bob");
    }

    /// <summary>
    /// Tests that deserializing valid JSON returns a properly populated object.
    /// </summary>
    [Fact]
    public void Deserialize_ValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"name\": \"Alice\", \"age\": 28}";

        // Act
        var obj = JsonUtility.Deserialize<TestObject>(json);

        // Assert
        obj.Should().NotBeNull();
        obj!.Name.Should().Be("Alice");
        obj.Age.Should().Be(28);
    }

    /// <summary>
    /// Tests that deserializing invalid JSON throws a JsonException.
    /// </summary>
    [Fact]
    public void Deserialize_InvalidJson_ThrowsException()
    {
        // Arrange
        var json = "{invalid json}";

        // Act
        var act = () => JsonUtility.Deserialize<TestObject>(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    /// <summary>
    /// Tests that deserializing an empty string returns null.
    /// </summary>
    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        // Act
        var obj = JsonUtility.Deserialize<TestObject>("");

        // Assert
        obj.Should().BeNull();
    }

    /// <summary>
    /// Tests that deserializing a whitespace-only string returns null.
    /// </summary>
    [Fact]
    public void Deserialize_WhitespaceString_ReturnsNull()
    {
        // Act
        var obj = JsonUtility.Deserialize<TestObject>(" ");

        // Assert
        obj.Should().BeNull();
    }

    /// <summary>
    /// Tests that deserializing valid JSON with the safe method returns a properly populated object.
    /// </summary>
    [Fact]
    public void DeserializeSafe_ValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"name\": \"Charlie\", \"age\": 35}";

        // Act
        var obj = JsonUtility.DeserializeSafe<TestObject>(json);

        // Assert
        obj.Should().NotBeNull();
        obj!.Name.Should().Be("Charlie");
    }

    /// <summary>
    /// Tests that deserializing invalid JSON with the safe method returns null instead of throwing.
    /// </summary>
    [Fact]
    public void DeserializeSafe_InvalidJson_ReturnsNull()
    {
        // Arrange
        var json = "{invalid}";

        // Act
        var obj = JsonUtility.DeserializeSafe<TestObject>(json);

        // Assert
        obj.Should().BeNull();
    }

    /// <summary>
    /// Tests that parsing valid JSON returns a JsonElement representing the JSON object.
    /// </summary>
    [Fact]
    public void ParseDynamic_ValidJson_ReturnsJsonElement()
    {
        // Arrange
        var json = "{\"name\": \"David\", \"age\": 45}";

        // Act
        var element = JsonUtility.ParseDynamic(json);

        // Assert
        element.Should().NotBeNull();
        element!.Value.ValueKind.Should().Be(JsonValueKind.Object);
    }

    /// <summary>
    /// Tests that parsing invalid JSON returns null.
    /// </summary>
    [Fact]
    public void ParseDynamic_InvalidJson_ReturnsNull()
    {
        // Arrange
        var json = "{invalid}";

        // Act
        var element = JsonUtility.ParseDynamic(json);

        // Assert
        element.Should().BeNull();
    }

    /// <summary>
    /// Tests that parsing valid JSON array returns a JsonElement representing the JSON array.
    /// </summary>
    [Fact]
    public void ParseDynamic_ValidArray_ReturnsArray()
    {
        // Arrange
        var json = "[1, 2, 3]";

        // Act
        var element = JsonUtility.ParseDynamic(json);

        // Assert
        element.Should().NotBeNull();
        element!.Value.ValueKind.Should().Be(JsonValueKind.Array);
    }

    /// <summary>
    /// Tests that the IsValidJson method correctly validates various JSON inputs.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <param name="expected">The expected validation result (true for valid JSON, false for invalid).</param>
    [Theory]
    [InlineData("{\"name\": \"Eve\"}", true)]
    [InlineData("[1, 2, 3]", true)]
    [InlineData("\"string\"", true)]
    [InlineData("123", true)]
    [InlineData("true", true)]
    [InlineData("null", true)]
    [InlineData("{invalid}", false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void IsValidJson_VariousInputs_ReturnsExpected(string json, bool expected)
    {
        JsonUtility.IsValidJson(json).Should().Be(expected);
    }

    /// <summary>
    /// Tests that merging two valid JSON strings returns a merged JSON string with properties from both.
    /// </summary>
    [Fact]
    public void MergeJson_BothValidJson_ReturnsMergedJson()
    {
        // Arrange
        var json1 = "{\"name\": \"Frank\", \"age\": 50}";
        var json2 = "{\"age\": 51, \"city\": \"NYC\"}";

        // Act
        var merged = JsonUtility.MergeJson(json1, json2);

        // Assert
        merged.Should().NotBeNullOrEmpty();
        merged.Should().Contain("Frank"); // kept from first document
        merged.Should().Contain("51"); // overwritten by second document
        merged.Should().Contain("NYC"); // added by second document
    }

    /// <summary>
    /// Tests that merging when the first JSON is invalid returns the first JSON unchanged.
    /// </summary>
    [Fact]
    public void MergeJson_FirstJsonInvalid_ReturnsFirstJson()
    {
        // Arrange
        var json1 = "{invalid}";
        var json2 = "{\"valid\": \"json\"}";

        // Act
        var merged = JsonUtility.MergeJson(json1, json2);

        // Assert
        merged.Should().Be(json1);
    }

    /// <summary>
    /// Tests that merging when the second JSON is invalid returns the first JSON unchanged.
    /// </summary>
    [Fact]
    public void MergeJson_SecondJsonInvalid_ReturnsFirstJson()
    {
        // Arrange
        var json1 = "{\"key\": \"value\"}";
        var json2 = "{invalid}";

        // Act
        var merged = JsonUtility.MergeJson(json1, json2);

        // Assert
        merged.Should().Be(json1);
    }

    /// <summary>
    /// Tests that serializing an enumeration value serializes it as a camelCase string.
    /// </summary>
    [Fact]
    public void Serialize_CaseSensitiveEnumeration_SerializesAsString()
    {
        // Arrange
        var policy = new RateLimitPolicy
        {
            Enabled = true,
            Strategy = RateLimitStrategy.SlidingWindow
        };

        // Act
        var json = JsonUtility.Serialize(policy);

        // Assert
        json.Should().Contain("slidingWindow"); // camelCase enum
    }

    /// <summary>
    /// Tests that deserializing JSON with different case property names handles case-insensitive deserialization.
    /// </summary>
    [Fact]
    public void Deserialize_CaseInsensitiveDeserialization_HandlesVariousCases()
    {
        // Arrange
        var json = "{\"NAME\": \"Upper\", \"AGE\": 25}";

        // Act
        var obj = JsonUtility.Deserialize<TestObject>(json);

        // Assert
        obj.Should().NotBeNull();
        obj!.Name.Should().Be("Upper");
    }

    /// <summary>
    /// Tests that serializing and deserializing an object is idempotent - the output should be identical.
    /// </summary>
    [Fact]
    public void Serialize_ComplexNestedObject_SerializesCorrectly()
    {
        // Arrange
        var obj = new TestObject { Name = "Nested", Age = 99 };
        var json1 = JsonUtility.Serialize(obj);

        // Act
        var deserialized = JsonUtility.Deserialize<TestObject>(json1);
        var json2 = JsonUtility.Serialize(deserialized!);

        // Assert
        json1.Should().Be(json2); // Serialize->Deserialize->Serialize should be idempotent
    }

    /// <summary>
    /// Tests that deserializing null JSON returns null.
    /// </summary>
    [Fact]
    public void Deserialize_NullJson_ReturnsNull()
    {
        // Act
        var obj = JsonUtility.Deserialize<TestObject>(null!);

        // Assert
        obj.Should().BeNull();
    }

    /// <summary>
    /// Tests that parsing null JSON returns null.
    /// </summary>
    [Fact]
    public void ParseDynamic_NullJson_ReturnsNull()
    {
        // Act
        var element = JsonUtility.ParseDynamic(null!);

        // Assert
        element.Should().BeNull();
    }

    /// <summary>
    /// Tests that deserializing null JSON with the safe method returns null.
    /// </summary>
    [Fact]
    public void DeserializeSafe_NullJson_ReturnsNull()
    {
        // Act
        var obj = JsonUtility.DeserializeSafe<TestObject>(null!);

        // Assert
        obj.Should().BeNull();
    }
}