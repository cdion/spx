using Microsoft.Extensions.Options;
using Spx.Web.Adapters.Account;
using Spx.Web.Options;
using Xunit;

namespace Spx.Web.Tests;

public sealed class AccountLinkBuilderTests
{
    [Fact]
    public void BuildConfirmationLink_uses_configured_base_url()
    {
        var builder = new AccountLinkBuilder(Microsoft.Extensions.Options.Options.Create(new AppUrlOptions
        {
            BaseUrl = "https://example.com"
        }));

        var link = builder.BuildConfirmationLink("user-1", "abc123");

        Assert.Equal("https://example.com/account/confirm-email?userId=user-1&code=abc123", link);
    }

    [Fact]
    public void BuildPasswordResetLink_uses_query_encoding_and_preserves_base_path()
    {
        var builder = new AccountLinkBuilder(Microsoft.Extensions.Options.Options.Create(new AppUrlOptions
        {
            BaseUrl = "https://example.com/spx"
        }));

        var link = builder.BuildPasswordResetLink("user+test@example.com", "code value");

        Assert.Equal("https://example.com/spx/reset-password?email=user%2Btest@example.com&code=code%20value", link);
    }

    [Fact]
    public void Constructor_throws_when_base_url_is_missing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new AccountLinkBuilder(Microsoft.Extensions.Options.Options.Create(new AppUrlOptions())));

        Assert.Equal("AppUrl configuration is missing. Set AppUrl:BaseUrl.", exception.Message);
    }
}