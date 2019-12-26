# XmlFormatter

Provides static methods for serializing and deserializing objects to and from XML strings and byte arrays, as well as escaping and unescaping XML special characters. This utility is intended for use within the dotnet-api-gateway project to standardize XML handling across components.

## API

### `public static string Serialize<T>(T value)`

Serializes an object of type `T` into an XML string.

- **Type Parameters**  
  `T` – The type of the object to serialize. Must be a type that is XML-serializable (e.g., has a parameterless constructor and public properties/fields).

- **Parameters**  
  `value` – The object to serialize. Can be `null` for nullable reference types.

- **Returns**  
  A `string` containing the XML representation of the object. If `value` is `null`, the returned string is an empty XML element representing a null value (e.g., `<T xsi:nil="true" />`).

- **Throws**  
  `InvalidOperationException` – If the type `T` is not XML-serializable or if the serialization fails due to circular references or other structural issues.  
  `ArgumentNullException` – Not thrown; `null` is handled gracefully.

### `public static byte[] SerializeToBytes<T>(T value)`

Serializes an object of type `T` into an XML byte array using UTF-8 encoding.

- **Type Parameters**  
  `T` – The type of the object to serialize.

- **Parameters**  
  `value` – The object to serialize. Can be `null`.

- **Returns**  
  A `byte[]` containing the UTF-8 encoded XML representation of the object.

- **Throws**  
  `InvalidOperationException` – If serialization fails for the same reasons as `Serialize<T>`.

### `public static T? Deserialize<T>(string xml)`

Deserializes an XML string into an object of type `T`.

- **Type Parameters**  
  `T` – The type of the object to deserialize into. Must be XML-deserializable.

- **Parameters**  
  `xml` – The XML string to deserialize. Must not be `null` or empty.

- **Returns**  
  An object of type `T?` (nullable for reference types, non-nullable for value types). Returns `null` if the XML represents a null value (e.g., `xsi:nil="true"`).

- **Throws**  
  `ArgumentNullException` – If `xml` is `null`.  
  `ArgumentException` – If `xml` is empty or contains only whitespace.  
  `InvalidOperationException` – If the XML is malformed or cannot be deserialized into the specified type.

### `public static string EscapeXml(string value)`

Escapes characters in a string that have special meaning in XML (`<`, `>`, `&`, `"`, `'`).

- **Parameters**  
  `value` – The string to escape. Can be `null`.

- **Returns**  
  A `string` with special XML characters replaced by their corresponding XML entities (`&lt;`, `&gt;`, `&amp;`, `&quot;`, `&apos;`). If `value` is `null`, returns `null`.

- **Throws**  
  None.

### `public static string UnescapeXml(string value)`

Reverses the escaping performed by `EscapeXml`, converting XML entities back to their original characters.

- **Parameters**  
  `value` – The string to unescape. Can be `null`.

- **Returns**  
  A `string` with XML entities replaced by their actual characters. If `value` is `null`, returns `null`.

- **Throws**  
  None.

## Usage

### Example 1: Serialize and deserialize a custom object

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var person = new Person { Name = "Alice", Age = 30 };

// Serialize to XML string
string xml = XmlFormatter.Serialize(person);
Console.WriteLine(xml);
// Output: <?xml version="1.0" encoding="utf-16"?><Person><Name>Alice</Name><Age>30</Age></Person>

// Deserialize back
Person? restored = XmlFormatter.Deserialize<Person>(xml);
Console.WriteLine(restored?.Name); // Alice
```

### Example 2: Escape and unescape XML content

```csharp
string raw = "Use <tag> & \"quotes\"";
string escaped = XmlFormatter.EscapeXml(raw);
Console.WriteLine(escaped);
// Output: Use &lt;tag&gt; &amp; &quot;quotes&quot;

string unescaped = XmlFormatter.UnescapeXml(escaped);
Console.WriteLine(unescaped);
// Output: Use <tag> & "quotes"
```

## Notes

- **Thread safety**: All members are static and do not maintain any shared state. The methods are thread-safe as long as the input parameters are not mutated concurrently by other code.
- **Null handling**: `Serialize<T>` and `SerializeToBytes<T>` accept `null` values and produce an XML element with `xsi:nil="true"`. `Deserialize<T>` returns `null` when the input XML represents a null value. `EscapeXml` and `UnescapeXml` return `null` when given `null`.
- **Encoding**: `SerializeToBytes<T>` always uses UTF-8 encoding. The `Serialize<T>` method uses the default encoding of the underlying `XmlWriter` (typically UTF-16). For consistent encoding across serialization forms, prefer `SerializeToBytes<T>` when byte output is required.
- **Type constraints**: The type `T` must be XML-serializable. This typically requires a parameterless constructor and public read/write properties or fields. Types implementing `IXmlSerializable` are also supported. Deserialization of anonymous types is not supported.
- **Malformed XML**: `Deserialize<T>` throws `InvalidOperationException` if the XML is not well-formed or does not match the expected schema of `T`. No additional validation is performed beyond the standard `XmlSerializer` behavior.
- **Performance**: Serialization and deserialization use the standard `System.Xml.Serialization.XmlSerializer`. For high-throughput scenarios, consider caching `XmlSerializer` instances if the same type is used repeatedly.
