using System.Globalization;

namespace Spx.Web.Components;

public static class UiFormatHelpers
{
    public static string FormatDateTime(DateTime value) =>
        value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}
