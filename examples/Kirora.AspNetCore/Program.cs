using Kirora;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKirora(options => options.ApiKey = builder.Configuration["Kirora:ApiKey"]
    ?? throw new InvalidOperationException("Configure Kirora:ApiKey through environment variables or a secret store."));
var app = builder.Build();
app.MapGet("/health/kirora", async (KiroraClient kirora, CancellationToken cancellationToken) =>
{
    await kirora.GetMeAsync(cancellationToken);
    return Results.Ok(new { connected = true });
});
app.Run();
