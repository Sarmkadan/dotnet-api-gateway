using System;
using DotNetApiGateway.Formatters;
using Xunit;

namespace DotNetApiGateway.Tests;

public class XmlFormatterTests
{
    [Fact]
    public void Serialize_NullObject_ReturnsNullElement()
    {
        var xml = XmlFormatter.Serialize<object>(null!);
        Assert.Equal("<null />", xml.Trim());
    }

    [Fact]
    public void Serialize_SimpleObject_ReturnsValidXml()
    {
        var obj = new TestClass { Id = 1, Name = "Test" };
        var xml = XmlFormatter.Serialize(obj);
        Assert.Contains("<TestClass>", xml);
        Assert.Contains("<Id>1</Id>", xml);
        Assert.Contains("<Name>Test</Name>", xml);
    }

    [Fact]
    public void Serialize_Exception_ReturnsErrorXmlWithEscapedMessage()
    {
        var xml = XmlFormatter.Serialize(new ThrowingClass());
        Assert.Contains("<error>", xml);
        Assert.Contains("<message>", xml);
        Assert.DoesNotContain("<", xml.Substring(xml.IndexOf("<message>", StringComparison.Ordinal) + "<message>".Length));
    }

    [Fact]
    public void EscapeAndUnescape_RoundTripPreservesOriginal()
    {
        var original = "Special chars: & < > \" '";
        var escaped = XmlFormatter.EscapeXml(original);
        var unescaped = XmlFormatter.UnescapeXml(escaped);
        Assert.Equal(original, unescaped);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class ThrowingClass
    {
        public ThrowingClass()
        {
            throw new InvalidOperationException("Invalid <data> & \"test\"");
        }
    }
}
