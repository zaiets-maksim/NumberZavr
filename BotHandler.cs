using System.Text.Json;
using NumberZavr;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PhoneBot;

public class BotHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly DataService _data;
    private readonly long _adminId;

    public BotHandler(
        ITelegramBotClient bot,
        DataService data,
        IConfiguration config)
    {
        _bot = bot;
        _data = data;
        _adminId = long.Parse(config["AdminId"] ?? "0");
    }

    public async Task HandleRawUpdateAsync(JsonElement update)
    {
        if (update.TryGetProperty("message", out var msgElement))
        {
            var msg = JsonSerializer.Deserialize<Message>(
                msgElement.GetRawText(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (msg != null)
                await HandleMessageAsync(msg);
        }
    }

    private async Task HandleMessageAsync(Message msg)
    {
        if (msg.Text == null)
            return;

        var isAdmin = msg.From?.Id == _adminId;

        switch (msg.Text.Trim())
        {
            case "/start":
                await _bot.SendMessage(
                    msg.Chat.Id,
                    "👋 Привіт! Натисни кнопку нижче щоб отримати номер.",
                    replyMarkup: MainKeyboard(isAdmin));
                break;

            case "📋 Номер":
                await HandleGetNumber(
                    msg.From!.Id,
                    msg.Chat.Id);
                break;

            case "🔄 Оновити":
                if (!isAdmin)
                    return;

                await _data.LoadPhonesAsync();

                await _bot.SendMessage(
                    msg.Chat.Id,
                    "✅ Номери оновлено.",
                    replyMarkup: MainKeyboard(true));
                break;

            case "🗑️ Скинути":
                if (!isAdmin)
                    return;

                await _data.ResetStateAsync();

                await _bot.SendMessage(
                    msg.Chat.Id,
                    "🗑️ Стан бота повністю скинуто (індекс обнулено, історію використаних номерів очищено).",
                    replyMarkup: MainKeyboard(true));
                break;
        }
    }

    private async Task HandleGetNumber(long userId, long chatId)
    {
        var number = await _data.GetPhoneAsync();

        string response = number == null
            ? "😕 Усі номери вже використані. Спробуйте пізніше."
            : $"📞 Твій номер:\n{number}";

        await _bot.SendMessage(
            chatId,
            response,
            replyMarkup: MainKeyboard(userId == _adminId));
    }

    private static ReplyKeyboardMarkup MainKeyboard(bool isAdmin)
    {
        var rows = new List<KeyboardButton[]>
        {
            new[]
            {
                new KeyboardButton("📋 Номер")
            }
        };

        if (isAdmin)
        {
            rows.Add(new[]
            {
                new KeyboardButton("🔄 Оновити"),
                new KeyboardButton("🗑️ Скинути")
            });
        }

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
    }
}