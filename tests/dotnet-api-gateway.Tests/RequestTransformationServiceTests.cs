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

namespace DotNetApiGateway.Tests;

public sealed class RequestTransformationServiceTests
{
    private static RequestTransformationService CreateService() =>
        new(NullLogger<RequestTransformationService>.Instance);

    // -------------------------------------------------------------------------
    // Request-phase tests
    // -------------------------------------------------------------------------

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
}
