namespace Spx.Web.Options;

public sealed class AppUrlOptions
{
    public const string SectionName = "AppUrl";

    public string BaseUrl { get; set; } = string.Empty;
}