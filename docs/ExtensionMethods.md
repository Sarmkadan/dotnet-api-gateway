# ExtensionMethods
The `ExtensionMethods` class provides a set of static methods that extend the functionality of various .NET types, including strings, collections, and byte arrays. These methods can be used to perform common operations such as string manipulation, data conversion, and collection merging, making it easier to write concise and efficient code.

## API
* `public static bool IsEmpty`: Returns a boolean indicating whether the current instance is empty. No parameters. Returns `true` if the instance is empty, `false` otherwise. Does not throw.
* `public static bool HasContent`: Returns a boolean indicating whether the current instance has content. No parameters. Returns `true` if the instance has content, `false` otherwise. Does not throw.
* `public static string Truncate`: Truncates a string to a specified length. Parameters: `string` to truncate, `int` length. Returns the truncated string. Does not throw.
* `public static string Remove`: Removes a specified substring from a string. Parameters: `string` to remove from, `string` to remove. Returns the resulting string. Does not throw.
* `public static string Repeat`: Repeats a string a specified number of times. Parameters: `string` to repeat, `int` count. Returns the repeated string. Does not throw.
* `public static byte[] ToBytes`: Converts a string to a byte array. Parameters: `string` to convert. Returns the byte array. Does not throw.
* `public static string ToHexString`: Converts a byte array to a hexadecimal string. Parameters: `byte[]` to convert. Returns the hexadecimal string. Does not throw.
* `public static bool IsEmpty<T>`: Returns a boolean indicating whether a collection is empty. Parameters: `IEnumerable<T>` collection. Returns `true` if the collection is empty, `false` otherwise. Does not throw.
* `public static bool HasElements<T>`: Returns a boolean indicating whether a collection has elements. Parameters: `IEnumerable<T>` collection. Returns `true` if the collection has elements, `false` otherwise. Does not throw.
* `public static T? GetOrDefault<T>`: Returns the first element of a collection, or a default value if the collection is empty. Parameters: `IEnumerable<T>` collection. Returns the first element, or `default(T)` if the collection is empty. Does not throw.
* `public static Dictionary<string, T> ToLowerKeyDictionary<T>`: Converts a dictionary to a new dictionary with lowercase keys. Parameters: `Dictionary<string, T>` dictionary. Returns the new dictionary. Does not throw.
* `public static Dictionary<TKey, TValue> Merge<TKey, TValue>`: Merges two dictionaries into a new dictionary. Parameters: `Dictionary<TKey, TValue>` first dictionary, `Dictionary<TKey, TValue>` second dictionary. Returns the merged dictionary. Does not throw.
* `public static string FormatMilliseconds`: Formats a time span in milliseconds as a string. Parameters: `long` milliseconds. Returns the formatted string. Does not throw.
* `public static bool MatchesAny`: Returns a boolean indicating whether a string matches any of a set of patterns. Parameters: `string` to match, `string[]` patterns. Returns `true` if the string matches any pattern, `false` otherwise. Does not throw.
* `public static long GetMemorySize`: Returns the size of an object in memory. Parameters: `object` to measure. Returns the size in bytes. Does not throw.

## Usage
```csharp
// Example 1: String manipulation
string original = "Hello, World!";
string truncated = ExtensionMethods.Truncate(original, 5);
Console.WriteLine(truncated); // Output: "Hello"

// Example 2: Collection merging
Dictionary<string, int> dict1 = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
Dictionary<string, int> dict2 = new Dictionary<string, int> { { "b", 3 }, { "c", 4 } };
Dictionary<string, int> merged = ExtensionMethods.Merge(dict1, dict2);
Console.WriteLine(merged["a"]); // Output: 1
Console.WriteLine(merged["b"]); // Output: 3
Console.WriteLine(merged["c"]); // Output: 4
```

## Notes
* The `IsEmpty` and `HasContent` methods are intended for use with strings and other sequences, and may not work as expected with other types.
* The `Truncate`, `Remove`, and `Repeat` methods modify the original string and return a new string, they do not modify the original string in place.
* The `ToBytes` and `ToHexString` methods assume that the input string or byte array is in a valid format, and may throw exceptions if the input is invalid.
* The `IsEmpty<T>` and `HasElements<T>` methods are generic and can be used with any type of collection.
* The `GetOrDefault<T>` method returns a default value if the collection is empty, which may be `null` for reference types or the default value for value types.
* The `ToLowerKeyDictionary<T>` method creates a new dictionary with lowercase keys, it does not modify the original dictionary.
* The `Merge` method merges two dictionaries into a new dictionary, it does not modify the original dictionaries.
* The `FormatMilliseconds` method formats a time span in milliseconds as a string, it does not throw exceptions.
* The `MatchesAny` method returns a boolean indicating whether a string matches any of a set of patterns, it does not throw exceptions.
* The `GetMemorySize` method returns the size of an object in memory, it does not throw exceptions.
* All methods are thread-safe, but may not be suitable for use in high-performance or real-time applications due to their potential overhead.
