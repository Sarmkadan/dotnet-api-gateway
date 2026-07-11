# CachePolicy

CachePolicy is a configuration class used to define caching behavior for API gateway responses. It specifies rules for which requests and responses should be cached, how long they should remain cached, and under what conditions the cache should vary based on request attributes such as query strings or headers.

## API

### Id

**Purpose**: Uniquely identifies the cache policy instance.  
**Parameters**: None  
**Return Value**: A `string` representing the policy identifier.  
**Exceptions**: None  

### Enabled

**Purpose**: Determines whether the cache policy is active.  
**Parameters**: None  
**Return Value**: A `bool` indicating if caching is enabled.  
**Exceptions**: None  

### DurationSeconds

**Purpose**: Specifies the time-to-live (TTL) for cached entries in seconds.  
**Parameters**: None  
**Return Value**: An `int` representing the cache duration.  
**Exceptions**: None  

### Strategy

**Purpose**: Defines the caching strategy (e.g., LRU, FIFO).  
**Parameters**: None  
**Return Value**: A `CacheStrategy` enum value.  
**Exceptions**: None  

### CacheableStatusCodes

**Purpose**: Lists HTTP status codes for which responses should be cached.  
**Parameters**: None  
**Return Value**: A `string[]` of status codes (e.g., `"200"`, `"404"`).  
**Exceptions**: None  

### CacheableHttpMethods

**Purpose**: Lists HTTP methods for which responses should be cached.  
**Parameters**: None  
**Return Value**: A `string[]` of HTTP methods (e.g., `"GET"`, `"POST"`).  
**Exceptions**: None  

### VaryByQueryString

**Purpose**: Indicates whether the cache key should include the request query string.  
**Parameters**: None  
**Return Value**: A `bool` indicating if query string variation is enabled.  
**Exceptions**: None  

### VaryByHeaders

**Purpose**: Indicates whether the cache key should include specified request headers.  
**Parameters**: None  
**Return Value**: A `bool` indicating if header variation is enabled.  
**Exceptions**: None  

### VaryHeaders

**Purpose**: Lists headers to include in the cache key when `VaryByHeaders` is true.  
**Parameters**: None  
**Return Value**: A `string[]` of header names.  
**Exceptions**: None  

### MaxEntriesInCache

**Purpose**: Limits the maximum number of entries stored in the cache.  
**Parameters**: None  
**Return Value**: An `int` representing the maximum cache size.  
**Exceptions**: None  

### Validate

**Purpose**: Validates the cache policy configuration for correctness.  
**Parameters**: None  
**Return Value**: `void`  
**Exceptions**: Throws `InvalidOperationException` if `DurationSeconds` or `MaxEntriesInCache` is negative, or if `CacheableStatusCodes`/`CacheableHttpMethods` contain invalid values.  

### IsCacheable

**Purpose**: Determines if a response with the given status code and HTTP method should be cached.  
**Parameters**:  
- `statusCode` (`string`): The HTTP status code of the response.  
- `httpMethod` (`string`): The HTTP method of the request.  
**Return Value**: `bool` indicating cache eligibility.  
**Exceptions**: None  

### GenerateCacheKey

**Purpose**: Generates a unique cache key based on the request context and policy settings.  
**Parameters**:  
- `context` (`HttpContext`): The HTTP request context.  
**Return Value**: A `string` representing the generated cache key.  
**Exceptions**: None  

## Usage

### Example 1: Configuring a Cache Policy

```csharp
var policy = new CachePolicy
{
    Id = "default-cache",
    Enabled = true,
    DurationSeconds = 300,
    Strategy = CacheStrategy.Lru,
    CacheableStatusCodes = new[] { "200", "404" },
    CacheableHttpMethods = new[] { "GET", "HEAD" },
    VaryByQueryString = true,
    VaryByHeaders = true,
    VaryHeaders = new[] { "Accept", "Accept-Encoding" },
    MaxEntriesInCache = 1000
};

policy.Validate(); // Ensures configuration is valid
```

### Example 2: Checking Cache Eligibility and Generating a Key

```csharp
var context = httpContextAccessor.HttpContext;
var responseStatusCode = context.Response.StatusCode.ToString();
var requestMethod = context.Request.Method;

if (policy.IsCacheable(responseStatusCode, requestMethod))
{
    var cacheKey = policy.GenerateCacheKey(context);
    // Use cacheKey to store/retrieve cached response
}
```

## Notes

- `Validate` must be called explicitly to enforce constraints on `DurationSeconds` and `MaxEntriesInCache`. Negative values or invalid status codes/methods will cause exceptions.  
- `IsCacheable` returns `false` if `CacheableStatusCodes` or `CacheableHttpMethods` is `null` or empty, or if the provided values are not present in the respective arrays.  
- `GenerateCacheKey` incorporates query string and header variations only if `VaryByQueryString` or `VaryByHeaders` are `true`.  
- Instances of `CachePolicy` are not thread-safe. External synchronization is required if shared across threads.  
- `MaxEntriesInCache` of `0` or negative values are invalid and will trigger exceptions in `Validate`.
