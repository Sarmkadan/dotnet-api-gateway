using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetApiGateway.Controllers;
using DotNetApiGateway.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

public class WebhookManagementControllerTests
{
    private readonly Mock<ILogger<WebhookManagementController>> _loggerMock;
    private readonly Mock<WebhookRegistry> _registryMock;
    private readonly WebhookManagementController _controller;

    public WebhookManagementControllerTests()
    {
        _loggerMock = new Mock<ILogger<WebhookManagementController>>();
        _registryMock = new Mock<WebhookRegistry>();
        _controller = new WebhookManagementController(_registryMock.Object, _loggerMock.Object);
    }

    private void SetAcceptHeader(string? value)
    {
        var httpContext = new DefaultHttpContext();
        if (value != null)
            httpContext.Request.Headers["Accept"] = value;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void CreateWebhookSubscription_InvalidRequest_ReturnsXmlWhenAcceptHeaderIsXml()
    {
        SetAcceptHeader("application/xml");
        var result = _controller.CreateWebhookSubscription(null);
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("application/xml", contentResult.ContentType);
        Assert.Contains("<error>", contentResult.Content);
    }

    [Fact]
    public void CreateWebhookSubscription_InvalidRequest_ReturnsJsonWhenNoAcceptHeader()
    {
        SetAcceptHeader(null);
        var result = _controller.CreateWebhookSubscription(null);
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorObj = badResult.Value as IDictionary<string, object>;
        Assert.NotNull(errorObj);
        Assert.True(errorObj.ContainsKey("error"));
    }
}
