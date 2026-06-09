using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace NumberZavr;

public class BotWorker : BackgroundService
{
    private readonly DataService _data;
    private readonly TelegramBotClient _bot;

    public BotWorker(DataService data, IConfiguration config)
    {
        _data = data;

        var token = config["TelegramToken"]
                    ?? throw new Exception("TelegramToken missing");

        _bot = new TelegramBotClient(token);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _data.InitAsync();

        Console.WriteLine("[BOT] Started");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        await Task.Delay(-1, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message?.Text == null)
            return;

        var text = update.Message.Text;

        Console.WriteLine($"[MSG] {text}");

        var phone = await _data.GetPhoneAsync();

        await bot.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: $"📱 Ваш номер: {phone}"
        );
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
        return Task.CompletedTask;
    }
}