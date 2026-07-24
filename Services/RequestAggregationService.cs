#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetApiGateway.Constants;
using DotNetApiGateway.Models;
using JsonCons.JsonPath;
using Microsoft.Extensions.Logging;

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for aggregating responses from multiple backend requests, optionally with conditional fan-out.
/// </summary>
public sealed class RequestAggregationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RequestAggregationService> _logger;

    public RequestAggregationService(HttpClient httpClient, ILogger<RequestAggregationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates responses from multiple backend requests based on the provided aggregation policy.
    /// Supports conditional fan-out based on the incoming request body.
    /// </summary>
    /// <param name="policy">The aggregation policy for selecting targets and strategy.</param>
    /// <param name="incomingRequestBody">The incoming HTTP request body as a string, used for JSONPath evaluation.</param>
    /// <returns>An AggregatedResponse containing results from the selected backend calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if policy is null.</exception>
    public async Task<AggregatedResponse> AggregateAsync(
        AggregationPolicy policy,
        string? incomingRequestBody)
    {
        ArgumentNullException.ThrowIfNull(policy, nameof(policy));

        var response = new AggregatedResponse();
        var startTime = DateTime.UtcNow;

        if (!policy.Enabled)
        {
            _logger.LogInformation("Aggregation policy is not enabled. Returning empty aggregated response.");
            response.TotalDuration = DateTime.UtcNow - startTime;
            return response;
        }

        JsonElement? requestJsonElement = null;
        if (!string.IsNullOrWhiteSpace(incomingRequestBody))
        {
            try
            {
                requestJsonElement = JsonDocument.Parse(incomingRequestBody).RootElement;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse incoming request body as JSON for aggregation. JSONPath conditions will not be evaluated.");
            }
        }

        var selectedTargets = SelectConditionalTargets(policy, requestJsonElement);

        if (!selectedTargets.Any())
        {
            _logger.LogInformation("No targets selected for aggregation based on policy and conditions. Returning empty aggregated response.");
            response.TotalDuration = DateTime.UtcNow - startTime;
            return response;
        }

        switch (policy.Strategy)
        {
            case AggregationStrategy.Sequential:
                await ExecuteSequentialAsync(selectedTargets, response);
                break;
            case AggregationStrategy.Parallel:
                await ExecuteParallelAsync(selectedTargets, response);
                break;
            case AggregationStrategy.FirstSuccess:
                await ExecuteFirstSuccessAsync(selectedTargets, response);
                break;
            case AggregationStrategy.FailFast:
                await ExecuteFailFastAsync(selectedTargets, response);
                break;
            case AggregationStrategy.BestEffort:
                await ExecuteBestEffortAsync(selectedTargets, response);
                break;
            default:
                _logger.LogWarning("Unsupported aggregation strategy: {Strategy}. Falling back to Parallel.", policy.Strategy);
                await ExecuteParallelAsync(selectedTargets, response);
                break;
        }

        response.TotalDuration = DateTime.UtcNow - startTime;
        return response;
    }

    private IEnumerable<ConditionalAggregationTarget> SelectConditionalTargets(AggregationPolicy policy, JsonElement? requestJsonElement)
    {
        var selected = new List<ConditionalAggregationTarget>();
        foreach (var target in policy.Targets)
        {
            if (string.IsNullOrWhiteSpace(target.JsonPathCondition))
            {
                selected.Add(target); // No condition, always select
                continue;
            }

            if (requestJsonElement is null)
            {
                _logger.LogDebug("Target '{TargetId}' has JSONPath condition but incoming request body is not JSON or empty. Skipping.", target.Id);
                continue;
            }

            try
            {
                var selector = JsonSelector.Parse(target.JsonPathCondition);
                var result = selector.Select(requestJsonElement.Value);
                if (result is not null && result.Count > 0)
                {
                    selected.Add(target);
                }
            }
            catch (JsonPathParseException ex)
            {
                _logger.LogError(ex, "Invalid JSONPath condition '{Condition}' for target '{TargetId}'. Skipping.", target.JsonPathCondition, target.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating JSONPath condition '{Condition}' for target '{TargetId}'. Skipping.", target.JsonPathCondition, target.Id);
            }
        }
        return selected;
    }

    private async Task ExecuteSequentialAsync(
        IEnumerable<ConditionalAggregationTarget> targets,
        AggregatedResponse response)
    {
        foreach (var target in targets)
        {
            try
            {
                target.Validate();
                var result = await ExecuteTargetRequestAsync(target, response.CancellationToken);
                response.AddResponse(target.Id, result.StatusCode, result.Body, result.Headers, result.Duration);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error executing sequential aggregated request for target '{TargetId}'", target.Id);
                if (!target.Optional)
                    throw;

                response.AddResponse(target.Id, 500, null, null, null, ex.Message);
            }
        }
    }

    private async Task ExecuteParallelAsync(
        IEnumerable<ConditionalAggregationTarget> targets,
        AggregatedResponse response)
    {
        var tasks = new List<Task>();
        var cts = new CancellationTokenSource();
        response.SetCancellationToken(cts);

        foreach (var target in targets)
        {
            // Create a linked cancellation token that respects both the global timeout and per-part timeout
            var timeoutCts = CreateTimeoutCancellationToken(target);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);

            var task = Task.Run(async () =>
            {
                try
                {
                    target.Validate();
                    var result = await ExecuteTargetRequestAsync(target, linkedCts.Token);
                    response.AddResponse(target.Id, result.StatusCode, result.Body, result.Headers, result.Duration);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error executing parallel aggregated request for target '{TargetId}'", target.Id);
                    if (!target.Optional)
                        throw;

                    response.AddResponse(target.Id, 500, null, null, null, ex.Message);
                }
                finally
                {
                    linkedCts.Dispose();
                    timeoutCts.Dispose();
                }
            }, cts.Token);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteFirstSuccessAsync(
        IEnumerable<ConditionalAggregationTarget> targets,
        AggregatedResponse response)
    {
        var cts = new CancellationTokenSource();
        response.SetCancellationToken(cts);

        foreach (var target in targets)
        {
            try
            {
                target.Validate();
                var result = await ExecuteTargetRequestAsync(target, cts.Token);
                response.AddResponse(target.Id, result.StatusCode, result.Body, result.Headers, result.Duration);

                if (result.StatusCode >= 200 && result.StatusCode < 300)
                {
                    _logger.LogInformation("First successful aggregated request for target '{TargetId}', stopping.", target.Id);
                    cts.Cancel();
                    break; // Stop on first success
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error executing first-success aggregated request for target '{TargetId}'", target.Id);
                if (!target.Optional)
                    throw;
            }
        }

        cts.Dispose();
    }

    private async Task ExecuteFailFastAsync(
        IEnumerable<ConditionalAggregationTarget> targets,
        AggregatedResponse response)
    {
        var cts = new CancellationTokenSource();
        response.SetCancellationToken(cts);

        var tasks = new List<Task>();

        foreach (var target in targets)
        {
            // Create a linked cancellation token that respects both the global timeout and per-part timeout
            var timeoutCts = CreateTimeoutCancellationToken(target);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);

            var task = Task.Run(async () =>
            {
                try
                {
                    target.Validate();
                    var result = await ExecuteTargetRequestAsync(target, linkedCts.Token);

                    // Check if this is a failure that should trigger fail-fast
                    if (result.StatusCode < 200 || result.StatusCode >= 300)
                    {
                        _logger.LogWarning("Fail-fast: Target '{TargetId}' returned status {StatusCode}. Cancelling sibling requests.", target.Id, result.StatusCode);
                        cts.Cancel();
                    }

                    response.AddResponse(target.Id, result.StatusCode, result.Body, result.Headers, result.Duration);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error executing fail-fast aggregated request for target '{TargetId}'", target.Id);
                    cts.Cancel(); // Cancel sibling requests on any error

                    if (!target.Optional)
                        throw;

                    response.AddResponse(target.Id, 500, null, null, null, ex.Message);
                }
                finally
                {
                    linkedCts.Dispose();
                    timeoutCts.Dispose();
                }
            }, cts.Token);
            tasks.Add(task);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fail-fast: Request was cancelled due to sibling failure.");
            // Don't throw - we want to return partial results
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task ExecuteBestEffortAsync(
        IEnumerable<ConditionalAggregationTarget> targets,
        AggregatedResponse response)
    {
        var cts = new CancellationTokenSource();
        response.SetCancellationToken(cts);

        var tasks = new List<Task>();

        foreach (var target in targets)
        {
            // Create a linked cancellation token that respects both the global timeout and per-part timeout
            var timeoutCts = CreateTimeoutCancellationToken(target);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);

            var task = Task.Run(async () =>
            {
                try
                {
                    target.Validate();
                    var result = await ExecuteTargetRequestAsync(target, linkedCts.Token);
                    response.AddResponse(target.Id, result.StatusCode, result.Body, result.Headers, result.Duration);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error executing best-effort aggregated request for target '{TargetId}'", target.Id);

                    // Always include the error in the response for best-effort
                    response.AddResponse(target.Id, 500, null, null, null, ex.Message);
                }
                finally
                {
                    linkedCts.Dispose();
                    timeoutCts.Dispose();
                }
            }, cts.Token);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        cts.Dispose();
    }

    /// <summary>
    /// Creates a cancellation token source that will timeout after PerPartTimeoutSeconds.
    /// </summary>
    private CancellationTokenSource CreateTimeoutCancellationToken(ConditionalAggregationTarget target)
    {
        var timeout = TimeSpan.FromSeconds(target.PerPartTimeoutSeconds);
        return new CancellationTokenSource(timeout);
    }

    private async Task<RequestAggregationResult> ExecuteTargetRequestAsync(
        ConditionalAggregationTarget target,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        using var httpRequest = new HttpRequestMessage(
            new System.Net.Http.HttpMethod(target.Method.ToString()),
            target.UpstreamUrl);

        if (target.Headers is not null)
        {
            foreach (var header in target.Headers)
                httpRequest.Headers.Add(header.Key, header.Value);
        }

        if (!string.IsNullOrWhiteSpace(target.Body))
            httpRequest.Content = new StringContent(target.Body);

        var timeout = TimeSpan.FromSeconds(target.TimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cts.Token);
        var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        var headers = httpResponse.Headers.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault() ?? string.Empty);

        return new RequestAggregationResult
        {
            StatusCode = (int)httpResponse.StatusCode,
            Body = body,
            Headers = headers,
            Duration = DateTime.UtcNow - startTime
        };
    }

    private class RequestAggregationResult
    {
        public int StatusCode { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string> Headers { get; set; } = [];
        public TimeSpan Duration { get; set; }
    }
}
