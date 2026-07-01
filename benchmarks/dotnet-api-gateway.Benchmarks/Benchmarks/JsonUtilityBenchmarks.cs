using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Utilities;

namespace DotNetApiGateway.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    public class JsonUtilityBenchmarks
    {
        private class TestClass
        {
            public string Name { get; set; } = "Test";
            public int Value { get; set; } = 123;
        }

        private TestClass _obj = null!;
        private string _json = null!;

        [GlobalSetup]
        public void Setup()
        {
            _obj = new TestClass();
            _json = JsonUtility.Serialize(_obj);
        }

        [Benchmark]
        public string Serialize()
        {
            return JsonUtility.Serialize(_obj);
        }

        [Benchmark]
        public TestClass? Deserialize()
        {
            return JsonUtility.Deserialize<TestClass>(_json);
        }
    }
}
