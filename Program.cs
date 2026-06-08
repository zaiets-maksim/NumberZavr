using Telegram.Bot;
using Telegram.Bot.Types;
using PhoneBot;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
var botToken = builder.Configuration["BotToken"]
    ?? throw new Exception("BotToken is not configured");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

// ── Webhook endpoint ──────────────────────────────────────────────────────────
app.MapPost("/webhook", async (Update update, BotHandler handler) =>
{
    await handler.HandleUpdateAsync(update);
    return Results.Ok();
});

// ── Health check (Render needs this) ─────────────────────────────────────────
app.MapGet("/", () => "PhoneBot is running ✓");

// ── Register webhook on startup ──────────────────────────────────────────────
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(async () =>
{
    var bot = app.Services.GetRequiredService<ITelegramBotClient>();
    var config = app.Services.GetRequiredService<IConfiguration>();
    var webhookUrl = config["WebhookUrl"]
        ?? throw new Exception("WebhookUrl is not configured");

    await bot.SetWebhook($"{webhookUrl.TrimEnd('/')}/webhook");
    Console.WriteLine($"[PhoneBot] Webhook set to {webhookUrl}/webhook");
});

app.Run();
