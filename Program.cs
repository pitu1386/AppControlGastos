using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AppControlGastos;

using AppControlGastos.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddScoped<SupabaseClientService>();
builder.Services.AddScoped<SupabaseSyncService>();
builder.Services.AddScoped<AppStateService>();

await builder.Build().RunAsync();
