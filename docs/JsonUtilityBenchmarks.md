# JsonUtilityBenchmarks

Provides a simple harness for measuring the performance of JSON serialization and deserialization operations within the dotnet‑api‑gateway project. The type exposes basic metadata fields and methods that drive a single benchmark iteration, allowing consumers to configure a label, capture a numeric result, prepare the test environment, and retrieve the serialized and deserialized forms of a test payload.

## API

### Name  
`public string Name`  
Gets or sets a identifier for the benchmark run. Typical usage is to assign a descriptive label that appears in output logs or reports. The property accepts any string value; assigning `null` is permitted but may cause logging utilities to treat the benchmark as unnamed. No exceptions are thrown by the property itself.

### Value  
`public int Value`  
Gets or sets an integer metric associated with the benchmark, such as elapsed milliseconds, operation count, or a custom score. The property imposes no range restrictions; negative values are allowed if they have meaning for the specific benchmark. No exceptions are thrown.

### Setup  
`public void Setup()`  
Prepares internal state required for the serialization and deserialization methods. This method should be invoked before calling `Serialize` or `Deserialize` to ensure that any necessary objects (e.g., a `TestClass` instance) are initialized. If `Setup` is not called, the behavior of `Serialize` and `Deserialize` is undefined and may result in `NullReferenceException`. The method takes no parameters and does not return a value.

### Serialize  
`public string Serialize()`  
Serializes the internal test payload to a JSON string and returns the result. The method assumes that `Setup` has been called successfully; otherwise it may throw a `NullReferenceException` if the payload has not been initialized. On success, it returns a well‑formed JSON representation; if the underlying serializer fails (e.g., due to unsupported types), it propagates the relevant `JsonException`.

### Deserialize  
`public TestClass? Deserialize()`  
Attempts to deserialize the internal JSON representation back into a `TestClass` instance and returns the result, which may be `null` if deserialization yields a null value or if an error occurs. Like `Serialize`, this method expects `Setup` to have been called first; failure to do so may cause a `NullReferenceException`. If the JSON content is malformed or does not match the `TestClass` contract, a `JsonException` is thrown.

## Usage

```csharp
var bench = new JsonUtilityBenchmarks
{
    Name = "JsonUtilityBenchmarks-Roundtrip",
    Value = 0   // will be updated after timing
};

bench.Setup();                         // prepare the test payload
string json = bench.Serialize();       // obtain JSON representation
TestClass? obj = bench.Deserialize();  // recover object from JSON

// Example: measure round‑trip time and store in Value
var sw = System.Diagnostics.Stopwatch.StartNew();
_ = bench.Serialize();
_ = bench.Deserialize();
sw.Stop();
bench.Value = (int)sw.ElapsedMilliseconds;
```

```csharp
// Benchmarking multiple configurations in a loop
var results = new List<(string label, int ms)>();
foreach (var config in GetTestConfigs())
{
    var bench = new JsonUtilityBenchmarks { Name = config.Label };
    bench.Setup();

    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < config.Iterations; i++)
    {
        bench.Serialize();
        bench.Deserialize();
    }
    sw.Stop();

    bench.Value = (int)sw.ElapsedMilliseconds;
    results.Add((bench.Name, bench.Value));
}
```

## Notes

- The `Name` and `Value` properties are simple storage fields; they do not affect the behavior of `Serialize` or `Deserialize`.  
- `Setup` must be called exactly once per benchmark instance before any serialization or deserialization operation; calling it multiple times is safe but may reset internal state unnecessarily.  
- The class is **not thread‑safe**. Concurrent calls to `Setup`, `Serialize`, or `Deserialize` from multiple threads on the same instance can lead to race conditions and inconsistent results. For parallel benchmarking, create separate instances per thread.  
- `Serialize` returns a fresh string each invocation; the returned JSON is not cached internally.  
- `Deserialize` may return `null` if the internal JSON represents a null value or if deserialization fails and the implementation chooses to return `null` instead of throwing; consumers should check for `null` when applicable.  
- Any exception thrown by the underlying JSON serializer (e.g., `System.Text.Json.JsonException`) is not caught by these members and will propagate to the caller.  
- The `TestClass` type referenced by the `Deserialize` return type is assumed to be defined elsewhere in the project; its shape determines what constitutes a successful round‑trip. Changes to `TestClass` may affect the validity of the benchmark results.
