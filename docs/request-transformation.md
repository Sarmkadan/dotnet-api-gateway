# Request Transformation

Request transformation rules let the gateway modify a proxied HTTP request before it is sent to the backend, and a proxied HTTP response before it is returned to the client. Rules are stored on each route as a list of `TransformationRule` objects and are applied by `RequestTransformationService`.

## Where it runs

In `Program.cs`, for every proxied call on a route that has at least one rule:

1. `ApplyRequestRules` runs on the outgoing `HttpRequestMessage`, before forwarding.
2. The upstream response goes through the response transformer (security and custom headers).
3. `ApplyResponseRules` runs on the upstream `HttpResponseMessage`, before it reaches the client.

Routes without rules skip both steps.

## The rule model

Each rule is a `TransformationRule` (`Models/TransformationRule.cs`):

| Property | Meaning |
|---|---|
| `Id` | Unique identifier, defaults to a new GUID. Used in log messages. |
| `Description` | Free text for humans. |
| `Phase` | `Request` (default) or `Response`. Each phase only runs its own rules. |
| `Operation` | One of the operations listed below. |
| `Key` | Header name, query parameter name, or path prefix, depending on the operation. Required. |
| `Value` | Header value, query parameter value, or replacement prefix. Required for some operations (see below). |
| `Order` | Evaluation order. Lower numbers run first. Default `0`. |
| `IsEnabled` | When `false`, the rule is skipped silently. Default `true`. |

`Validate()` throws `ArgumentException` when `Key` is empty, or when `Value` is empty for `AddHeader`, `SetHeader`, `AddQueryParam`, `SetQueryParam` or `RewritePathPrefix`.

## Operations

| Operation | Request phase | Response phase |
|---|---|---|
| `AddHeader` | Adds the header only if it is not already present. | Same, on the response. |
| `SetHeader` | Removes any existing value, then sets the header. | Same, on the response. |
| `RemoveHeader` | Removes the header. | Same, on the response. |
| `AddQueryParam` | Adds the query parameter only if the key is not already in the URL. | Not supported, logged and skipped. |
| `SetQueryParam` | Sets the query parameter, replacing any existing value. | Not supported, logged and skipped. |
| `RemoveQueryParam` | Removes the query parameter from the URL. | Not supported, logged and skipped. |
| `RewritePathPrefix` | If the path starts with `Key` (case-insensitive), replaces that prefix with `Value`. Otherwise the URI is unchanged. | Not supported, logged and skipped. |
| `AllowlistHeaders` | Defined in the enum but **not handled** by the service. See "Known gaps". | Not handled. |

Query and path operations only exist for the request phase because the response has no URL.

## Ordering and error handling

- Rules are filtered by `IsEnabled` and `Phase`, then sorted by `Order` (stable sort, so rules with equal `Order` keep their list order).
- Each rule runs in its own `try`/`catch`. A failing rule logs a warning with its `Id` and `Operation` and the next rule still runs. The request is never aborted by a transformation error.
- The request object is mutated in place and returned. For URL operations, `RequestUri` is replaced with a rebuilt `Uri`.

## Examples

Add a fixed header to every request on a route:

```json
{
  "Description": "Identify the gateway to the backend",
  "Phase": "Request",
  "Operation": "SetHeader",
  "Key": "X-Gateway",
  "Value": "dotnet-api-gateway",
  "Order": 10
}
```

Move `/api/v1/*` on the public side to `/internal/v1/*` on the backend:

```json
{
  "Description": "Strip the public API prefix",
  "Phase": "Request",
  "Operation": "RewritePathPrefix",
  "Key": "/api/v1",
  "Value": "/internal/v1",
  "Order": 1
}
```

Remove a server header from responses:

```json
{
  "Description": "Hide backend server banner",
  "Phase": "Response",
  "Operation": "RemoveHeader",
  "Key": "Server"
}
```

Helper extension methods exist for common cases, for example `AddRequestHeader` and `SetResponseHeader` in `Models/TransformationRuleExtensions.cs`. They set `Phase`, `Operation`, `Key`, `Value`, `Order` and `IsEnabled` in one call.

## Known gaps

- `AllowlistHeaders` is in the `TransformationOperation` enum but has no case in either switch. Rules with this operation fall into the `default` branch and log a warning (request phase) or are skipped with a warning (response phase). Do not rely on it to strip headers until it is implemented.
- Query and path operations are request-only. A response rule with one of them is skipped with a warning.
- `ApplyRequestRules` and `ApplyResponseRules` do not call `Validate()`. Invalid rules (for example, an empty `Value` on `SetHeader`) are not rejected at apply time. They are passed to the header API, which may silently do nothing. Validate rules when they are created or loaded.
- `RemoveQueryParam` and the add/set query helpers rebuild the URI through `ParseQueryString`, so the query string is re-encoded. Parameter order and encoding may change.

## Related

- `Controllers/RequestTransformationController.cs` and `docs/RequestTransformationController.md`: a test-only controller that runs header, body and query transformations against sample input. It is separate from the `TransformationRule` pipeline above.
- `Services/RequestInterceptor.cs`: a separate, per-route `RequestTransformer` hook (headers, body, query string) registered at runtime.
- `Services/IResponseTransformer.cs` and `Services/ResponseTransformationService.cs`: the response transformer that runs before response rules.
