using NumberZavr;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

// ✅ правильно через DI factory
builder.Services.AddSingleton<GitHubStateService>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    return new GitHubStateService(
        cfg["GitHubOwner"],
        cfg["GitHubRepo"],
        cfg["GitHubToken"]
    );
});

builder.Services.AddSingleton<DataService>();
builder.Services.AddHostedService<BotWorker>();

var app = builder.Build();

// 🔥 важно для Render (порт)
app.MapGet("/", () => "Bot is running");

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");