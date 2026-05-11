using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Spx.Account;
using Spx.Web.Options;

namespace Spx.Web.Adapters.Account;

public sealed class ResendAccountEmailSender(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    IHttpContextAccessor httpContextAccessor) : IAccountEmailSender
{
    private readonly ResendOptions resendOptions = options.Value;

    public Task SendConfirmationEmailAsync(string email, string userId, string code, CancellationToken cancellationToken = default)
    {
        var confirmationLink = BuildAbsoluteUri("/account/confirm-email", ("userId", userId), ("code", code));
        var html = $"<p>Confirm your account by <a href=\"{confirmationLink}\">clicking here</a>.</p>";
        return SendEmailAsync(email, "Confirm your email", html, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var resetLink = BuildAbsoluteUri("/reset-password", ("email", email), ("code", code));
        var html = $"<p>Reset your password by <a href=\"{resetLink}\">clicking here</a>.</p>";
        return SendEmailAsync(email, "Reset your password", html, cancellationToken);
    }

    private string BuildAbsoluteUri(string path, params (string Key, string? Value)[] values)
    {
        var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HTTP request is available for building account links.");
        var query = new QueryBuilder();

        foreach (var (key, value) in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add(key, value);
            }
        }

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{path}{query}";
    }

    private async Task SendEmailAsync(string email, string subject, string html, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resendOptions.ApiKey) || string.IsNullOrWhiteSpace(resendOptions.FromEmail))
        {
            throw new InvalidOperationException("Resend configuration is missing. Set Resend:ApiKey and Resend:FromEmail.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Add("Authorization", $"Bearer {resendOptions.ApiKey}");
        request.Content = JsonContent.Create(new ResendEmailRequest(
            From: string.IsNullOrWhiteSpace(resendOptions.FromName)
                ? resendOptions.FromEmail
                : $"{resendOptions.FromName} <{resendOptions.FromEmail}>",
            To: [email],
            Subject: subject,
            Html: html));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}