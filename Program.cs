using System.Text.Json;
using PhoneBot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
var botClient = new TelegramBotClient(builder.Configuration["BotToken"]!);

builder.Services.AddSingleton<ITelegramBotClient>(botClient);
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

// Використовуємо JsonElement для стабільності
app.MapPost("/webhook", async (JsonElement update, BotHandler handler) =>
{
    await handler.HandleRawUpdateAsync(update);
    return Results.Ok();
});

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(async () =>
{
    var data = app.Services.GetRequiredService<DataService>();
    await data.InitAsync();
});

app.Run();