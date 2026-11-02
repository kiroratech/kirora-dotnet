using Kirora;
using Kirora.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddKirora(options => options.ApiKey = builder.Configuration["Kirora:ApiKey"]
    ?? throw new InvalidOperationException("Configure Kirora:ApiKey before starting the worker."));
builder.Services.AddHostedService<KiroraPoller>();
await builder.Build().RunAsync();

internal sealed class KiroraPoller(IHttpClientFactory httpFactory,
    Microsoft.Extensions.Options.IOptions<KiroraOptions> options, ILogger<KiroraPoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            // A background service is a singleton. Create a short-lived factory-managed HttpClient for each poll.
            using var http = httpFactory.CreateClient("Kirora");
            var kirora = new KiroraClient(http, options.Value);
            try
            {
                await kirora.Reports.GetOverviewAsync(cancellationToken: stoppingToken);
                logger.LogInformation("Kirora overview request succeeded.");
            }
            catch (KiroraApiException error)
            {
                logger.LogWarning("Kirora API status {Status}, code {Code}, request {RequestId}.", (int)error.StatusCode, error.Code, error.RequestId);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
