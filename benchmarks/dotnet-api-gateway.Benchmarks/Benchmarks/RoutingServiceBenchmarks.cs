using BenchmarkDotNet.Attributes;
using DotNetApiGateway.Models;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Services;
using DotNetApiGateway.Constants;

namespace DotNetApiGateway.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark class for the RoutingService.
    /// </summary>
    [MemoryDiagnoser]
    public class RoutingServiceBenchmarks
    {
        private RoutingService _routingService = null!;
        private GatewayRoute _route = null!;

        /// <summary>
        /// Sets up the benchmark by creating a new RoutingService instance and a GatewayRoute instance.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var repository = new GatewayRouteRepository();
            _routingService = new RoutingService(repository);
            
            _route = new GatewayRoute
            {
                Name = "TestRoute",
                PathPattern = "/api/test",
                AllowedMethods = ["GET"],
                Targets = [
                    new RouteTarget { Name = "Target1", BaseUrl = "http://localhost:5001", Weight = 10 },
                    new RouteTarget { Name = "Target2", BaseUrl = "http://localhost:5002", Weight = 20 }
                ]
            };
        }

        /// <summary>
        /// Benchmarks the SelectTarget method of the RoutingService using a round-robin strategy.
        /// </summary>
        /// <returns>The selected RouteTarget.</returns>
        [Benchmark]
        public RouteTarget SelectTargetRoundRobin()
        {
            return _routingService.SelectTarget(_route);
        }
    }
}
