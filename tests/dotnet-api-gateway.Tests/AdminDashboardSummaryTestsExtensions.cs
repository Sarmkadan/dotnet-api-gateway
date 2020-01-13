using Xunit;
using System;
using System.Collections.Generic;

namespace dotnet_api_gateway.Tests
{
    /// <summary>
    /// Provides extension methods for asserting properties of <see cref="AdminDashboardSummaryTests"/> instances in unit tests.
    /// </summary>
    public static class AdminDashboardSummaryTestsExtensions
    {
        /// <summary>
        /// Verifies that the average request duration falls within the specified inclusive range.
        /// </summary>
        /// <param name="tests">The <see cref="AdminDashboardSummaryTests"/> instance containing request data.</param>
        /// <param name="min">The minimum acceptable average duration in milliseconds.</param>
        /// <param name="max">The maximum acceptable average duration in milliseconds.</param>
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
        /// Verifies that the status‑code distribution contains the expected count for a given HTTP status code.
        /// </summary>
        /// <param name="tests">The <see cref="AdminDashboardSummaryTests"/> instance containing the distribution.</param>
        /// <param name="statusCode">The HTTP status code to look up.</param>
        /// <param name="expectedCount">The expected number of occurrences for <paramref name="statusCode"/>.</param>
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
        /// Verifies that the collection of route metrics is not empty, i.e., at least one route has been recorded.
        /// </summary>
        /// <param name="tests">The <see cref="AdminDashboardSummaryTests"/> instance containing route metrics.</param>
        public static void VerifyRouteMetricsAreNotEmpty(this AdminDashboardSummaryTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            Assert.NotNull(tests.Routes);
            Assert.True(tests.Routes.Total > 0, "Route metrics should contain at least one route");
        }
    }
}
