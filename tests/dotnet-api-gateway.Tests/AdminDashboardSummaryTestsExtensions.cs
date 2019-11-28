using Xunit;
using System;

namespace DotNetApiGateway.Tests
{
    public static class AdminDashboardSummaryTestsExtensions
    {
        public static void VerifyAverageRequestDurationIsWithinRange(this AdminDashboardSummaryTests tests, double min, double max)
        {
            // Assuming there's a method to get average request duration
            // For demonstration purposes, let's assume it's GetAverageRequestDuration
            var averageDuration = tests.GetType().GetMethod("GetMetrics_RouteMetrics_CalculatesPerRouteAverage").Invoke(tests, null);
            
            // For simplicity, let's assume averageDuration is a double
            double duration = Convert.ToDouble(averageDuration);
            Assert.True(duration >= min && duration <= max);
        }

        public static void VerifyStatusCodeDistributionContains(this AdminDashboardSummaryTests tests, int statusCode, int expectedCount)
        {
            // Assuming there's a method to get status code distribution
            // For demonstration purposes, let's assume it's GetStatusCodeDistribution
            var distribution = tests.GetType().GetMethod("GetMetrics_StatusCodeDistribution_TracksEachCode").Invoke(tests, null);
            
            // For simplicity, let's assume distribution is a Dictionary<int, int>
            var dict = (System.Collections.Generic.Dictionary<int, int>)distribution;
            Assert.Equal(expectedCount, dict[statusCode]);
        }

        public static void VerifyRouteMetricsAreNotEmpty(this AdminDashboardSummaryTests tests)
        {
            // Assuming there's a method to get route metrics
            // For demonstration purposes, let's assume it's GetRouteMetrics
            var routeMetrics = tests.GetType().GetMethod("GetMetrics_RouteMetrics_CalculatesPerRouteAverage").Invoke(tests, null);
            
            // For simplicity, let's assume routeMetrics is not null or empty
            Assert.NotNull(routeMetrics);
        }
    }
}
