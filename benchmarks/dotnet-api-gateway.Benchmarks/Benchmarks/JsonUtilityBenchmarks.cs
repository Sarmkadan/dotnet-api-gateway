using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Utilities;

namespace DotNetApiGateway.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark class for JsonUtility.
    /// </summary>
    [MemoryDiagnoser]
    public class JsonUtilityBenchmarks
    {
        /// <summary>
        /// Test class used for serialization and deserialization benchmarks.
        /// </summary>
        public class TestClass
        {
            public string Name { get; set; } = "Test";
            public int Value { get; set; } = 123;
        }

        private TestClass _obj = null!;
        private string _json = null!;

        /// <summary>
        /// Global setup method to initialize the test object and JSON string.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _obj = new TestClass();
            _json = JsonUtility.Serialize(_obj);
        }

        /// <summary>
        /// Benchmark method to measure the serialization time of the test object.
        /// </summary>
        /// <returns>The serialized JSON string.</returns>
        [Benchmark]
        public string Serialize()
        {
            return JsonUtility.Serialize(_obj);
        }

        /// <summary>
        /// Benchmark method to measure the deserialization time of the test object from the JSON string.
        /// </summary>
        /// <returns>The deserialized test object.</returns>
        [Benchmark]
        public TestClass? Deserialize()
        {
            return JsonUtility.Deserialize<TestClass>(_json);
        }
    }
}
