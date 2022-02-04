using Application.Behaviours;
using Application.Cryptography;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Application
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));

            services.AddTransient<ICryptographyService, MD5CryptographyService>();
          
            services.AddTransient((apiKey) =>
            {
                return new Func<string, GovUkPayApiClient.Api.ICardPaymentsApi>(
                    (apiKey) => {

                        var config = new GovUkPayApiClient.Client.Configuration();
                        config.AccessToken = apiKey;

                        return new GovUkPayApiClient.Api.CardPaymentsApi(config);
                    }
                );
            });

            services.AddTransient<LocalGovImsApiClient.IClient, LocalGovImsApiClient.Client>();

            return services;
        }
    }
}
