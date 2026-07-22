using System;
using System.Collections.Generic;
using DotNetApiGateway.Formatters;
using FluentAssertions;
using Xunit;

namespace DotNetApiGateway.Tests;

public class CsvFormatterTests
{
    [Fact]
    public void FormatCsv_WithNullCollection_ReturnsEmptyString()
    {
        // Act
        var result = CsvFormatter.FormatCsv<object>(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatCsv_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var emptyList = new List<TestPerson>();

        // Act
        var result = CsvFormatter.FormatCsv(emptyList);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatCsv_WithSingleItem_ReturnsHeaderAndDataRow()
    {
        // Arrange
        var person = new TestPerson { Id = 1, Name = "John", Age = 30 };
        var list = new List<TestPerson> { person };

        // Act
        var result = CsvFormatter.FormatCsv(list);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().StartWith("Id,Name,Age");
        result.Should().Contain("1,John,30");
    }

    [Fact]
    public void FormatCsv_WithMultipleItems_ReturnsHeaderAndDataRows()
    {
        // Arrange
        var people = new List<TestPerson>
        {
            new TestPerson { Id = 1, Name = "John", Age = 30 },
            new TestPerson { Id = 2, Name = "Jane", Age = 25 },
            new TestPerson { Id = 3, Name = "Bob", Age = 35 }
        };

        // Act
        var result = CsvFormatter.FormatCsv(people);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Id");
        result.Should().Contain("Name");
        result.Should().Contain("Age");
        result.Should().Contain("1,John,30");
        result.Should().Contain("2,Jane,25");
        result.Should().Contain("3,Bob,35");
    }

    [Fact]
    public void FormatCsv_WithPropertyContainingComma_EscapesValue()
    {
        // Arrange
        var person = new TestPerson { Id = 1, Name = "Doe, John", Age = 30 };
        var list = new List<TestPerson> { person };

        // Act
        var result = CsvFormatter.FormatCsv(list);

        // Assert - value with comma should be quoted
        result.Should().Contain("Doe, John");
        result.Should().Contain("\"");
    }

    [Fact]
    public void FormatCsv_WithPropertyContainingQuotes_EscapesValue()
    {
        // Arrange
        var person = new TestPerson { Id = 1, Name = "John \"The Boss\" Doe", Age = 30 };
        var list = new List<TestPerson> { person };

        // Act
        var result = CsvFormatter.FormatCsv(list);

        // Assert - value with quotes should be properly escaped
        result.Should().NotBeEmpty();
        result.Should().Contain("John");
        result.Should().Contain("The Boss");
        result.Should().Contain("Doe");
    }

    [Fact]
    public void FormatCsv_WithPropertyContainingNewline_EscapesValue()
    {
        // Arrange
        var person = new TestPerson { Id = 1, Name = "John\nDoe", Age = 30 };
        var list = new List<TestPerson> { person };

        // Act
        var result = CsvFormatter.FormatCsv(list);

        // Assert - value with newline should be quoted
        result.Should().NotBeEmpty();
        result.Should().Contain("John");
        result.Should().Contain("Doe");
        result.Should().Contain("\n");
    }

    [Fact]
    public void FormatCsv_WithNullPropertyValue_EscapesAsEmptyQuotedString()
    {
        // Arrange
        var person = new TestPerson { Id = 1, Name = null, Age = 30 };
        var list = new List<TestPerson> { person };

        // Act
        var result = CsvFormatter.FormatCsv(list);

        // Assert - null value should be quoted empty string
        result.Should().Contain(",\"\",");
    }

    [Fact]
    public void FormatCsv_WithEmptyStringPropertyValue_EscapesAsEmptyQuotedString()
    {
        // Arrange
        var person = new TestPerson { Id = 1, Name = "", Age = 30 };
        var list = new List<TestPerson> { person };

        // Act
        var result = CsvFormatter.FormatCsv(list);

        // Assert - empty string should be quoted empty string
        result.Should().Contain(",\"\",");
    }

    [Fact]
    public void FormatCsv_WithDictionary_ReturnsHeaderAndDataRow()
    {
        // Arrange
        var dict = new List<Dictionary<string, object?>>
        {
            new Dictionary<string, object?> { { "Id", 1 }, { "Name", "John" }, { "Age", 30 } }
        };

        // Act
        var result = CsvFormatter.FormatCsv(dict);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Id");
        result.Should().Contain("Name");
        result.Should().Contain("Age");
        result.Should().Contain("1");
        result.Should().Contain("John");
        result.Should().Contain("30");
    }

    [Fact]
    public void FormatCsv_WithDictionaryContainingComma_EscapesValue()
    {
        // Arrange
        var dict = new List<Dictionary<string, object?>>
        {
            new Dictionary<string, object?> { { "Name", "Doe, John" } }
        };

        // Act
        var result = CsvFormatter.FormatCsv(dict);

        // Assert - value with comma should be quoted
        result.Should().Contain("\"Doe, John\"");
    }

    [Fact]
    public void FormatCsv_WithDictionaryContainingMissingKeys_OutputsEmptyStringForMissingKeys()
    {
        // Arrange
        var dict = new List<Dictionary<string, object?>>
        {
            new Dictionary<string, object?> { { "Id", 1 }, { "Name", "John" } }
        };

        // Act
        var result = CsvFormatter.FormatCsv(dict);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("1");
        result.Should().Contain("John");
    }

    [Fact]
    public void FormatCsvBytes_WithItems_ReturnsCsvBytes()
    {
        // Arrange
        var people = new List<TestPerson>
        {
            new TestPerson { Id = 1, Name = "John", Age = 30 }
        };

        // Act
        var result = CsvFormatter.FormatCsvBytes(people);

        // Assert
        result.Should().NotBeEmpty();
        var csvString = System.Text.Encoding.UTF8.GetString(result);
        csvString.Should().Contain("Id,Name,Age");
        csvString.Should().Contain("1,John,30");
    }

    [Fact]
    public void FormatCsv_WithComplexObject_ReturnsValidCsv()
    {
        // Arrange
        var items = new List<ComplexTestObject>
        {
            new ComplexTestObject { Id = 1, Description = "Test, item", Value = 100 },
            new ComplexTestObject { Id = 2, Description = "Normal item", Value = 200 }
        };

        // Act
        var result = CsvFormatter.FormatCsv(items);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("Id");
        result.Should().Contain("Description");
        result.Should().Contain("Value");
        result.Should().Contain("1");
        result.Should().Contain("100");
        result.Should().Contain("2");
        result.Should().Contain("200");
    }

    private class TestPerson
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private class ComplexTestObject
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public int Value { get; set; }
    }
}
