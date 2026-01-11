public interface IRateLimiter
{
    bool IsAllowed(string key);
}
