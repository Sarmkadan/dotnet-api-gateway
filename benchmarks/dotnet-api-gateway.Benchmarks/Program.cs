using BenchmarkDotNet.Running;
using DotNetApiGateway.Benchmarks.Benchmarks;

namespace DotNetApiGateway.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
