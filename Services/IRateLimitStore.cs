public interface IRateLimitStore
{
    TokenBucket GetOrCreate(string key, Func<TokenBucket> factory);
}