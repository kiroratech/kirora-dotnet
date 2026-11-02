using System.Net.Http.Headers;

namespace Kirora.Authentication;

internal static class BearerAuthentication
{
    internal static void Apply(HttpRequestMessage request, string apiKey) =>
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
}
