namespace Spx.Nexus.Application;

internal static class NexusInputNormalizer
{
    public static string NormalizeDisplayText(string value) =>
        string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );

    public static string NormalizeLookupKey(string value) =>
        NormalizeDisplayText(value).ToUpperInvariant();
}
