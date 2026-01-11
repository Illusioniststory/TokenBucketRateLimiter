using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly IRateLimitStore _bucketStore;
    private readonly IOptionsMonitor<RateLimitRule> _rules;
    private readonly ILogger<TokenBucketRateLimiter> _logger;

    public TokenBucketRateLimiter(
        IRateLimitStore bucketStore,
        IOptionsMonitor<RateLimitRule> rules,
        ILogger<TokenBucketRateLimiter> logger)
    {
        _bucketStore = bucketStore;
        _rules = rules;
        _logger = logger;
    }

    public bool IsAllowed(string key)
    {
        var rule = _rules.CurrentValue;
        if (rule.Key != key)
        {
            _logger.LogWarning("Rate limit rule key does not match request key. Request key: {RequestKey}, Rule key: {RuleKey}",
                key, rule.Key);
            return false;
        }
        var now = DateTime.UtcNow;

        var bucket = _bucketStore.GetOrCreate(key, () =>
        {
            _logger.LogInformation("Creating new token bucket for key: {Key} with capacity: {Capacity}, refill rate: {RefillPerSecond}/sec",
                key, rule.Capacity, rule.RefillPerSecond);
            return new TokenBucket
            {
                Tokens = rule.Capacity,
                LastRefill = now
            };
        });

        lock (bucket) 
        {
            var elapsedSeconds = (now - bucket.LastRefill).TotalSeconds;
            var tokensBeforeRefill = bucket.Tokens;
            
            _logger.LogInformation("Refilling token bucket for key: {Key}. Elapsed: {ElapsedSeconds:F2}s",
                key, elapsedSeconds);
            
            bucket.Tokens = Math.Min(
                rule.Capacity,
                bucket.Tokens + elapsedSeconds * rule.RefillPerSecond
            );

            var tokensRefilled = bucket.Tokens - tokensBeforeRefill;
            
            if (elapsedSeconds > 0 && tokensRefilled > 0)
            {
                _logger.LogInformation("Token bucket refilled for key: {Key}. Elapsed: {ElapsedSeconds:F2}s, Tokens before: {TokensBefore:F2}, Tokens after: {TokensAfter:F2}, Refilled: {TokensRefilled:F2}",
                    key, elapsedSeconds, tokensBeforeRefill, bucket.Tokens, tokensRefilled);
            }

            bucket.LastRefill = now;

            if (bucket.Tokens >= 1)
            {
                bucket.Tokens -= 1;
                _logger.LogInformation("Request allowed for key: {Key}. Tokens remaining: {RemainingTokens:F2}, Capacity: {Capacity}",
                    key, bucket.Tokens, rule.Capacity);
                return true;
            }

            _logger.LogWarning("Request rate limited for key: {Key}. Tokens available: {Tokens:F2}, Required: 1, Capacity: {Capacity}",
                key, bucket.Tokens, rule.Capacity);
            return false;
        }
    }
}
