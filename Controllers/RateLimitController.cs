using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("check")]
public class RateLimitController : ControllerBase
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitController> _logger;

    public RateLimitController(IRateLimiter rateLimiter, ILogger<RateLimitController> logger)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Check([FromBody] RateLimitRequest request)
    {
        _logger.LogInformation("Rate limit check requested for key: {Key}", request.Key);

        if (_rateLimiter.IsAllowed(request.Key))
        {
            _logger.LogInformation("Rate limit check passed for key: {Key}. Returning 200 OK", request.Key);
            return Ok();
        }

        _logger.LogWarning("Rate limit check failed for key: {Key}. Returning 429 Too Many Requests", request.Key);
        return StatusCode(429, "Too Many Requests");
    }
}

public record RateLimitRequest(string Key);
