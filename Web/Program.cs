using EvacAlert.Explore.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEvacuationDataService, EvacAzFunctionService>();
builder.Services.AddSingleton<EvacAzFunctionService.Options>(ctx =>
{
    return new EvacAzFunctionService.Options()
    {
        EvacAlertFunctionEndpoint = ctx.GetRequiredService<IConfiguration>().GetValue<string>("EvacAlertFunctionEndpoint"),
        EvacAlertFunctionKey = ctx.GetRequiredService<IConfiguration>().GetValue<string>("EvacAlertFunctionKey")
    };
});
builder.Services.AddSingleton<StaticBlobStorageInformationService.Options>(ctx =>
{
    return new StaticBlobStorageInformationService.Options()
    {
        FacilitiesCSVUrl = ctx.GetRequiredService<IConfiguration>().GetValue<string>("FacilitiesCsvUrl"),
        RegionsGeoJsonUrl = ctx.GetRequiredService<IConfiguration>().GetValue<string>("RegionsGeoJsonUrl")
    };
});
builder.Services.AddScoped<IStaticLocationInformationService, StaticBlobStorageInformationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

