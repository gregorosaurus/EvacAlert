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

            builder.Services.AddSingleton<Functions.GeocodeAddresses.GeocodeStorageOptions>(ctx =>
            {
                return new Functions.GeocodeAddresses.GeocodeStorageOptions()
                {
                    ConnectionString = Environment.GetEnvironmentVariable("DataLakeConnectionString"),
                    Container = Environment.GetEnvironmentVariable("OutputDataContainer") ?? "data",
                    OutputFilePath = Environment.GetEnvironmentVariable("OutputDataPath")
                };
            });

            builder.Services.AddScoped<Services.IGeoCodingService, Services.AzureMapsService>();

            builder.Services.AddScoped<Services.IEvacuationService, Services.BCEvacuationAreaService>();
        }
    }
}
