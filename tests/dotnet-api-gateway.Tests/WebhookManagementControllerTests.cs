using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetApiGateway.Configuration;
using DotNetApiGateway.Controllers;
using DotNetApiGateway.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DotNetApiGateway.Tests;

/// <summary>
/// Unit tests for <see cref="WebhookManagementController"/> that verify webhook subscription creation and validation behavior.
/// Tests cover XML/JSON response formatting based on Accept headers and error handling for invalid requests.
/// </summary>
public class WebhookManagementControllerTests
{
	private readonly Mock<ILogger<WebhookManagementController>> _loggerMock;
	private readonly Mock<WebhookRegistry> _registryMock;
	private readonly WebhookCallbackUrlValidator _urlValidator;
	private readonly WebhookManagementController _controller;

	/// <summary>
	/// Initializes a new instance of the <see cref="WebhookManagementControllerTests"/> class.
	/// Sets up mock dependencies for <see cref="WebhookManagementController"/> testing.
	/// </summary>
	public WebhookManagementControllerTests()
	{
		_loggerMock = new Mock<ILogger<WebhookManagementController>>();
		_urlValidator = new WebhookCallbackUrlValidator(
			Options.Create(new WebhookSecurityOptions()),
			new Mock<ILogger<WebhookCallbackUrlValidator>>().Object);
		_registryMock = new Mock<WebhookRegistry>(new Mock<ILogger<WebhookRegistry>>().Object, _urlValidator);
		_controller = new WebhookManagementController(_registryMock.Object, _urlValidator, _loggerMock.Object);
	}

	private void SetAcceptHeader(string? value)
	{
		var httpContext = new DefaultHttpContext();
		if (value != null)
			httpContext.Request.Headers["Accept"] = value;
		_controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
	}

	[Fact]
	/// <summary>
	/// Tests that invalid webhook subscription requests return XML response when Accept header is 'application/xml'.
	/// Validates that the controller correctly formats error responses as XML based on client preferences.
	/// </summary>
	public async Task CreateWebhookSubscription_InvalidRequest_ReturnsXmlWhenAcceptHeaderIsXml()
	{
		SetAcceptHeader("application/xml");
		var result = await _controller.CreateWebhookSubscription(null);
		var contentResult = Assert.IsType<ContentResult>(result);
		Assert.Equal("application/xml", contentResult.ContentType);
		Assert.Contains("<error>", contentResult.Content);
	}

	[Fact]
	/// <summary>
	/// Tests that invalid webhook subscription requests return JSON error response when no Accept header is specified.
	/// Validates that the controller defaults to JSON format for error responses when client preferences are not indicated.
	/// </summary>
	public async Task CreateWebhookSubscription_InvalidRequest_ReturnsJsonWhenNoAcceptHeader()
	{
		SetAcceptHeader(null);
		var result = await _controller.CreateWebhookSubscription(null);
		var badResult = Assert.IsType<BadRequestObjectResult>(result);
		var errorObj = badResult.Value as IDictionary<string, object>;
		Assert.NotNull(errorObj);
		Assert.True(errorObj.ContainsKey("error"));
	}
}