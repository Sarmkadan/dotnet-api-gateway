#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetApiGateway.Models;
using DotNetApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using HttpMethod = System.Net.Http.HttpMethod;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Tests for the RequestTransformationService class.
/// </summary>
public sealed class RequestTransformationServiceTests
{
    /// <summary>
    /// Creates a new instance of the RequestTransformationService class for testing purposes.
    /// </summary>
    /// <returns>A new instance of the RequestTransformationService class.</returns>
    private static RequestTransformationService CreateService() =>
        new(NullLogger<RequestTransformationService>.Instance);

    // -------------------------------------------------------------------------
    // Request-phase tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests that the ApplyRequestRules method appends a header when it is missing.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_AddHeader_AppendsHeaderWhenMissing()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/users");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.AddHeader,
                Key = "X-Tenant-Id",
                Value = "acme"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.TryGetValues("X-Tenant-Id", out var values).Should().BeTrue();
        values!.First().Should().Be("acme");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method does not overwrite an existing header.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_AddHeader_DoesNotOverwriteExistingHeader()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/users");
        request.Headers.TryAddWithoutValidation("X-Tenant-Id", "original");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.AddHeader,
                Key = "X-Tenant-Id",
                Value = "new-value"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.TryGetValues("X-Tenant-Id", out var values).Should().BeTrue();
        values!.First().Should().Be("original");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method replaces an existing header.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_SetHeader_ReplacesExistingHeader()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/users");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", "old-id");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.SetHeader,
                Key = "X-Correlation-Id",
                Value = "new-id"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be("new-id");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method deletes a header.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_RemoveHeader_DeletesHeader()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/users");
        request.Headers.TryAddWithoutValidation("X-Internal-Secret", "s3cr3t");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.RemoveHeader,
                Key = "X-Internal-Secret"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.Contains("X-Internal-Secret").Should().BeFalse();
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method appends a query parameter to the URI.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_SetQueryParam_AppendsParamToUri()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/search");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.SetQueryParam,
                Key = "env",
                Value = "production"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.RequestUri!.Query.Should().Contain("env=production");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method drops a query parameter from the URI.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_RemoveQueryParam_DropsParamFromUri()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/search?debug=true&q=foo");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.RemoveQueryParam,
                Key = "debug"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.RequestUri!.Query.Should().NotContain("debug");
        request.RequestUri.Query.Should().Contain("q=foo");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method adds a query parameter if it does not already exist.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_AddQueryParam_AddsParamWhenMissing()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/search");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.AddQueryParam,
                Key = "tracking",
                Value = "12345"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.RequestUri!.Query.Should().Contain("tracking=12345");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method does not overwrite an existing query parameter when using AddQueryParam.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_AddQueryParam_DoesNotOverwriteExistingParam()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api/search?tracking=existing");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.AddQueryParam,
                Key = "tracking",
                Value = "new-value"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.RequestUri!.Query.Should().Contain("tracking=existing");
        request.RequestUri.Query.Should().NotContain("new-value");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method replaces a matching prefix in the path.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_RewritePathPrefix_ReplacesMatchingPrefix()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/v2/orders/123");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.RewritePathPrefix,
                Key = "/v2",
                Value = "/api/v2"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.RequestUri!.AbsolutePath.Should().Be("/api/v2/orders/123");
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method skips a disabled rule.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_DisabledRule_IsSkipped()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Request,
                Operation = TransformationOperation.AddHeader,
                Key = "X-Should-Not-Appear",
                Value = "yes",
                IsEnabled = false
            }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.Contains("X-Should-Not-Appear").Should().BeFalse();
    }

    /// <summary>
    /// Tests that the ApplyRequestRules method does not apply a response-phase rule to a request.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_ResponsePhaseRule_IsNotAppliedToRequest()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Response,  // wrong phase
                Operation = TransformationOperation.AddHeader,
                Key = "X-Response-Only",
                Value = "yes"
            }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.Contains("X-Response-Only").Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Response-phase tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests that the ApplyResponseRules method injects a header into a response.
    /// </summary>
    [Fact]
    public void ApplyResponseRules_SetHeader_InjectsHeaderIntoResponse()
    {
        var service = CreateService();
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Response,
                Operation = TransformationOperation.SetHeader,
                Key = "X-Gateway-Version",
                Value = "2.0"
            }
        };

        service.ApplyResponseRules(response, rules);

        response.Headers.TryGetValues("X-Gateway-Version", out var values).Should().BeTrue();
        values!.First().Should().Be("2.0");
    }

    /// <summary>
    /// Tests that the ApplyResponseRules method deletes a header from a response.
    /// </summary>
    [Fact]
    public void ApplyResponseRules_RemoveHeader_DeletesHeaderFromResponse()
    {
        var service = CreateService();
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("Server", "Apache/2.4");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Response,
                Operation = TransformationOperation.RemoveHeader,
                Key = "Server"
            }
        };

        service.ApplyResponseRules(response, rules);

        response.Headers.Contains("Server").Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Rule ordering
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests that the ApplyRequestRules method applies ordered rules in order.
    /// </summary>
    [Fact]
    public void ApplyRequestRules_OrderedRules_AreAppliedInOrder()
    {
        var service = CreateService();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://backend/api");

        // Rule order 1: Set X-Count to "first"
        // Rule order 2: Override X-Count with "second"
        var rules = new List<TransformationRule>
        {
            new() { Phase = TransformationPhase.Request, Operation = TransformationOperation.SetHeader, Key = "X-Count", Value = "second", Order = 2 },
            new() { Phase = TransformationPhase.Request, Operation = TransformationOperation.SetHeader, Key = "X-Count", Value = "first",  Order = 1 }
        };

        service.ApplyRequestRules(request, rules);

        request.Headers.TryGetValues("X-Count", out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be("second");
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests that the TransformationRule Validate method throws an exception when the key is empty.
    /// </summary>
    [Fact]
    public void TransformationRule_Validate_ThrowsWhenKeyEmpty()
    {
        var rule = new TransformationRule
        {
            Operation = TransformationOperation.AddHeader,
            Key = "",
            Value = "val"
        };

        var act = () => rule.Validate();
        act.Should().Throw<ArgumentException>().WithMessage("*Key cannot be empty*");
    }

    /// <summary>
    /// Tests that the TransformationRule Validate method throws an exception when the value is missing for an add operation.
    /// </summary>
    [Fact]
    public void TransformationRule_Validate_ThrowsWhenValueMissingForAdd()
    {
        var rule = new TransformationRule
        {
            Operation = TransformationOperation.AddHeader,
            Key = "X-Foo",
            Value = null
        };

        var act = () => rule.Validate();
        act.Should().Throw<ArgumentException>().WithMessage("*Value is required*");
    }

    /// <summary>
    /// Tests that the TransformationRule Validate method does not throw an exception for a remove operation.
    /// </summary>
    [Fact]
    public void TransformationRule_Validate_DoesNotThrowForRemove()
    {
        var rule = new TransformationRule
        {
            Operation = TransformationOperation.RemoveHeader,
            Key = "X-Remove-Me"
        };

        var act = () => rule.Validate();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the ApplyResponseRules method adds a header if it does not already exist.
    /// </summary>
    [Fact]
    public void ApplyResponseRules_AddHeader_AddsHeaderWhenMissing()
    {
        var service = CreateService();
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Response,
                Operation = TransformationOperation.AddHeader,
                Key = "X-Response-Added",
                Value = "present"
            }
        };

        service.ApplyResponseRules(response, rules);

        response.Headers.TryGetValues("X-Response-Added", out var values).Should().BeTrue();
        values!.First().Should().Be("present");
    }

    /// <summary>
    /// Tests that the ApplyResponseRules method does not overwrite an existing header when using AddHeader.
    /// </summary>
    [Fact]
    public void ApplyResponseRules_AddHeader_DoesNotOverwriteExistingHeader()
    {
        var service = CreateService();
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("X-Response-Id", "original-id");

        var rules = new List<TransformationRule>
        {
            new()
            {
                Phase = TransformationPhase.Response,
                Operation = TransformationOperation.AddHeader,
                Key = "X-Response-Id",
                Value = "new-id"
            }
        };

        service.ApplyResponseRules(response, rules);

        response.Headers.TryGetValues("X-Response-Id", out var values).Should().BeTrue();
        values!.First().Should().Be("original-id");
    }
}
