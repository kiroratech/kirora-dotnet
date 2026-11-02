using Kirora;
using Kirora.Models;

namespace Kirora.Examples;

/// <summary>Read-only examples; these helpers are never invoked by the default console entry point.</summary>
public static class ExtendedResourceExamples
{
    public static async Task InspectEngagementAsync(KiroraClient kirora, string eventName, CancellationToken cancellationToken)
    {
        var catalog = await kirora.Events.ListCatalogAsync(cancellationToken: cancellationToken);
        var series = await kirora.Events.GetSeriesAsync(new GetEventSeriesQuery { Name = eventName, Interval = "day" }, cancellationToken: cancellationToken);
        var rfm = await kirora.Reports.GetRfmSummaryAsync(cancellationToken: cancellationToken);
        Console.WriteLine($"Catalog, event series and RFM summary read: {catalog is not null && series is not null && rfm is not null}.");
        await foreach (var template in kirora.Templates.ListAllAsync(cancellationToken: cancellationToken))
            Console.WriteLine(template.Id);
    }

    public static async Task DownloadExportAsync(KiroraClient kirora, string readyExportId, Stream destination, CancellationToken cancellationToken) =>
        await kirora.Messages.DownloadExportToAsync(readyExportId, destination, cancellationToken: cancellationToken);
}
