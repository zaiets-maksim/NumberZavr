using System.Text.Json;
using NumberZavr;
using PhoneBot;
using Telegram.Bot;

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

// Регистрация клиента Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var token = sp.GetRequiredService<IConfiguration>()["BotToken"]
                ?? throw new Exception("BotToken missing");
    return new TelegramBotClient(token);
});

// Регистрация обработчика сообщений
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

// Инициализация DataService при старте
var dataService = app.Services.GetRequiredService<DataService>();
await dataService.InitAsync();

// Настройка Webhook при запуске
var webhookUrl = config["WebhookUrl"];
if (!string.IsNullOrEmpty(webhookUrl))
{
    var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
    await botClient.SetWebhook(
        url: $"{webhookUrl.TrimEnd('/')}/bot"
    );
    Console.WriteLine($"[BOT] Webhook set to {webhookUrl.TrimEnd('/')}/bot");
}

// 🔥 важно для Render (порт)
app.MapGet("/", () => "Bot is running");

// Endpoint для приема обновлений от Telegram
app.MapPost("/bot", async (JsonElement update, BotHandler handler) =>
{
    try
    {
        await handler.HandleRawUpdateAsync(update);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
    }
    return Results.Ok();
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");