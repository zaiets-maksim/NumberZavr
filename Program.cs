using PhoneBot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var botClient = new TelegramBotClient(config["BotToken"]!);

// Ініціалізація DataService з вашими параметрами
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

app.MapPost("/webhook", async (JsonElement update, BotHandler handler) => {
    // Ваша логіка обробки (як ми робили раніше)
});

app.Run();