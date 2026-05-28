using Microsoft.Extensions.DependencyInjection;
using Spx.Account.Application;
using Spx.Web.Adapters.Account;

namespace Microsoft.Extensions.DependencyInjection;

public static class AccountWebServiceCollectionExtensions
{
    public static IServiceCollection AddAccountWebAdapters(this IServiceCollection services)
    {
        services.AddSingleton<AccountLinkBuilder>();
        services.AddScoped<IAccountIdentity, IdentityAccountIdentityAdapter>();

        return services;
    }
}
