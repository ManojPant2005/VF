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
using System.ServiceProcess;
using System.Net.Http;
using System.Text.Json;
using Blazored.LocalStorage;


var builder = WebApplication.CreateBuilder(args);

// Register essential services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register ServiceController with its dependencies
builder.Services.AddSingleton<PushSmsJob>(provider =>
{
    var databaseConfigService = provider.GetRequiredService<DatabaseConfigService>();
    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.json");
    return new PushSmsJob(databaseConfigService, configPath);
});

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

builder.Services.AddBlazoredLocalStorage();
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

var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.json");

// Register PushSmsJob as a singleton
builder.Services.AddSingleton<PushSmsJob>(provider =>
{
    var databaseConfigService = provider.GetRequiredService<DatabaseConfigService>();
    return new PushSmsJob(databaseConfigService, configPath);
});

// Register ServiceController to manage services
builder.Services.AddSingleton<UI.Services.ServiceController>();

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
