using System.Text.Json;
using PhoneBot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

var botToken = builder.Configuration["BotToken"] ?? throw new Exception("BotToken missing");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

// Використовуємо JsonElement для bypass-десеріалізації
app.MapPost("/webhook", async (JsonElement update, BotHandler handler) =>
{
    await handler.HandleRawUpdateAsync(update);
    return Results.Ok();
});

app.MapGet("/", () => "PhoneBot is running ✓");

// Ініціалізація Webhook
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(async () =>
{
    var bot = app.Services.GetRequiredService<ITelegramBotClient>();
    var data = app.Services.GetRequiredService<DataService>();
    var config = app.Services.GetRequiredService<IConfiguration>();
    var webhookUrl = config["WebhookUrl"] ?? throw new Exception("WebhookUrl missing");

    await bot.SetWebhook($"{webhookUrl.TrimEnd('/')}/webhook");
    await data.InitAsync();
    Console.WriteLine("[PhoneBot] System Ready");
});

app.Run();