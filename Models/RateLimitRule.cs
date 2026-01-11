public class RateLimitRule
{
    public string Key { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public double RefillPerSecond { get; set; }
}
