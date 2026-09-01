using HomeschoolManager.Infrastructure;
using HomeschoolManager.Application;
using HomeschoolManager.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

await builder.Build().RunAsync();
