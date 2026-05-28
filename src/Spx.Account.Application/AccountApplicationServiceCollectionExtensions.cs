using Microsoft.Extensions.DependencyInjection;
using Spx.Account.Application;
using Spx.Account.Application.Features.ConfirmEmail;
using Spx.Account.Application.Features.ForgotPassword;
using Spx.Account.Application.Features.Login;
using Spx.Account.Application.Features.Logout;
using Spx.Account.Application.Features.Register;
using Spx.Account.Application.Features.ResendConfirmation;
using Spx.Account.Application.Features.ResetPassword;

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
