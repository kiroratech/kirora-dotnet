using Kirora;

var key = Environment.GetEnvironmentVariable("KIRORA_API_KEY");
if (string.IsNullOrWhiteSpace(key))
{
    Console.Error.WriteLine("Configure KIRORA_API_KEY in this process.");
    return;
}
var options = new KiroraOptions { ApiKey = key };
var baseUrl = Environment.GetEnvironmentVariable("KIRORA_BASE_URL");
if (!string.IsNullOrEmpty(baseUrl)) options.BaseAddress = new Uri(baseUrl);
using var handler = new SocketsHttpHandler
{
    AllowAutoRedirect = false, UseCookies = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
};
using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
var kirora = new KiroraClient(http, options);
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var caller = await kirora.GetMeAsync(deadline.Token);
Console.WriteLine($"Connected to workspace {caller.Workspace.Id}.");
