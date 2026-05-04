// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Services;

/// <summary>
/// Service for aggregating responses from multiple backend requests
/// </summary>
public class RequestAggregationService
{
    private readonly HttpClient _httpClient;

    public RequestAggregationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AggregatedResponse> AggregateAsync(
        IEnumerable<AggregatedRequest> requests,
        AggregationStrategy strategy,
        RouteTarget target)
    {
        var response = new AggregatedResponse();

        return strategy switch
        {
            AggregationStrategy.Sequential => await ExecuteSequentialAsync(requests, response, target),
            AggregationStrategy.Parallel => await ExecuteParallelAsync(requests, response, target),
            AggregationStrategy.FirstSuccess => await ExecuteFirstSuccessAsync(requests, response, target),
            _ => await ExecuteSequentialAsync(requests, response, target)
        };
    }

    private async Task<AggregatedResponse> ExecuteSequentialAsync(
        IEnumerable<AggregatedRequest> requests,
        AggregatedResponse response,
        RouteTarget target)
    {
        var startTime = DateTime.UtcNow;

        foreach (var request in requests)
        {
            try
            {
                request.Validate();
                var result = await ExecuteRequestAsync(request, target);
                response.AddResponse(request.Alias, result.StatusCode, result.Body, result.Headers, result.Duration);
            }
            catch (Exception ex)
            {
                if (!request.Optional)
                    throw;

                response.AddResponse(request.Alias, 0, null, null, null);
            }
        }

        response.TotalDuration = DateTime.UtcNow - startTime;
        return response;
    }

    private async Task<AggregatedResponse> ExecuteParallelAsync(
        IEnumerable<AggregatedRequest> requests,
        AggregatedResponse response,
        RouteTarget target)
    {
        var startTime = DateTime.UtcNow;
        var tasks = new List<Task>();

        foreach (var request in requests)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    request.Validate();
                    var result = await ExecuteRequestAsync(request, target);
                    response.AddResponse(request.Alias, result.StatusCode, result.Body, result.Headers, result.Duration);
                }
                catch (Exception ex)
                {
                    if (!request.Optional)
                        throw;

                    response.AddResponse(request.Alias, 0, null, null, null);
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        response.TotalDuration = DateTime.UtcNow - startTime;
        return response;
    }

    private async Task<AggregatedResponse> ExecuteFirstSuccessAsync(
        IEnumerable<AggregatedRequest> requests,
        AggregatedResponse response,
        RouteTarget target)
    {
        var startTime = DateTime.UtcNow;

        foreach (var request in requests)
        {
            try
            {
                request.Validate();
                var result = await ExecuteRequestAsync(request, target);
                response.AddResponse(request.Alias, result.StatusCode, result.Body, result.Headers, result.Duration);

                if (result.StatusCode >= 200 && result.StatusCode < 300)
                    break;
            }
            catch (Exception ex)
            {
                if (!request.Optional)
                    throw;
            }
        }

        response.TotalDuration = DateTime.UtcNow - startTime;
        return response;
    }

    private async Task<RequestAggregationResult> ExecuteRequestAsync(AggregatedRequest request, RouteTarget target)
    {
        var startTime = DateTime.UtcNow;
        var url = target.GetForwardUrl(request.Path);

        using var httpRequest = new HttpRequestMessage(
            new System.Net.Http.HttpMethod(request.Method),
            url);

        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
                httpRequest.Headers.Add(header.Key, header.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Body))
            httpRequest.Content = new StringContent(request.Body);

        var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds ?? 30);
        using var cts = new CancellationTokenSource(timeout);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cts.Token);
        var body = await httpResponse.Content.ReadAsStringAsync();

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
