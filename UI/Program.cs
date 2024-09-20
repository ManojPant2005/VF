using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using UI.Data;
using UI.Jobs;
using UI.Services;
using System.Net.Http;
using System.Text.Json;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Graceful shutdown handling
builder.Host.ConfigureServices(services =>
{
    services.Configure<HostOptions>(options =>
    {
        // timeout for graceful shutdown to avoid exceptions when stopping services
        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    });
});

// Register PushSmsJob and ensure it's only registered once
builder.Services.AddSingleton<PushSmsJob>(provider =>
{
    var databaseConfigService = provider.GetRequiredService<DatabaseConfigService>();
    var logger = provider.GetRequiredService<ILogger<PushSmsJob>>();
    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.json");

    return new PushSmsJob(databaseConfigService, configPath, logger);
});

// Register ServiceController with PushSmsJob dependency
builder.Services.AddSingleton<UI.Services.ServiceController>(provider =>
{
    var pushSmsJob = provider.GetRequiredService<PushSmsJob>();
    return new UI.Services.ServiceController(pushSmsJob);
});

// Register Database-related services
builder.Services.AddSingleton<IDatabaseConfigurationService, SqlServerConfigurationService>();
builder.Services.AddSingleton<SqlServerConfigurationService>();
builder.Services.AddSingleton<DatabaseConfigurationFactory>();
builder.Services.AddSingleton<DatabaseConfigService>();
builder.Services.AddSingleton<DatabaseService>();

// Load the DB configuration from a JSON file and register it as a service
builder.Services.AddSingleton(async sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var response = await httpClient.GetAsync("dbconfig.json");
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    var dbConfig = JsonSerializer.Deserialize<DbConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    return dbConfig;
});

builder.Configuration.AddJsonFile("dbconfig.json", optional: false, reloadOnChange: true);

// Register Blazored Local Storage
builder.Services.AddBlazoredLocalStorage();

// Register MVC for controllers (if you have API controllers or MVC views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map Blazor hub and fallback route
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run the application
app.Run();
