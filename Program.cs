using Telegram.Bot;
using Telegram.Bot.Types;
using PhoneBot;

var builder = WebApplication.CreateBuilder(args);

var botToken = builder.Configuration["BotToken"]
    ?? throw new Exception("BotToken is not configured");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<BotHandler>();
builder.Services.ConfigureTelegramBotMvc();

var app = builder.Build();

app.MapPost("/webhook", async (Update update, BotHandler handler) =>
{
    Console.WriteLine($"[PhoneBot] Update: {update.Type}");
    await handler.HandleUpdateAsync(update);
    return Results.Ok();
});

app.MapGet("/", () => "PhoneBot is running ✓");

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(async () =>
{
    var bot = app.Services.GetRequiredService<ITelegramBotClient>();
    var data = app.Services.GetRequiredService<DataService>();
    var config = app.Services.GetRequiredService<IConfiguration>();

    var webhookUrl = config["WebhookUrl"]
        ?? throw new Exception("WebhookUrl is not configured");

    await bot.SetWebhook($"{webhookUrl.TrimEnd('/')}/webhook");
    Console.WriteLine($"[PhoneBot] Webhook set to {webhookUrl}/webhook");

    await data.InitAsync();
    Console.WriteLine("[PhoneBot] Data initialized");
});

app.Run();
