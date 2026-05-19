namespace Spx.Account;

public enum LoginOutcomeStatus
{
    Succeeded,
    ValidationFailed,
    InvalidCredentials,
    EmailConfirmationRequired,
    LockedOut,
}

public sealed record LoginOutcome(
    LoginOutcomeStatus Status,
    string ReturnUrl,
    string? Email = null
);

public enum RegisterOutcomeStatus
{
    ValidationFailed,
    PasswordMismatch,
    Failed,
    ConfirmationResendRequired,
    ConfirmationSent,
}

public sealed record RegisterOutcome(
    RegisterOutcomeStatus Status,
    string? Email = null,
    IReadOnlyList<string>? Errors = null
);

public enum ConfirmEmailOutcomeStatus
{
    InvalidLink,
    Succeeded,
    Failed,
}

public sealed record ConfirmEmailOutcome(ConfirmEmailOutcomeStatus Status, string? Email = null);

public enum ForgotPasswordOutcomeStatus
{
    ValidationFailed,
    Completed,
}

public sealed record ForgotPasswordOutcome(ForgotPasswordOutcomeStatus Status);

public enum ResetPasswordOutcomeStatus
{
    IncompleteLink,
    PasswordMismatch,
    Completed,
    Failed,
}

public sealed record ResetPasswordOutcome(
    ResetPasswordOutcomeStatus Status,
    string Email,
    string Code,
    IReadOnlyList<string>? Errors = null
);

public enum ResendConfirmationOutcomeStatus
{
    ValidationFailed,
    Completed,
}

public sealed record ResendConfirmationOutcome(
    ResendConfirmationOutcomeStatus Status,
    string? Email = null
);

public sealed record LogoutOutcome;
