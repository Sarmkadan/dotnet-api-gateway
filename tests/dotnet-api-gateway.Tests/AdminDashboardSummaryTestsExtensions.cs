using Xunit;
using System;
using System.Collections.Generic;

namespace dotnet_api_gateway.Tests
{
    /// <summary>
    /// Extension methods for verifying admin dashboard summary test results.
    /// </summary>
    public static class AdminDashboardSummaryTestsExtensions
    {
        /// <summary>
        /// Verifies that the average request duration falls within the specified range.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <param name="min">The minimum acceptable duration in milliseconds.</param>
        /// <param name="max">The maximum acceptable duration in milliseconds.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is negative or <paramref name="max"/> is less than <paramref name="min"/>.</exception>
        public static void VerifyAverageRequestDurationIsWithinRange(this AdminDashboardSummaryTests tests, double min, double max)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentOutOfRangeException.ThrowIfNegative(min);
            ArgumentOutOfRangeException.ThrowIfLessThan(max, min);

            Assert.NotNull(tests.Requests);
            var averageDuration = tests.Requests.AverageResponseTimeMs;
            Assert.True(averageDuration >= min && averageDuration <= max,
                $"Average request duration {averageDuration}ms is not within expected range [{min}ms, {max}ms]");
        }

        /// <summary>
        /// Verifies that the status code distribution contains the expected count for the specified status code.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <param name="statusCode">The HTTP status code to check.</param>
        /// <param name="expectedCount">The expected count of occurrences.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="expectedCount"/> is negative.</exception>
        public static void VerifyStatusCodeDistributionContains(this AdminDashboardSummaryTests tests, int statusCode, long expectedCount)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentOutOfRangeException.ThrowIfNegative(expectedCount);

            Assert.NotNull(tests.StatusCodeDistribution);

            if (tests.StatusCodeDistribution.TryGetValue(statusCode, out var actualCount))
            {
                Assert.Equal(expectedCount, actualCount);
            }
            else
            {
                Assert.Equal(expectedCount, 0L);
            }
        }

        /// <summary>
        /// Verifies that route metrics are not empty.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        public static void VerifyRouteMetricsAreNotEmpty(this AdminDashboardSummaryTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            Assert.NotNull(tests.Routes);
            Assert.True(tests.Routes.Total > 0, "Route metrics should contain at least one route");
        }
    }
}