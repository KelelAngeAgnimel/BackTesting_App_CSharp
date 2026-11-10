using BackTester.Web.Data;
using BackTester.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<MarketDataService>();
var apiKey = builder.Configuration["AlphaVantage:ApiKey"];
Console.WriteLine(apiKey is null ? "Clé introuvable" : "Clé chargée");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
