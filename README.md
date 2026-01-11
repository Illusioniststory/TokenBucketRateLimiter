# Rate Limiter

A high-performance, in-memory rate limiting service built with ASP.NET Core that implements the **Token Bucket Algorithm**. This service provides per-key rate limiting to protect APIs from abuse and ensure fair resource usage.

## Features

- 🚀 **Token Bucket Algorithm**: Implements a flexible rate limiting strategy that allows burst traffic while maintaining sustained rate limits
- 🔑 **Per-Key Rate Limiting**: Each client/key has its own independent token bucket
- ⚡ **High Performance**: In-memory storage with thread-safe operations using `ConcurrentDictionary`
- 📊 **Comprehensive Logging**: Structured logging for all rate limiting operations
- ⚙️ **Configurable**: Easy configuration via `appsettings.json`
- 🔄 **Hot Reload**: Configuration changes are automatically picked up using `IOptionsMonitor`
- 🛡️ **Thread-Safe**: Per-key locking ensures thread safety without global bottlenecks

## Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2022, VS Code, or any .NET-compatible IDE

## Getting Started

### 1. Clone and Build

```bash
git clone <repository-url>
cd RateLimiter
dotnet restore
dotnet build
```

### 2. Run the Application

```bash
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000` (check `Properties/launchSettings.json` for exact ports).

### 3. Test the API

#### Using cURL

```bash
# Check rate limit for a key
curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user123"}'
```

#### Using HTTP File (RateLimiter.http)

```http
POST http://localhost:5000/check
Content-Type: application/json

{
  "key": "user123"
}
```

## Configuration

Rate limiting rules are configured in `appsettings.json`:

```json
{
  "RateLimiting": {
    "Key": "api",
    "Capacity": 10,
    "RefillPerSecond": 1
  }
}
```

### Configuration Parameters

- **Capacity**: Maximum number of tokens in the bucket (default: 10)
  - This is the burst capacity - how many requests can be made immediately
- **RefillPerSecond**: Number of tokens added per second (default: 1)
  - This is the sustained rate limit

### Example Scenarios

**Scenario 1: Burst Traffic**
- Capacity: 10, RefillPerSecond: 1
- Allows 10 immediate requests, then 1 request per second

**Scenario 2: Higher Sustained Rate**
- Capacity: 20, RefillPerSecond: 5
- Allows 20 immediate requests, then 5 requests per second

**Scenario 3: Strict Rate Limiting**
- Capacity: 5, RefillPerSecond: 0.5
- Allows 5 immediate requests, then 1 request every 2 seconds

## API Documentation

### Check Rate Limit

Check if a request is allowed for a given key.

**Endpoint:** `POST /check`

**Request Body:**
```json
{
  "key": "user123"
}
```

**Responses:**

- **200 OK**: Request is allowed
  ```json
  {}
  ```

- **429 Too Many Requests**: Rate limit exceeded
  ```
  Too Many Requests
  ```

**Example Request:**
```bash
curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user123"}'
```

## Architecture

### Components

#### 1. **TokenBucketRateLimiter** (`Services/TokenBucketRateLimiter.cs`)
   - Implements the token bucket algorithm
   - Manages token refilling and consumption
   - Thread-safe per-key operations

#### 2. **InMemoryRateLimitStore** (`Services/InMemoryRateLimitStore.cs`)
   - Stores token buckets in memory using `ConcurrentDictionary`
   - Provides thread-safe get-or-create operations

#### 3. **RateLimitController** (`Controllers/RateLimitController.cs`)
   - REST API endpoint for rate limit checks
   - Returns appropriate HTTP status codes

#### 4. **Models**
   - `TokenBucket`: Represents the state of a token bucket (tokens, last refill time)
   - `RateLimitRule`: Configuration model for rate limiting rules

### Token Bucket Algorithm

The token bucket algorithm works as follows:

1. **Initialization**: Each key gets a bucket with `Capacity` tokens
2. **Request Arrives**: 
   - Calculate elapsed time since last refill
   - Add tokens: `tokens = min(capacity, current + elapsed × refillRate)`
   - If tokens ≥ 1: consume 1 token and allow request
   - Otherwise: deny request (429)
3. **Refilling**: Tokens are continuously refilled based on elapsed time

### Thread Safety

- **Per-Key Locking**: Each bucket is locked independently
- **ConcurrentDictionary**: Thread-safe dictionary for bucket storage
- **No Global Locks**: Different keys can process requests in parallel

## Logging

The application provides comprehensive structured logging:

### Log Levels

- **Information**: Important events (requests, allowed/denied, bucket creation, refills)
- **Warning**: Rate limiting events (429 responses)
- **Debug**: Detailed operational info (bucket retrieval)

### Example Log Output

```
[Information] Rate limit check requested for key: user123
[Information] Creating new token bucket for key: user123 with capacity: 10, refill rate: 1/sec
[Information] Request allowed for key: user123. Tokens remaining: 9.00, Capacity: 10
[Information] Rate limit check passed for key: user123. Returning 200 OK
```

## Usage Examples

### Example 1: Basic Rate Limit Check

```bash
# First request - allowed
curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user123"}'
# Response: 200 OK

# 10 rapid requests - all allowed (burst capacity)
for i in {1..10}; do
  curl -X POST http://localhost:5000/check \
    -H "Content-Type: application/json" \
    -d '{"key": "user123"}'
done
# All responses: 200 OK

# 11th request - rate limited
curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user123"}'
# Response: 429 Too Many Requests
```

### Example 2: Wait and Retry

```bash
# Exhaust tokens
for i in {1..10}; do
  curl -X POST http://localhost:5000/check \
    -H "Content-Type: application/json" \
    -d '{"key": "user123"}'
done

# Wait 2 seconds
sleep 2

# Request should be allowed (2 tokens refilled)
curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user123"}'
# Response: 200 OK
```

### Example 3: Different Keys

```bash
# Each key has its own bucket
curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user1"}'
# Response: 200 OK

curl -X POST http://localhost:5000/check \
  -H "Content-Type: application/json" \
  -d '{"key": "user2"}'
# Response: 200 OK (independent bucket)
```

## Development

### Project Structure

```
RateLimiter/
├── Controllers/
│   └── RateLimitController.cs      # API endpoint
├── Models/
│   ├── RateLimitRule.cs            # Configuration model
│   └── TokenBucket.cs              # Token bucket state
├── Services/
│   ├── IRateLimiter.cs             # Rate limiter interface
│   ├── IRateLimitStore.cs          # Store interface
│   ├── TokenBucketRateLimiter.cs   # Token bucket implementation
│   └── InMemoryRateLimitStore.cs   # In-memory store
├── Program.cs                      # Application bootstrap
├── appsettings.json                # Configuration
└── RateLimiter.csproj              # Project file
```

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running in Development Mode

```bash
dotnet run --environment Development
```

OpenAPI documentation will be available at `/openapi/v1.json` in development mode.

## Limitations

- **In-Memory Storage**: Buckets are stored in memory and not persisted across restarts
- **Single Instance**: Not suitable for distributed systems (use Redis or similar for multi-instance deployments)
- **No Expiration**: Buckets are never cleaned up (consider adding TTL for production)

## Future Enhancements

- [ ] Redis-backed storage for distributed deployments
- [ ] Bucket expiration/TTL cleanup
- [ ] Multiple rate limit rules per key
- [ ] Rate limit headers in responses (X-RateLimit-*)
- [ ] Metrics and monitoring integration
- [ ] Middleware for automatic rate limiting

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]

Note: Help from Chatgpt
