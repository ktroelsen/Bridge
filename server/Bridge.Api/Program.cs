using System.Text.Json.Serialization;
using Bridge.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));

// Allow the Vite dev server to call the API cross-origin (it normally proxies /api anyway).
const string DevCors = "dev-frontend";
builder.Services.AddCors(o => o.AddPolicy(DevCors, p => p
    .WithOrigins("http://localhost:3000", "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseCors(DevCors);

// Serve the built React frontend from wwwroot (the deploy copies dist → wwwroot), so the
// whole app is one .NET site: frontend and API under one origin.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapDealEndpoints();
app.MapMiniBridgeEndpoints();

// SPA fallback after the API, so it never shadows it.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
