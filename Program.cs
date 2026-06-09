using NumberZavr;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;

var github = new GitHubStateService(
    config["GitHubOwner"],
    config["GitHubRepo"]
);

builder.Services.AddSingleton(github);
builder.Services.AddSingleton<DataService>();
builder.Services.AddHostedService<BotWorker>();

var app = builder.Build();

app.MapGet("/", () => "Bot is running");

app.Run($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "10000"}");