# RateLimitMetrics

Provides in‑memory tracking and reporting of request rates per client, enabling the gateway to enforce limits and expose statistics for monitoring and debugging.

## API

### `RecordRequest()`
Records a single request for the client associated with the instance.  
- **Parameters:** none.  
- **Return value:** none.  
- **Exceptions:** May throw an `InvalidOperationException` if the internal state is corrupted (e.g., after a call to `Clear` while concurrent operations are in progress).

### `GetClientStats()`
Retrieves the detailed statistics for the client identified by the `ClientId` property.  
- **Parameters:** none.  
- **Return value:** A `ClientRateLimitStats` instance containing per‑client metrics, or `null` if no data exists for that client.  
- **Exceptions:** None under normal operation; may throw if the object is in an invalid state.

### `GetAllStats()`
Returns the statistics for every client currently being tracked.  
- **Parameters:** none.  
- **Return value:** A `List<ClientRateLimitStats>` containing an entry for each tracked client; the list may be empty if no clients have been recorded.  
- **Exceptions:** None.

### `GetTopClients()`
Returns a list of clients sorted by request volume in descending order.  
- **Parameters:** none.  
- **Return value:** A `List<ClientRateLimitStats>` with the highest‑request clients; empty list if no data is present.  
- **Exceptions:** None.

### `GetViolatingClients()`
Returns a list of clients that have exceeded their configured rate limits.  
- **Parameters:** none.  
- **Return value:** A `List<ClientRateLimitStats>` containing only violating clients; empty list if none are violating.  
- **Exceptions:** None.

### `GetOverallMetrics()`
Provides aggregated metrics across all tracked clients.  
- **Parameters:** none.  
- **Return value:** A `RateLimitOverallMetrics` object summarizing total requests, limited requests, averages, etc.  
- **Exceptions:** None.

### `Clear()`
Resets all internal counters and removes tracking information for every client.  
- **Parameters:** none.  
- **Return value:** none.  
- **Exceptions:** None.

### `RemoveOldEntries()`
Removes entries whose timestamps are older than the internal retention window and returns the number of entries removed.  
- **Parameters:** none.  
- **Return value:** An `int` indicating how many client records were deleted.  
- **Exceptions:** None.

### `ClientId`
Gets the identifier of the client whose metrics are represented by this instance.  
- **Return value:** A `string`; `null` or empty if the instance is not bound to a specific client.  
- **Exceptions:** None.

### `TotalRequests`
Gets the total number of requests recorded for the client identified by `ClientId`.  
- **Return value:** A `long` representing the request count.  
- **Exceptions:** None.

### `LimitedRequests`
Gets the number of requests that were limited (i.e., rejected due to rate‑limit enforcement) for the client.  
- **Return value:** A `long`.  
- **Exceptions:** None.

### `FirstRequestTime`
Gets the timestamp of the first request observed for the client.  
- **Return value:** A `DateTime`; `DateTime.MinValue` if no requests have been recorded.  
- **Exceptions:** None.

### `LastRequestTime`
Gets the timestamp of the most recent request observed for the client.  
- **Return value:** A `DateTime`; `DateTime.MinValue` if no requests have been recorded.  
- **Exceptions:** None.

### `TotalClients`
Gets the number of distinct clients currently being tracked.  
- **Return value:** An `int`.  
- **Exceptions:** None.

### `TotalLimitedRequests`
Gets the cumulative count of limited requests across all tracked clients.  
- **Return value:** A `long`.  
- **Exceptions:** None.

### `AverageRequestsPerClient`
Gets the average number of requests per client (total requests divided by `TotalClients`).  
- **Return value:** A `double`; returns `0.0` when `TotalClients` is zero.  
- **Exceptions:** None.

## Usage

### Example 1: Recording requests and retrieving per‑client stats
```csharp
var metrics = new RateLimitMetrics { ClientId = "client-42" };

// Simulate incoming traffic
for (int i = 0; i < 120; i++)
{
    metrics.RecordRequest();
}

// Obtain the stats for this specific client
var stats = metrics.GetClientStats();
if (stats != null)
{
    Console.WriteLine($"Total requests: {stats.TotalRequests}");
    Console.WriteLimited requests: {stats.LimitedRequests}");
}
```

### Example 2: Getting global insights and cleaning old data
```csharp
var metrics = new RateLimitMetrics();

// Assume RecordRequest has been called elsewhere for various clients

// Overall gateway metrics
var overall = metrics.GetOverallMetrics();
Console.WriteLine($"Total clients: {overall.TotalClients}");
Console.WriteLine($"Average requests/client: {overall.AverageRequestsPerClient:F2}");

// Identify top abusers
var top = metrics.GetTopClients();
foreach (var client in top.Take(5))
{
    Console.WriteLine($"{client.ClientId}: {client.TotalRequests} requests");
}

// Remove stale entries (e.g., older than 1 hour) and report how many were purged
int removed = metrics.RemoveOldEntries();
Console.WriteLine($"Cleared {removed} outdated client records.");
```

## Notes
- The type is **not thread‑safe**; concurrent calls to `RecordRequest`, `Clear`, or any of the query methods from multiple threads should be synchronized externally (e.g., using `lock` or a concurrent wrapper).  
- `GetClientStats` returns `null` when the `ClientId` does not correspond to any recorded client; callers must check for null before accessing members of the returned object.  
- `GetTopClients` and `GetViolatingClients` return lists sorted in descending order by request count and by number of violations, respectively; the exact sorting algorithm is internal and subject to change.  
- `RemoveOldEntries` uses an internal retention policy (not exposed via the API) to determine which entries are considered old; the method always returns a non‑negative integer.  
- Property values such as `FirstRequestTime` and `LastRequestTime` are set to `DateTime.MinValue` when no requests have been recorded for the associated client.  
- After invoking `Clear`, all counters reset to zero and all client‑specific data is removed; subsequent calls to `GetAllStats`, `GetTopClients`, or `GetViolatingClients` will return empty collections.  
- The `AverageRequestsPerClient` property computes the ratio using floating‑point division; if `TotalClients` is zero the result is defined as `0.0` to avoid division by zero.
