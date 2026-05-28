namespace Spx.Nexus.Application;

internal static class NexusInputValidation
{
    public static bool TryNormalizeGameName(
        string value,
        out string normalizedValue,
        out string errorMessage
    )
    {
        normalizedValue = NexusInputNormalizer.NormalizeDisplayText(value);

        if (normalizedValue.Length < 2)
        {
            errorMessage = "Game names must be at least 2 characters long.";
            return false;
        }

        if (normalizedValue.Length > 100)
        {
            errorMessage = "Game names must be 100 characters or fewer.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool TryNormalizePlayerName(
        string value,
        out string normalizedValue,
        out string normalizedLookupValue,
        out string errorMessage
    )
    {
        normalizedValue = NexusInputNormalizer.NormalizeDisplayText(value);
        normalizedLookupValue = NexusInputNormalizer.NormalizeLookupKey(value);

        if (normalizedValue.Length < 2)
        {
            errorMessage = "Player names must be at least 2 characters long.";
            return false;
        }

        if (normalizedValue.Length > 40)
        {
            errorMessage = "Player names must be 40 characters or fewer.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
