# ConversionUtility
The `ConversionUtility` class provides a set of static methods for converting between different data types, including numeric, string, and date/time types. It also includes methods for converting to and from base64-encoded strings and for converting between any two types using the `ConvertTo` method. These methods can be used to simplify type conversion code and reduce the risk of errors.

## API
* `public static string ToString`: Converts an object to a string. Parameters: none (extension method), Return value: a string representation of the object.
* `public static int ToInt`: Converts an object to a 32-bit integer. Parameters: none (extension method), Return value: the integer value of the object, Throws: `FormatException` if the object cannot be converted to an integer.
* `public static long ToLong`: Converts an object to a 64-bit integer. Parameters: none (extension method), Return value: the long integer value of the object, Throws: `FormatException` if the object cannot be converted to a long integer.
* `public static decimal ToDecimal`: Converts an object to a decimal. Parameters: none (extension method), Return value: the decimal value of the object, Throws: `FormatException` if the object cannot be converted to a decimal.
* `public static double ToDouble`: Converts an object to a double-precision floating-point number. Parameters: none (extension method), Return value: the double value of the object, Throws: `FormatException` if the object cannot be converted to a double.
* `public static bool ToBoolean`: Converts an object to a boolean. Parameters: none (extension method), Return value: the boolean value of the object, Throws: `FormatException` if the object cannot be converted to a boolean.
* `public static DateTime ToDateTime`: Converts an object to a date and time. Parameters: none (extension method), Return value: the date and time value of the object, Throws: `FormatException` if the object cannot be converted to a date and time.
* `public static Guid ToGuid`: Converts an object to a globally unique identifier. Parameters: none (extension method), Return value: the Guid value of the object, Throws: `FormatException` if the object cannot be converted to a Guid.
* `public static string ToBase64`: Converts a byte array to a base64-encoded string. Parameters: none (extension method), Return value: the base64-encoded string.
* `public static byte[]? FromBase64`: Converts a base64-encoded string to a byte array. Parameters: none (extension method), Return value: the byte array, or null if the string is not a valid base64-encoded string.
* `public static T? ConvertTo<T>`: Converts an object to the specified type. Parameters: none (extension method), Return value: the converted object, or null if the conversion fails, Throws: `InvalidCastException` if the object cannot be converted to the specified type.

## Usage
The following examples demonstrate how to use the `ConversionUtility` class:
```csharp
// Convert a string to an integer
string str = "123";
int num = ConversionUtility.ToInt(str);
Console.WriteLine(num); // Output: 123

// Convert a byte array to a base64-encoded string
byte[] bytes = { 1, 2, 3, 4, 5 };
string base64 = ConversionUtility.ToBase64(bytes);
Console.WriteLine(base64); // Output: a base64-encoded string
```

## Notes
The `ConversionUtility` class is designed to be thread-safe, as all of its methods are static and do not access any shared state. However, the `ConvertTo` method may throw an `InvalidCastException` if the object being converted is not compatible with the target type. Additionally, the `ToBase64` and `FromBase64` methods may throw a `FormatException` if the input string is not a valid base64-encoded string. It is also worth noting that the `ConvertTo` method uses the `Convert.ChangeType` method under the hood, which may not always produce the desired results for certain types (e.g. enums). In such cases, explicit casting or custom conversion logic may be necessary.
