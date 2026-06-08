using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PhoneBot;

public class BotHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly DataService _data;
    private readonly long _adminId;

    public BotHandler(ITelegramBotClient bot, DataService data, IConfiguration config)
    {
        _bot = bot;
        _data = data;
        _adminId = long.Parse(config["AdminId"] ?? "0");
    }

    public async Task HandleRawUpdateAsync(JsonElement update)
    {
        // Ручний парсинг замість довіри до вбудованого мапінгу
        if (update.TryGetProperty("message", out var msgElement))
        {
            var msg = JsonSerializer.Deserialize<Message>(msgElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (msg != null) await HandleMessageAsync(msg);
        }
        else if (update.TryGetProperty("callback_query", out var cbElement))
        {
            var cb = JsonSerializer.Deserialize<CallbackQuery>(cbElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cb != null) await HandleCallbackAsync(cb);
        }
    }

    private async Task HandleMessageAsync(Message msg)
    {
        if (msg.Text?.Trim() == "/start")
        {
            await _bot.SendMessage(msg.Chat.Id, "👋 Привіт! Натисни *Номер*.", 
                parseMode: ParseMode.MarkdownV2, replyMarkup: MainKeyboard(msg.From?.Id == _adminId));
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery cb)
    {
        if (cb.Message is null) return;
        await _bot.AnswerCallbackQuery(cb.Id);

        switch (cb.Data)
        {
            case "get_number":
                await HandleGetNumber(cb.From.Id, cb.Message.Chat.Id);
                break;
            case "reload_numbers":
                await _data.LoadPhonesAsync();
                await _bot.SendMessage(cb.Message.Chat.Id, "✅ Оновлено");
                break;
        }
    }

    // ... (решта методів: HandleGetNumber, MainKeyboard, Escape без змін)
}