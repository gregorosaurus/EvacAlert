using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(EvacAlert.Startup))]
namespace EvacAlert
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<Services.AzureMapsService.Options>(ctx =>
            {
                return new Services.AzureMapsService.Options()
                {
                    ApiKey = Environment.GetEnvironmentVariable("AzureMapsApiKey")
                };
            });

            builder.Services.AddScoped<Services.IGeoCodingService, Services.AzureMapsService>();

            builder.Services.AddScoped<Services.IEvacuationService, Services.BCEvacuationAreaService>();
        }
    }
}
