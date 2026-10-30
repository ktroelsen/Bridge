using System.Text.Json.Serialization;
using Bridge.Api;
using Bridge.Api.Data;
using Bridge.Api.Players;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQLite in App_Data under the content root. The deploy skips App_Data, so players and their
// progress survive redeploys. Override with ConnectionStrings__Default.
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? $"Data Source={Path.Combine(dataDir, "bridge.db")}";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));

// Allow the Vite dev server to call the API cross-origin (it normally proxies /api anyway).
const string DevCors = "dev-frontend";
builder.Services.AddCors(o => o.AddPolicy(DevCors, p => p
    .WithOrigins("http://localhost:3000", "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

if (app.Environment.IsDevelopment())
    app.UseCors(DevCors);

// Serve the built React frontend from wwwroot (the deploy copies dist → wwwroot), so the
// whole app is one .NET site: frontend and API under one origin.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapDealEndpoints();
app.MapMiniBridgeEndpoints();
app.MapPlayerEndpoints();

// SPA fallback after the API, so it never shadows it.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
