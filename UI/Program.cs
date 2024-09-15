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

var builder = WebApplication.CreateBuilder(args);

// Registering essential services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register WindowsServiceManager with its dependencies
builder.Services.AddSingleton<WindowsServiceManager>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<WindowsServiceManager>>();
    return new WindowsServiceManager(configuration, logger);
});

builder.Configuration.AddJsonFile("dbconfig.json", optional: true, reloadOnChange: true);

// Register Database-related services
builder.Services.AddSingleton<IDatabaseConfigurationService, SqlServerConfigurationService>();
builder.Services.AddSingleton<SqlServerConfigurationService>();
builder.Services.AddSingleton<DatabaseConfigurationFactory>();
builder.Services.AddSingleton<DatabaseConfigService>();
builder.Services.AddSingleton<DatabaseService>();


var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.json");

// Register PushSmsJob as a hosted service and pass the configPath

builder.Services.AddHostedService(sp => new PushSmsJob(sp.GetRequiredService<DatabaseConfigService>(), configPath));

builder.Services.AddSingleton<PushSmsJob>(provider =>
{
    var databaseConfigService = provider.GetRequiredService<DatabaseConfigService>();
    return new PushSmsJob(databaseConfigService, configPath);
});

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