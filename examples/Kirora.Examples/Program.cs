using Kirora;
using Kirora.Exceptions;

var key = Environment.GetEnvironmentVariable("KIRORA_API_KEY");
if (string.IsNullOrWhiteSpace(key))
{
    Console.Error.WriteLine("Set KIRORA_API_KEY to run the read-only connection example.");
    return 1;
}
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
using var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false };
using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
var client = new KiroraClient(http, new KiroraOptions { ApiKey = key });
try
{
    var caller = await client.GetMeAsync(cancellation.Token);
    Console.WriteLine($"Connected to {caller.Workspace.Name} ({caller.Workspace.Id}).");
    return 0;
}
catch (KiroraApiException e)
{
    Console.Error.WriteLine($"Kirora error {e.Code}; HTTP {(int)e.StatusCode}; request {e.RequestId}.");
    return 1;
}
catch (OperationCanceledException) { Console.Error.WriteLine("Request canceled."); return 1; }
catch (KiroraTransportException) { Console.Error.WriteLine("Connection failed or timed out."); return 1; }
