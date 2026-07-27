using DotNetApiGateway.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace dotnet_api_gateway.Tests
{
    public class RateLimitMetricsTests
    {
        [Fact]
        public void ThrottleRate_ClientWithNoRequests_ReturnsZero()
        {
            // Arrange
            var metrics = new RateLimitMetrics();

            // Act
            var throttleRate = metrics.ThrottleRate("client1");

            // Assert
            Assert.Equal(0, throttleRate);
        }

        [Fact]
        public void ThrottleRate_ClientWithRequests_ReturnsThrottleRate()
        {
            // Arrange
            var metrics = new RateLimitMetrics();
            metrics.RecordRequest("client1", limited: true);
            metrics.RecordRequest("client1");

            // Act
            var throttleRate = metrics.ThrottleRate("client1");

            // Assert
            Assert.Equal(50, throttleRate);
        }

        [Fact]
        public void TopOffenders_NoClients_ReturnsEmptyList()
        {
            // Arrange
            var metrics = new RateLimitMetrics();

            // Act
            var topOffenders = metrics.TopOffenders(10);

            // Assert
            Assert.Empty(topOffenders);
        }

        [Fact]
        public void TopOffenders_ClientsWithRequests_ReturnsTopOffenders()
        {
            // Arrange
            var metrics = new RateLimitMetrics();
            metrics.RecordRequest("client1", limited: true);
            metrics.RecordRequest("client2", limited: true);
            metrics.RecordRequest("client2", limited: true);

            // Act
            var topOffenders = metrics.TopOffenders(10);

            // Assert
            Assert.Single(topOffenders);
            Assert.Equal("client2", topOffenders[0].ClientId);
        }

        [Fact]
        public void ToSummary_NoClients_ReturnsSummary()
        {
            // Arrange
            var metrics = new RateLimitMetrics();

            // Act
            var summary = metrics.ToSummary();

            // Assert
            Assert.Contains("Total Clients: 0", summary);
        }

        [Fact]
        public void ToSummary_ClientsWithRequests_ReturnsSummary()
        {
            // Arrange
            var metrics = new RateLimitMetrics();
            metrics.RecordRequest("client1");
            metrics.RecordRequest("client2");

            // Act
            var summary = metrics.ToSummary();

            // Assert
            Assert.Contains("Total Clients: 2", summary);
            Assert.Contains("Total Requests: 2", summary);
        }
    }
}
