using System.Net;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Kirora.Authentication;
using Kirora.Exceptions;
using Kirora.Models;
using Kirora.Serialization;

namespace Kirora.Http;

internal sealed class KiroraHttpTransport(HttpClient httpClient, KiroraOptions options)
{
    private static JsonSerializerOptions Json => KiroraCoreJsonContext.Default.Options;

    internal Task<T> GetDataAsync<T>(string path, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken) =>
        ExecuteAsync(new Operation("getMe", "GET", "read", false, null), path, null, null, null, cancellationToken,
            (bytes, metadata) =>
            {
                using var document = JsonDocument.Parse(bytes);
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
                    throw new KiroraInvalidResponseException("The response is missing its data payload.", metadata);
                return data.Deserialize(typeInfo) ?? throw new KiroraInvalidResponseException("The data payload was null.", metadata);
            });

    internal Task<T> SendAsync<T>(Operation operation, string path, object? body, object? query,
        KiroraRequestOptions? requestOptions, CancellationToken cancellationToken) =>
        ExecuteAsync(operation, path, Snapshot(body), Snapshot(query) as JsonObject, requestOptions, cancellationToken,
            (bytes, metadata) => JsonSerializer.Deserialize(bytes, TypeInfo<T>()) ??
                throw new KiroraInvalidResponseException("The response was null.", metadata));

    internal async Task SendNoContentAsync(Operation operation, string path, object? body, object? query,
        KiroraRequestOptions? requestOptions, CancellationToken cancellationToken) =>
        await ExecuteAsync(operation, path, Snapshot(body), Snapshot(query) as JsonObject, requestOptions, cancellationToken,
            (_, _) => true).ConfigureAwait(false);

    internal async Task<T> SendMultipartAsync<T>(Operation operation, string path, UploadAssetInput body,
        string fileName, string contentType, KiroraRequestOptions? requestOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(body.File);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (fileName.Any(char.IsControl) || fileName.IndexOfAny(['/', '\\']) >= 0)
            throw new ArgumentException("Use a file name without path components or control characters.", nameof(fileName));
        if (contentType is not ("image/png" or "image/jpeg" or "image/gif"))
            throw new ArgumentException("The API supports PNG, JPEG and GIF images.", nameof(contentType));
        if (body.File.Length is < 1 or > 2097152) throw new ArgumentException("An image must contain 1–2097152 bytes.", nameof(body));
        if (body.Name.IsSet && (body.Name.Value is null || body.Name.Value.Length is < 1 or > 200))
            throw new ArgumentException("The library name must contain 1–200 characters.", nameof(body));
        using var multipart = new MultipartFormDataContent();
        using var file = new ByteArrayContent(body.File.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        if (body.Name.IsSet) multipart.Add(new StringContent(body.Name.Value), "name");
        var bytes = await multipart.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteAsync(operation, path, null, null, requestOptions, cancellationToken,
            (payload, metadata) => JsonSerializer.Deserialize(payload, TypeInfo<T>()) ??
                throw new KiroraInvalidResponseException("The response was null.", metadata),
            bytes, multipart.Headers.ContentType!.ToString()).ConfigureAwait(false);
    }

    internal async Task DownloadToAsync(Operation operation, string path, Stream destination,
        KiroraRequestOptions? requestOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        await ExecuteAsync(operation, path, null, null, requestOptions, cancellationToken,
            (_, _) => true, destination: destination).ConfigureAwait(false);
    }

    private static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)Json.GetTypeInfo(typeof(T));

    private static JsonNode? Snapshot(object? value) => value is null ? null :
        JsonSerializer.SerializeToNode(value, Json.GetTypeInfo(value.GetType()));

