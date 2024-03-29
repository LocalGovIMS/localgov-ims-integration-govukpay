﻿using Application.Behaviours;
using Application.Cryptography;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Application
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));

            services.AddTransient<ICryptographyService, MD5CryptographyService>();
          
            services.AddTransient((apiKey) =>
            {
                return new Func<string, GovUKPayApiClient.Api.ICardPaymentsApiAsync>(
                    (apiKey) => {

                        var config = new GovUKPayApiClient.Client.Configuration();
                        config.AccessToken = apiKey;

                        return new GovUKPayApiClient.Api.CardPaymentsApi(config);
                    }
                );
            });

            services.AddTransient((apiKey) =>
            {
                return new Func<string, GovUKPayApiClient.Api.IRefundingCardPaymentsApiAsync>(
                    (apiKey) => {

                        var config = new GovUKPayApiClient.Client.Configuration();
                        config.AccessToken = apiKey;

                        return new GovUKPayApiClient.Api.RefundingCardPaymentsApi(config);
                    }
                );
            });

            AddLocalGovImsApiClients(services, configuration);

            return services;
        }

        private static IServiceCollection AddLocalGovImsApiClients(this IServiceCollection services, IConfiguration configuration)
        {
            var localGovImsApiBaseUrl = configuration.GetValue<string>("LocalGovImsApiUrl");

            services.AddTransient<LocalGovImsApiClient.Api.IFundMetadataApiAsync>(s => new LocalGovImsApiClient.Api.FundMetadataApi(localGovImsApiBaseUrl));
            services.AddTransient<LocalGovImsApiClient.Api.IFundsApiAsync>(s => new LocalGovImsApiClient.Api.FundsApi(localGovImsApiBaseUrl));
            services.AddTransient<LocalGovImsApiClient.Api.IMethodOfPaymentsApiAsync>(s => new LocalGovImsApiClient.Api.MethodOfPaymentsApi(localGovImsApiBaseUrl));
            services.AddTransient<LocalGovImsApiClient.Api.IPendingTransactionsApiAsync>(s => new LocalGovImsApiClient.Api.PendingTransactionsApi(localGovImsApiBaseUrl));
            services.AddTransient<LocalGovImsApiClient.Api.IProcessedTransactionsApiAsync>(s => new LocalGovImsApiClient.Api.ProcessedTransactionsApi(localGovImsApiBaseUrl));

            return services;
        }
    }
}
