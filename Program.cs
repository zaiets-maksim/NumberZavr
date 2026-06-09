using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NumberZavr;

var builder = WebApplication.CreateBuilder(args);

// конфиг
builder.Configuration.AddJsonFile("appsettings.json", optional: true);

var app = builder.Build();

// ==========================
// 👇 твой бот (фон)
// ==========================
var github = new GitHubStateService(
    builder.Configuration["GitHubOwner"],
    builder.Configuration["GitHubRepo"]
);

var dataService = new DataService(github);

// запускаем бот в фоне
_ = Task.Run(async () =>
{
    await dataService.InitAsync();
    Console.WriteLine("[BOT] Started");

    while (true)
    {
        await Task.Delay(1000);
    }
});

// ==========================
// 👇 фейковый HTTP сервер
// ==========================
app.MapGet("/", () => "Bot is running");

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";

app.Run($"http://0.0.0.0:{port}");