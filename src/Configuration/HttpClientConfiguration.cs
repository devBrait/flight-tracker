using FlightTracker.Common;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlightTracker.Configuration
{
    public static class HttpClientConfiguration
    {
        public static IServiceCollection AddExternalHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient("Amadeus", client =>
            {
                client.BaseAddress = new Uri(Constants.AmadeusTestBase);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = Constants.MaxRetries;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
            });

            services.AddHttpClient("Telegram", client =>
            {
                client.BaseAddress = new Uri(Constants.TelegramApiBase);
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = Constants.MaxRetries;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
            });

            return services;
        }
    }
}
