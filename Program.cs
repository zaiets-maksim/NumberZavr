using PhoneBot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
var botClient = new TelegramBotClient(builder.Configuration["BotToken"]!);

builder.Services.AddSingleton<ITelegramBotClient>(botClient);
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

app.MapPost("/webhook", async (Update update, BotHandler handler) =>
{
    if (update.CallbackQuery != null) await handler.HandleCallbackAsync(update.CallbackQuery);
    return Results.Ok();
});

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(async () =>
{
    var data = app.Services.GetRequiredService<DataService>();
    await data.InitAsync();
});

app.Run();