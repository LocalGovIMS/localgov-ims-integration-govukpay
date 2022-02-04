using Application.Clients.LocalGovImsPaymentApi;
using Application.Data;
using Infrastructure.Clients.LocalGovImsPaymentApi;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddTransient<ILocalGovImsPaymentApiClient, LocalGovImsPaymentApiClient>();

            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

            return services;
        }
    }
}
