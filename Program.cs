using PhoneBot;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var botClient = new TelegramBotClient(config["BotToken"]!);

var dataService = new DataService(
    botClient, 
    long.Parse(config["StateChatId"]!), 
    int.Parse(config["StateMessageId"]!)
);
await dataService.InitializeAsync(config["NumbersUrl"]!);

builder.Services.AddSingleton<ITelegramBotClient>(botClient);
builder.Services.AddSingleton(dataService);
builder.Services.AddSingleton<BotHandler>();

var app = builder.Build();

app.MapPost("/webhook", async (Update update, BotHandler handler) => {
    if (update.Message != null) await handler.HandleMessageAsync(update.Message);
    if (update.CallbackQuery != null) await handler.HandleCallbackAsync(update.CallbackQuery);
    return Results.Ok();
});

app.Run();