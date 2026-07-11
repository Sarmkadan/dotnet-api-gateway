# RequestInterceptor
The `RequestInterceptor` class provides a mechanism to declaratively modify outgoing `HttpRequestMessage` instances before they are sent by the HTTP client. It allows registration of custom transformers, static header/query‑string manipulations, and a body template, all of which can be enabled or disabled at runtime.

## API
### RequestInterceptor()
Initializes a new instance of the `RequestInterceptor`. Internal collections for transformers, headers, and query‑parameter mappings are created empty. No exceptions are thrown under normal conditions.

### RegisterTransformer
Registers a `RequestTransformer` instance so it can be retrieved later by `GetTransformer`.  
- **Parameters**: `key` (string) – the identifier used to look up the transformer; `transformer` (RequestTransformer) – the transformer to store.  
- **Return value**: `void`.  
- **Exceptions**:  
  - `ArgumentNullException` if `key` or `transformer` is `null`.  
  - `ArgumentException` if a transformer is already registered under the same `key`.

### GetTransformer
Retrieves a previously registered transformer.  
- **Parameters**: `key` (string) – the identifier of the transformer to fetch.  
- **Return value**: `RequestTransformer?` – the transformer associated with `key`, or `null` if none is registered.  
- **Exceptions**:  
  - `ArgumentNullException` if `key` is `null`.

### InterceptAsync
Applies all configured transformations to an `HttpRequestMessage`.  
- **Parameters**: `request` (HttpRequestMessage) – the request to modify.  
- **Return value**: `Task<HttpRequestMessage>` – a task that yields the transformed request (the same instance may be returned if no changes are applied).  
- **Exceptions**:  
  - `ObjectDisposedException` if the interceptor has been disposed.  
  - `InvalidOperationException` if `Enabled` is `false` (the method still returns the original request but may throw depending on implementation).  
  - Any exception thrown by a registered `RequestTransformer` during its execution.

### UnregisterTransformer
Removes a transformer from the internal registry.  
- **Parameters**: `key` (string) – the identifier of the transformer to remove.  
- **Return value**: `void`.  
- **Exceptions**:  
  - `ArgumentNullException` if `key` is `null`.  
  - `KeyNotFoundException` if no transformer is registered under the supplied `key`.

### HeadersToAdd
Gets or sets a dictionary of header names and values that will be added to each request processed by `InterceptAsync`.  
- **Type**: `Dictionary<string, string>?`.  
- **Remarks**: A `null` value indicates no headers are to be added. When non‑null, the dictionary is used as‑is; modifications to the returned dictionary affect the interceptor's behavior.

### HeadersToRemove
Gets or sets a list of header names that will be removed from each request processed by `InterceptAsync`.  
- **Type**: `List<string>?`.  
- **Remarks**: A `null` value indicates no headers are to be removed. When non‑null, the list is used as‑is; modifications to the returned list affect the interceptor's behavior.

### BodyTemplate
Gets or sets a string template used to generate or replace the request body.  
- **Type**: `string?`.  
- **Remarks**: A `null` value leaves the body unchanged. When a non‑null template is provided, it is applied during interception (the exact application logic is defined by the interceptor's implementation).

### QueryParamMappings
Gets or sets a dictionary that maps original query‑parameter names to new names.  
- **Type**: `Dictionary<string, string>?`.  
- **Remarks**: A `null` value indicates no query‑parameter rewriting. When non‑null, each key/value pair specifies that a query parameter named `key` should be renamed to `value` during interception.

### Enabled
Gets or sets a flag that determines whether the interceptor actively processes requests.  
- **Type**: `bool`.  
- **Remarks**: When `false`, `InterceptAsync` returns the request unchanged (though it may still perform basic validation). The default value is `true`.

## Usage
```csharp
using System.Net.Http;
using System.Threading.Tasks;

// Example 1: basic setup and interception
var interceptor = new RequestInterceptor
{
    Enabled = true,
    HeadersToAdd = new Dictionary<string, string> { { "X-Custom-Header", "value" } },
    HeadersToRemove = new List<string> { "X-Obsolete" },
    BodyTemplate = "{ \"message\": \"hello\" }",
    QueryParamMappings = new Dictionary<string, string> { { "oldParam", "newParam" } }
};

// Register a custom transformer
interceptor.RegisterTransformer("auth", new RequestTransformer(req =>
{
    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token");
    return Task.CompletedTask;
}));

HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource?oldParam=123");
HttpRequestMessage transformed = await interceptor.InterceptAsync(request);
// transformed now contains the added header, removed header, rewritten query string,
// the body replaced by the template, and the Authorization header added by the transformer.
```

```csharp
using System.Net.Http;

// Example 2: retrieving and removing a transformer, then disabling the interceptor
var interceptor = new RequestInterceptor();

// Assume a transformer was previously registered under the key "logging"
RequestTransformer? logger = interceptor.GetTransformer("logging");
if (logger != null)
{
    // Use the logger transformer elsewhere …
}

// Remove the transformer when it is no longer needed
interceptor.UnregisterTransformer("logging");

// Disable further processing; InterceptAsync will now pass requests through unchanged
interceptor.Enabled = false;
```

## Notes
- The properties `HeadersToAdd`, `HeadersToRemove`, `QueryParamMappings`, and `BodyTransformer` accept `null` to signify “no operation”. Setting them to `null` effectively clears any previously configured values.  
- Modifying the objects returned by these properties after they have been assigned will directly affect the interceptor's behavior, as no defensive copies are made.  
- The class does **not** provide internal synchronization; concurrent calls to `RegisterTransformer`, `GetTransformer`, `UnregisterTransformer`, or property setters from multiple threads may result in undefined behavior. External locking is required for thread‑safe scenarios.  
- `InterceptAsync` may be invoked concurrently; if the interceptor's state (e.g., `Enabled`, header collections) is changed while a request is being processed, the outcome is nondeterministic.  
- If a registered transformer throws, the exception propagates out of `InterceptAsync`; the interceptor does not swallow or wrap such exceptions.  
- The constructor does not allocate any unmanaged resources, so there is no need to call `Dispose` unless a derived class introduces them.  
- When `Enabled` is `false`, the interceptor still performs null‑argument checks on the `request` parameter but skips all transformation steps.
