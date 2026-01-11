using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, TokenBucket> _store = new();
    private readonly ILogger<InMemoryRateLimitStore> _logger;

    public InMemoryRateLimitStore(ILogger<InMemoryRateLimitStore> logger)
    {
        _logger = logger;
    }

    public TokenBucket GetOrCreate(string key, Func<TokenBucket> factory)
    {
        var isNew = !_store.ContainsKey(key);
        var bucket = _store.GetOrAdd(key, _ => factory());
        
        if (isNew)
        {
            _logger.LogDebug("Retrieved new token bucket for key: {Key}. Total buckets in store: {BucketCount}", 
                key, _store.Count);
        }
        else
        {
            _logger.LogDebug("Retrieved existing token bucket for key: {Key}. Total buckets in store: {BucketCount}", 
                key, _store.Count);
        }
        
        return bucket;
    }
}
