var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<RateLimitRule>(
    builder.Configuration.GetSection("RateLimiting"));

builder.Services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
builder.Services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
