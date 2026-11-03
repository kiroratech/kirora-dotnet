using Kirora;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKirora(options =>
{
    options.ApiKey = builder.Configuration["KIRORA_API_KEY"]
        ?? throw new InvalidOperationException("Configure KIRORA_API_KEY.");
    var baseUrl = builder.Configuration["KIRORA_BASE_URL"];
    if (!string.IsNullOrEmpty(baseUrl)) options.BaseAddress = new Uri(baseUrl);
});
var app = builder.Build();
app.MapGet("/kirora/check", async (KiroraClient kirora, CancellationToken token) =>
{
    await kirora.GetMeAsync(token);
    return Results.Ok(new { connected = true });
});
app.Run();
