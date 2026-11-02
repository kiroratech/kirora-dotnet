namespace Kirora.Resources;

internal static class ResourceHelpers
{
    internal static string Segment(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value is "." or "..") throw new ArgumentException("A resource identifier cannot be a dot segment.", nameof(value));
        return Uri.EscapeDataString(value);
    }
}
