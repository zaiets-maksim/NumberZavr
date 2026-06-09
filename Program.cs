using System.Text.Json;
using Microsoft.Extensions.Hosting;
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

// Регистрация уведомлений жизненного цикла
builder.Services.AddHostedService<LifetimeEventsHostedService>();

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

public class LifetimeEventsHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ITelegramBotClient _bot;
    private readonly string _adminId;

    public LifetimeEventsHostedService(
        IHostApplicationLifetime appLifetime,
        ITelegramBotClient bot,
        IConfiguration config)
    {
        _appLifetime = appLifetime;
        _bot = bot;
        _adminId = config["AdminId"] ?? "";
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(OnStarted);
        _appLifetime.ApplicationStopping.Register(OnStopping);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnStarted()
    {
        if (long.TryParse(_adminId, out var id))
        {
            Task.Run(async () =>
            {
                try
                {
                    await _bot.SendMessage(id, "Крошечка, бот готовий для твоїх лапок! 🐾");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LIFETIME ERROR] {ex.Message}");
                }
            });
        }
    }

    private void OnStopping()
    {
        if (long.TryParse(_adminId, out var id))
        {
            try
            {
                _bot.SendMessage(id, "Крошечка, бот перезавантажується, почекай 2 хвилинки 🔄").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LIFETIME ERROR] {ex.Message}");
            }
        }
    }
}