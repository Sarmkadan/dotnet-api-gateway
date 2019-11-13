#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetApiGateway.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotNetApiGateway.Services;
using DotNetApiGateway.Repositories;
using DotNetApiGateway.Configuration;

/// <summary>
/// Admin dashboard endpoint that provides a real-time HTML overview of gateway health,
/// metrics, routes, and circuit breaker states. Intended for internal operator use.
/// </summary>
[ApiController]
[Route("admin")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly MetricsService _metricsService;
    private readonly RoutingService _routingService;
    private readonly CircuitBreakerService _circuitBreakerService;
    private readonly GatewayRouteRepository _routeRepository;
    private readonly DotnetApiGatewayOptions _configuration;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        MetricsService metricsService,
        RoutingService routingService,
        CircuitBreakerService circuitBreakerService,
        GatewayRouteRepository routeRepository,
        DotnetApiGatewayOptions configuration,
        ILogger<AdminDashboardController> logger)
    {
        _metricsService = metricsService;
        _routingService = routingService;
        _circuitBreakerService = circuitBreakerService;
        _routeRepository = routeRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Returns the admin dashboard as an HTML page with real-time gateway statistics,
    /// route listing, circuit breaker states, and performance metrics.
    /// </summary>
    [HttpGet("dashboard")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var metrics = _metricsService.GetMetrics();
        var routes = (await _routeRepository.GetAllAsync()).ToList();
        var circuitBreakers = (await _circuitBreakerService.GetAllStatusesAsync()).ToList();

        var html = BuildDashboardHtml(metrics, routes, circuitBreakers);
        return Content(html, "text/html");
    }

    /// <summary>
    /// Returns a JSON summary of the current gateway state, suitable for monitoring agents.
    /// </summary>
    [HttpGet("dashboard/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var metrics = _metricsService.GetMetrics();
        var routes = await _routeRepository.GetAllAsync();
        var circuitBreakers = (await _circuitBreakerService.GetAllStatusesAsync()).ToList();

        var openBreakers = circuitBreakers.Count(cb => cb.State == Constants.CircuitBreakerState.Open);
        var halfOpenBreakers = circuitBreakers.Count(cb => cb.State == Constants.CircuitBreakerState.HalfOpen);

        return Ok(new
        {
            gateway = new
            {
                name = _configuration.ApplicationName,
                version = _configuration.Version,
                uptime = metrics.Uptime.ToString(@"d\.hh\:mm\:ss"),
                startedAt = metrics.StartTime
            },
            requests = new
            {
                total = metrics.TotalRequests,
                successful = metrics.SuccessfulRequests,
                failed = metrics.FailedRequests,
                successRatePercent = Math.Round(metrics.SuccessRate, 2),
                averageResponseTimeMs = Math.Round(metrics.AverageResponseTimeMs, 2),
                requestsPerSecond = Math.Round(metrics.GetRequestsPerSecond(), 2)
            },
            routes = new
            {
                total = routes.Count(),
                active = routes.Count(r => r.IsActive),
                inactive = routes.Count(r => !r.IsActive)
            },
            circuitBreakers = new
            {
                total = circuitBreakers.Count,
                open = openBreakers,
                halfOpen = halfOpenBreakers,
                closed = circuitBreakers.Count - openBreakers - halfOpenBreakers
            },
            statusCodeDistribution = metrics.StatusCodeDistribution,
            timestamp = DateTime.UtcNow
        });
    }

    // -------------------------------------------------------------------------
    // HTML rendering
    // -------------------------------------------------------------------------

    private string BuildDashboardHtml(
        GatewayMetrics metrics,
        List<GatewayRoute> routes,
        IReadOnlyList<Models.CircuitBreakerStatus> circuitBreakers)
    {
        var successRateColor = metrics.SuccessRate >= 99 ? "#22c55e"
            : metrics.SuccessRate >= 90 ? "#f59e0b"
            : "#ef4444";

        var uptimeStr = metrics.Uptime.ToString(@"d\.hh\:mm\:ss");
        var routeRows = BuildRouteRows(routes, metrics);
        var cbRows = BuildCircuitBreakerRows(circuitBreakers);
        var statusDistRows = BuildStatusDistributionRows(metrics.StatusCodeDistribution);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{{_configuration.ApplicationName}} — Admin Dashboard</title>
  <style>
    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
           background: #0f172a; color: #e2e8f0; min-height: 100vh; padding: 24px; }
    h1   { font-size: 1.6rem; font-weight: 700; color: #f8fafc; }
    h2   { font-size: 1.1rem; font-weight: 600; color: #94a3b8; margin-bottom: 12px; text-transform: uppercase; letter-spacing: 0.05em; }
    .header { display: flex; justify-content: space-between; align-items: center;
              margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid #1e293b; }
    .badge { font-size: 0.75rem; padding: 3px 10px; border-radius: 9999px; background: #1e293b; color: #94a3b8; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(210px, 1fr)); gap: 16px; margin-bottom: 24px; }
    .card { background: #1e293b; border-radius: 12px; padding: 20px; }
    .card .label { font-size: 0.78rem; color: #94a3b8; margin-bottom: 6px; }
    .card .value { font-size: 1.9rem; font-weight: 700; }
    .card .sub   { font-size: 0.75rem; color: #64748b; margin-top: 4px; }
    .section { background: #1e293b; border-radius: 12px; padding: 20px; margin-bottom: 24px; overflow-x: auto; }
    table  { width: 100%; border-collapse: collapse; font-size: 0.87rem; }
    th     { text-align: left; padding: 8px 12px; color: #64748b; border-bottom: 1px solid #334155; font-weight: 500; }
    td     { padding: 10px 12px; border-bottom: 1px solid #0f172a; }
    tr:last-child td { border-bottom: none; }
    .pill  { display: inline-block; padding: 2px 8px; border-radius: 9999px; font-size: 0.75rem; font-weight: 600; }
    .green { background: #14532d; color: #86efac; }
    .red   { background: #450a0a; color: #fca5a5; }
    .yellow{ background: #431407; color: #fdba74; }
    .gray  { background: #1e293b; color: #94a3b8; border: 1px solid #334155; }
    .refresh { font-size: 0.78rem; color: #64748b; }
  </style>
  <meta http-equiv="refresh" content="30" />
</head>
<body>
  <div class="header">
    <div>
      <h1>{{_configuration.ApplicationName}}</h1>
      <div style="margin-top:6px; font-size:0.82rem; color:#64748b">
        v{{_configuration.Version}} &nbsp;·&nbsp; Uptime: {{uptimeStr}} &nbsp;·&nbsp; Started: {{metrics.StartTime:u}}
      </div>
    </div>
    <span class="badge refresh">Auto-refreshes every 30 s &nbsp;·&nbsp; {{DateTime.UtcNow:u}}</span>
  </div>

  <div class="grid">
    <div class="card">
      <div class="label">Total Requests</div>
      <div class="value">{{metrics.TotalRequests:N0}}</div>
      <div class="sub">{{metrics.GetRequestsPerSecond():F2}} req/s</div>
    </div>
    <div class="card">
      <div class="label">Success Rate</div>
      <div class="value" style="color:{{successRateColor}}">{{metrics.SuccessRate:F1}}%</div>
      <div class="sub">{{metrics.SuccessfulRequests:N0}} succeeded / {{metrics.FailedRequests:N0}} failed</div>
    </div>
    <div class="card">
      <div class="label">Avg Response Time</div>
      <div class="value">{{metrics.AverageResponseTimeMs:F1}}<span style="font-size:1rem;font-weight:400"> ms</span></div>
    </div>
    <div class="card">
      <div class="label">Active Routes</div>
      <div class="value">{{routes.Count(r => r.IsActive)}}</div>
      <div class="sub">{{routes.Count}} total</div>
    </div>
    <div class="card">
      <div class="label">Circuit Breakers</div>
      <div class="value">{{circuitBreakers.Count(cb => cb.State == Constants.CircuitBreakerState.Open)}}<span style="font-size:1rem;font-weight:400"> open</span></div>
      <div class="sub">{{circuitBreakers.Count}} tracked</div>
    </div>
  </div>

  <div class="section">
    <h2>Routes</h2>
    <table>
      <thead>
        <tr><th>ID</th><th>Name</th><th>Path</th><th>Methods</th><th>Targets</th><th>Status</th><th>Requests</th><th>Avg ms</th></tr>
      </thead>
      <tbody>{{routeRows}}</tbody>
    </table>
  </div>

  <div class="section">
    <h2>Circuit Breakers</h2>
    <table>
      <thead>
        <tr><th>Service</th><th>State</th><th>Failures</th><th>Successes</th><th>Last Error</th><th>Last Failure</th></tr>
      </thead>
      <tbody>{{cbRows}}</tbody>
    </table>
  </div>

  <div class="section">
    <h2>Status Code Distribution</h2>
    <table>
      <thead><tr><th>Status Code</th><th>Count</th><th>Share</th></tr></thead>
      <tbody>{{statusDistRows}}</tbody>
    </table>
  </div>
</body>
</html>
""";
    }

    private string BuildRouteRows(List<GatewayRoute> routes, GatewayMetrics metrics)
    {
        if (routes.Count == 0)
            return "<tr><td colspan=\"8\" style=\"color:#64748b;text-align:center\">No routes configured</td></tr>";

        var sb = new System.Text.StringBuilder();
        foreach (var route in routes)
        {
            var routeMetrics = metrics.RouteMetrics.FirstOrDefault(rm => rm.RouteId == route.Id);
            var statusPill = route.IsActive
                ? "<span class=\"pill green\">Active</span>"
                : "<span class=\"pill gray\">Inactive</span>";
            var methods = string.Join(", ", route.AllowedMethods);
            var healthyTargets = route.Targets.Count(t => t.IsHealthy);
            var requestCount = routeMetrics?.RequestCount ?? 0;
            var avgMs = routeMetrics != null ? $"{routeMetrics.GetAverageResponseTime():F1}" : "—";

            sb.Append($"<tr>");
            sb.Append($"<td style=\"color:#64748b;font-size:0.75rem\">{System.Net.WebUtility.HtmlEncode(route.Id[..Math.Min(8, route.Id.Length)])}&hellip;</td>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(route.Name)}</td>");
            sb.Append($"<td><code style=\"background:#0f172a;padding:2px 6px;border-radius:4px\">{System.Net.WebUtility.HtmlEncode(route.PathPattern)}</code></td>");
            sb.Append($"<td style=\"color:#818cf8\">{System.Net.WebUtility.HtmlEncode(methods)}</td>");
            sb.Append($"<td>{healthyTargets}/{route.Targets.Length} healthy</td>");
            sb.Append($"<td>{statusPill}</td>");
            sb.Append($"<td>{requestCount:N0}</td>");
            sb.Append($"<td>{avgMs}</td>");
            sb.Append("</tr>");
        }
        return sb.ToString();
    }

    private static string BuildCircuitBreakerRows(IReadOnlyList<Models.CircuitBreakerStatus> statuses)
    {
        if (statuses.Count == 0)
            return "<tr><td colspan=\"6\" style=\"color:#64748b;text-align:center\">No circuit breakers tracked</td></tr>";

        var sb = new System.Text.StringBuilder();
        foreach (var cb in statuses)
        {
            var statePill = cb.State switch
            {
                Constants.CircuitBreakerState.Open => "<span class=\"pill red\">Open</span>",
                Constants.CircuitBreakerState.HalfOpen => "<span class=\"pill yellow\">Half-Open</span>",
                _ => "<span class=\"pill green\">Closed</span>"
            };

            sb.Append("<tr>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(cb.ServiceName)}</td>");
            sb.Append($"<td>{statePill}</td>");
            sb.Append($"<td>{cb.FailureCount}</td>");
            sb.Append($"<td>{cb.SuccessCount}</td>");
            sb.Append($"<td style=\"color:#f87171;font-size:0.8rem\">{System.Net.WebUtility.HtmlEncode(cb.LastError ?? "—")}</td>");
            sb.Append($"<td style=\"color:#64748b;font-size:0.8rem\">{(cb.LastFailureAt is null ? "—" : cb.LastFailureAt.Value.ToString("u"))}</td>");
            sb.Append("</tr>");
        }
        return sb.ToString();
    }

    private static string BuildStatusDistributionRows(Dictionary<int, long> distribution)
    {
        if (distribution.Count == 0)
            return "<tr><td colspan=\"3\" style=\"color:#64748b;text-align:center\">No requests recorded yet</td></tr>";

        var total = distribution.Values.Sum();
        var sb = new System.Text.StringBuilder();
        foreach (var (code, count) in distribution.OrderBy(kv => kv.Key))
        {
            var pct = total > 0 ? count * 100.0 / total : 0;
            var pillClass = code is >= 200 and < 300 ? "green"
                : code is >= 400 and < 500 ? "yellow"
                : code >= 500 ? "red"
                : "gray";
            sb.Append("<tr>");
            sb.Append($"<td><span class=\"pill {pillClass}\">{code}</span></td>");
            sb.Append($"<td>{count:N0}</td>");
            sb.Append($"<td>{pct:F1}%</td>");
            sb.Append("</tr>");
        }
        return sb.ToString();
    }
}
