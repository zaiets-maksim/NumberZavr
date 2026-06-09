using Microsoft.AspNetCore.Builder;
using NumberZavr;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddSingleton<GitHubStateService>();
builder.Services.AddSingleton<DataService>();
builder.Services.AddHostedService<BotWorker>();

var app = builder.Build();

// health endpoint
app.MapGet("/", () => "Bot is running");

app.Run($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "10000"}");