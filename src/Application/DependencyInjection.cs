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
                return new Func<string, GovUKPayApiClient.Api.ICardPaymentsApi>(
                    (apiKey) => {

                        var config = new GovUKPayApiClient.Client.Configuration();
                        config.AccessToken = apiKey;

                        return new GovUKPayApiClient.Api.CardPaymentsApi(config);
                    }
                );
            });
            
            services.AddTransient<LocalGovImsApiClient.Api.IFundMetadataApi>(s => new LocalGovImsApiClient.Api.FundMetadataApi("https://localhost:44364/"));
            services.AddTransient<LocalGovImsApiClient.Api.IFundsApi>(s => new LocalGovImsApiClient.Api.FundsApi("https://localhost:44364/"));
            services.AddTransient<LocalGovImsApiClient.Api.IMethodOfPaymentsApi>(s => new LocalGovImsApiClient.Api.MethodOfPaymentsApi("https://localhost:44364/"));
            services.AddTransient<LocalGovImsApiClient.Api.IPendingTransactionsApi>(s => new LocalGovImsApiClient.Api.PendingTransactionsApi("https://localhost:44364/"));
            services.AddTransient<LocalGovImsApiClient.Api.IProcessedTransactionsApi>(s => new LocalGovImsApiClient.Api.ProcessedTransactionsApi("https://localhost:44364/"));

            return services;
        }
    }
}
