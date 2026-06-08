using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using PhoneBot;

var builder = WebApplication.CreateBuilder(args);

var botToken = builder.Configuration["BotToken"]
    ?? throw new Exception("BotToken is not configured");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

app.MapPost("/webhook", async (HttpContext ctx, BotHandler handler) =>
{
    try
    {
        var body = await new System.IO.StreamReader(ctx.Request.Body).ReadToEndAsync();
        Console.WriteLine($"[PhoneBot] Webhook body: {body[..Math.Min(200, body.Length)]}");
        var update = JsonSerializer.Deserialize<Update>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (update is not null)
            await handler.HandleUpdateAsync(update);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PhoneBot] Webhook error: {ex.Message}");
    }
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