    internal async IAsyncEnumerable<TItem> EnumerateAsync<TPage, TItem>(Operation operation, string path, object? query,
        KiroraRequestOptions? requestOptions, Func<TPage, IReadOnlyList<TItem>> items, Func<TPage, string?> cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var snapshot = Snapshot(query) as JsonObject ?? new JsonObject();
        KiroraResponseMetadata? lastMetadata = null;
        var controls = new KiroraRequestOptions
        {
            IdempotencyKey = requestOptions?.IdempotencyKey,
            ResponseReceived = metadata => { lastMetadata = metadata; requestOptions?.ResponseReceived?.Invoke(metadata); }
        };
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (snapshot["cursor"]?.GetValue<string>() is { } initial) seen.Add(initial);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await ExecuteAsync(operation, path, null, snapshot, controls, cancellationToken,
                (bytes, metadata) => JsonSerializer.Deserialize(bytes, TypeInfo<TPage>()) ??
                    throw new KiroraInvalidResponseException("The page was null.", metadata)).ConfigureAwait(false);
            foreach (var item in items(page)) { cancellationToken.ThrowIfCancellationRequested(); yield return item; }
            cancellationToken.ThrowIfCancellationRequested();
            var next = cursor(page);
            if (next is null) yield break;
            if (!seen.Add(next)) throw new KiroraInvalidResponseException("The API repeated a pagination cursor.",
                lastMetadata ?? new KiroraResponseMetadata(HttpStatusCode.OK, null, null, null, false));
            snapshot["cursor"] = next;
        }
    }

    private async Task<T> ExecuteAsync<T>(Operation operation, string path, JsonNode? body, JsonObject? query,
        KiroraRequestOptions? requestOptions, CancellationToken cancellationToken, Func<byte[], KiroraResponseMetadata, T> decode,
        byte[]? rawBody = null, string? contentType = null, Stream? destination = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (operation.Id != "getMe" && rawBody is null) ContractValidator.Validate(operation.Id, body, query);
        var key = requestOptions?.IdempotencyKey;
        if (key is not null)
        {
            if (!operation.AcceptsKey) throw new ArgumentException("This operation does not accept an Idempotency-Key header.", nameof(requestOptions));
            ValidateKey(key);
        }
        else if (operation.AcceptsKey) key = Guid.NewGuid().ToString();
        if (operation.Entries is { } entries && body?[entries] is JsonArray batch)
            foreach (var entry in batch.OfType<JsonObject>())
                if (entry["idempotencyKey"] is null) entry["idempotencyKey"] = Guid.NewGuid().ToString();
        // Serialization is complete before the first attempt; request objects are never changed.
        var bytes = rawBody ?? (body is null ? null : Encoding.UTF8.GetBytes(body.ToJsonString()));
        var safe = destination is null && (key is not null || operation.Retry is "read" or "converges" or "state" or "done" or "entries");
        if (operation.Id == "updateContact" && query?["idType"]?.GetValue<string>() is { } lookup && lookup != "id" && body is JsonObject fields && fields.ContainsKey(lookup))
        {
            var original = Uri.UnescapeDataString(path.Split('/')[3]);
            var changed = fields[lookup]?.GetValue<string>();
            safe = lookup == "email" ? string.Equals(original.Trim(), changed?.Trim(), StringComparison.OrdinalIgnoreCase) : original == changed;
        }
        var url = path;
        if (query is { Count: > 0 })
            url += "?" + string.Join("&", query.Select(pair =>
            {
                using var document = JsonDocument.Parse(pair.Value?.ToJsonString() ?? "null");
                var value = document.RootElement;
                return Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText());
            }));
        var mightHaveRun = false;
        var protectedByKey = key is not null || operation.Entries is not null;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var beganUtc = DateTimeOffset.UtcNow;
        Exception? retryFailure = null;
        TimeSpan RemainingKeyWindow()
        {
            var monotonic = System.Diagnostics.Stopwatch.GetElapsedTime(started);
            var wall = DateTimeOffset.UtcNow - beganUtc;
            return TimeSpan.FromHours(24) - (wall > monotonic ? wall : monotonic);
        }
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (attempt > 0 && protectedByKey && RemainingKeyWindow() <= TimeSpan.Zero)
                ExceptionDispatchInfo.Capture(retryFailure!).Throw();
            try
            {
                return await SendOnceAsync(operation, url, bytes, key, requestOptions, decode, cancellationToken, contentType, destination).ConfigureAwait(false);
            }
            catch (ResponseCallbackFailure error)
            {
                ExceptionDispatchInfo.Capture(error.InnerException!).Throw();
                throw;
            }
            catch (KiroraApiException error)
            {
                if (mightHaveRun && operation.Method == "DELETE" && operation.Retry is "done" or "converges" && error.StatusCode == HttpStatusCode.NotFound)
                    return decode([], error.Metadata);
                // OUTCOME_UNKNOWN must always be surfaced; never create a new key automatically.
                var worthRetrying = error.Code != "IDEMPOTENCY_KEY_OUTCOME_UNKNOWN" &&
                    (error.StatusCode == HttpStatusCode.TooManyRequests || safe && ((int)error.StatusCode >= 500 || error.Code == "IDEMPOTENCY_KEY_IN_USE"));
                if (attempt >= options.MaxRetries || !worthRetrying) throw;
                var delay = error.Metadata.RetryAfter ?? Backoff(attempt);
                if (delay > options.MaxRetryDelay || protectedByKey && delay >= RemainingKeyWindow()) throw;
                retryFailure = error;
                mightHaveRun |= error.StatusCode != HttpStatusCode.TooManyRequests;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (KiroraTransportException error) when (safe && attempt < options.MaxRetries)
            {
                var delay = Backoff(attempt);
                if (protectedByKey && delay >= RemainingKeyWindow()) throw;
                retryFailure = error;
                mightHaveRun = true;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class ResponseCallbackFailure(Exception original) : Exception("The response callback failed.", original);

    private static void ValidateKey(string key)
    {
        if (key.Length is < 8 or > 255 || key.Any(char.IsControl))
            throw new ArgumentException("An idempotency key must contain 8–255 characters without control characters.", nameof(key));
    }

    private TimeSpan Backoff(int attempt) => TimeSpan.FromMilliseconds(
        Math.Min(options.MaxRetryDelay.TotalMilliseconds, 1000 * Math.Pow(2, attempt) * (0.75 + Random.Shared.NextDouble() * 0.5)));

    private async Task<T> SendOnceAsync<T>(Operation operation, string path, byte[]? body, string? key,
        KiroraRequestOptions? requestOptions, Func<byte[], KiroraResponseMetadata, T> decode, CancellationToken cancellationToken,
        string? contentType = null, Stream? destination = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.RequestTimeout);
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(operation.Method), new Uri(options.BaseAddress, path));
            BearerAuthentication.Apply(request, options.ApiKey);
            request.Headers.Accept.ParseAdd(destination is null ? "application/json" : "text/csv");
            request.Headers.UserAgent.ParseAdd("kirora-dotnet/" + SdkVersion.Value);
            if (options.WorkspaceExternalId is not null) request.Headers.Add("Kirora-Workspace", options.WorkspaceExternalId);
            if (key is not null) request.Headers.Add("Idempotency-Key", key);
            if (body is not null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType ?? "application/json");
            }
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var metadata = KiroraResponseMetadata.From(response);
            try { requestOptions?.ResponseReceived?.Invoke(metadata); }
            catch (Exception error) { throw new ResponseCallbackFailure(error); }
            if (response.IsSuccessStatusCode && destination is not null)
            {
                await response.Content.CopyToAsync(destination, timeout.Token).ConfigureAwait(false);
                return decode([], metadata);
            }
            var bytes = await ReadBodyAsync(response, metadata, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw ParseError(bytes, metadata);
            try
            {
                if (response.StatusCode != HttpStatusCode.NoContent && destination is null)
                    ContractValidator.ValidateResponse(operation.Id, (int)response.StatusCode, bytes);
                return decode(bytes, metadata);
            }
            catch (JsonException error)
            {
                throw new KiroraInvalidResponseException("The response did not match the expected JSON contract.", metadata, error);
            }
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested) { throw new KiroraTimeoutException(error); }
        catch (HttpRequestException error) { throw new KiroraTransportException("The Kirora HTTP request failed.", error); }
        catch (IOException error) { throw new KiroraTransportException("The Kirora response could not be read.", error); }
    }

    private async Task<byte[]> ReadBodyAsync(HttpResponseMessage response, KiroraResponseMetadata metadata, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength > options.MaxResponseBytes)
            throw new KiroraInvalidResponseException("The response exceeds the configured size limit.", metadata);
        await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, options.MaxResponseBytes - (int)buffer.Length + 1)), token).ConfigureAwait(false)) != 0)
        {
            if (buffer.Length + read > options.MaxResponseBytes)
                throw new KiroraInvalidResponseException("The response exceeds the configured size limit.", metadata);
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static KiroraApiException ParseError(byte[] bytes, KiroraResponseMetadata metadata)
    {
        var code = metadata.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "UNAUTHORIZED", HttpStatusCode.Forbidden => "FORBIDDEN",
            HttpStatusCode.NotFound => "NOT_FOUND", HttpStatusCode.Conflict => "CONFLICT",
            HttpStatusCode.MethodNotAllowed => "METHOD_NOT_ALLOWED", HttpStatusCode.RequestEntityTooLarge => "PAYLOAD_TOO_LARGE",
            HttpStatusCode.UnsupportedMediaType => "UNSUPPORTED_MEDIA_TYPE",
            HttpStatusCode.TooManyRequests => "RATE_LIMITED", HttpStatusCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
            _ => (int)metadata.StatusCode >= 500 ? "INTERNAL_ERROR" : "BAD_REQUEST"
        };
        var message = $"Kirora API returned HTTP {(int)metadata.StatusCode}.";
        var errors = new List<FieldError>();
        JsonElement? details = null;
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
            {
                static string? Text(JsonElement e, string name) =>
                    e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                code = Text(error, "code") is { Length: > 0 } c ? c : code;
                message = Text(error, "message") is { Length: > 0 } m ? m : message;
                metadata = metadata with { RequestId = metadata.RequestId ?? Text(error, "requestId") };
                if (error.TryGetProperty("details", out var detail)) details = detail.Clone();
                if (error.TryGetProperty("errors", out var fields) && fields.ValueKind == JsonValueKind.Array)
                    foreach (var field in fields.EnumerateArray())
                    {
                        try
                        {
                            var parsed = field.Deserialize(KiroraJsonContext.Default.FieldError);
                            if (parsed is not null && parsed.Field is not null && parsed.Message is not null) errors.Add(parsed);
                        }
                        catch (JsonException) { /* A malformed entry must not hide the server refusal. */ }
                    }
            }
        }
        catch (JsonException) { /* Proxy or empty errors still retain status and request ID. */ }
        return KiroraApiException.Create(code, message, metadata, errors.AsReadOnly(), details);
    }
}
