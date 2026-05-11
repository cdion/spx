using Microsoft.Extensions.DependencyInjection;
using Spx.Account;
using Spx.Account.Features.ConfirmEmail;
using Spx.Account.Features.ForgotPassword;
using Spx.Account.Features.Login;
using Spx.Account.Features.Logout;
using Spx.Account.Features.Register;
using Spx.Account.Features.ResendConfirmation;
using Spx.Account.Features.ResetPassword;

namespace Microsoft.Extensions.DependencyInjection;

public static class AccountApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAccountApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoginHandler, LoginHandler>();
        services.AddScoped<ILogoutHandler, LogoutHandler>();
        services.AddScoped<IRegisterHandler, RegisterHandler>();
        services.AddScoped<IConfirmEmailHandler, ConfirmEmailHandler>();
        services.AddScoped<IForgotPasswordHandler, ForgotPasswordHandler>();
        services.AddScoped<IResetPasswordHandler, ResetPasswordHandler>();
        services.AddScoped<IResendConfirmationHandler, ResendConfirmationHandler>();

        return services;
    }
}