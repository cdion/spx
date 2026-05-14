using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Spx.Web.Options;

namespace Spx.Web.Adapters.Account;

public sealed class AccountLinkBuilder(IOptions<AppUrlOptions> options)
{
    private readonly Uri baseUri = CreateBaseUri(options.Value.BaseUrl);

    public string BuildConfirmationLink(string userId, string code)
        => BuildAbsoluteUri("account/confirm-email", ("userId", userId), ("code", code));

    public string BuildPasswordResetLink(string email, string code)
        => BuildAbsoluteUri("reset-password", ("email", email), ("code", code));

    private string BuildAbsoluteUri(string path, params (string Key, string? Value)[] values)
    {
        var query = new QueryBuilder();

        foreach (var (key, value) in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add(key, value);
            }
        }

        var absolutePath = new Uri(baseUri, path).GetLeftPart(UriPartial.Path);
        return $"{absolutePath}{query}";
    }

    private static Uri CreateBaseUri(string baseUrl)
    {
        var normalizedBaseUrl = baseUrl.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl))
        {
            throw new InvalidOperationException("AppUrl configuration is missing. Set AppUrl:BaseUrl.");
        }

        var candidate = normalizedBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? normalizedBaseUrl
            : $"{normalizedBaseUrl}/";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("AppUrl:BaseUrl must be an absolute http or https URL.");
        }

        return uri;
    }
}