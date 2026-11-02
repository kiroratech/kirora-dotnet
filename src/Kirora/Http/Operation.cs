namespace Kirora.Http;

internal sealed record Operation(string Id, string Method, string Retry, bool AcceptsKey, string? Entries);
