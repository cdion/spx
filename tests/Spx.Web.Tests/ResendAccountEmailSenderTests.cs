using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Spx.Web.Adapters.Account;
using Spx.Web.Options;
using Xunit;

namespace Spx.Web.Tests;

public sealed class ResendAccountEmailSenderTests
{
    [Fact]
    public async Task SendConfirmationEmailAsync_includes_resend_response_body_when_request_fails()
    {
        using var httpClient = new HttpClient(
            new StubHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent(
                        "{\"message\":\"The from address is not verified.\"}"
                    ),
                }
            )
        )
        {
            BaseAddress = new Uri("https://api.resend.com/"),
        };

        var sender = new ResendAccountEmailSender(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(
                new ResendOptions
                {
                    ApiKey = "test-key",
                    FromEmail = "noreply@example.com",
                    FromName = "Spx",
                }
            ),
            new AccountLinkBuilder(
                Microsoft.Extensions.Options.Options.Create(
                    new AppUrlOptions { BaseUrl = "https://example.com" }
                )
            )
        );

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendConfirmationEmailAsync("user@example.com", "user-1", "confirm-token")
        );

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Contains("Resend response", exception.Message);
        Assert.Contains("The from address is not verified.", exception.Message);
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(response);
    }
}
