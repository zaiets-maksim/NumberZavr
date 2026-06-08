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
        _adminId = long.Parse(config["AdminId"] ?? throw new Exception("AdminId not configured"));
    }

    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Type == UpdateType.Message && update.Message is { } msg)
            await HandleMessageAsync(msg);
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } cb)
            await HandleCallbackAsync(cb);
    }

    private async Task HandleMessageAsync(Message msg)
    {
        if (msg.From is null) return;
        long userId = msg.From.Id;
        long chatId = msg.Chat.Id;
        string text = msg.Text?.Trim() ?? "";

        if (text is "/start" or "/menu")
        {
            bool isAdmin = userId == _adminId;
            await _bot.SendMessage(chatId,
                "👋 Привіт\\! Натисни *Номер*, щоб отримати номер телефону\\.\n📋 Ліміт: *2 рази / 24 год*",
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: MainKeyboard(isAdmin));
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery cb)
    {
        if (cb.From is null || cb.Message is null) return;
        long userId = cb.From.Id;
        long chatId = cb.Message.Chat.Id;
        bool isAdmin = userId == _adminId;

        await _bot.AnswerCallbackQuery(cb.Id);

        switch (cb.Data)
        {
            case "get_number":
                await HandleGetNumber(userId, chatId);
                break;

            case "reload_numbers":
                if (!isAdmin) return;
                await _data.LoadPhonesAsync();
                int count = _data.GetPhones().Count;
                await _bot.SendMessage(chatId,
                    $"🔄 Номери оновлено\\! Зараз в базі: *{count}* номерів\\.",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: MainKeyboard(isAdmin: true));
                break;

            case "back":
                await _bot.SendMessage(chatId, "🏠 Головне меню",
                    replyMarkup: MainKeyboard(isAdmin));
                break;
        }
    }

    private async Task HandleGetNumber(long userId, long chatId)
    {
        var (number, remaining) = await _data.TryIssuePhoneAsync(userId);

        if (number is null)
        {
            bool noPhones = _data.GetPhones().Count == 0;
            string msg = noPhones
                ? "😕 База номерів порожня\\."
                : "⏳ Ти вже отримав свій ліміт номерів сьогодні\\.\n_Повертайся завтра\\!_";
            await _bot.SendMessage(chatId, msg, parseMode: ParseMode.MarkdownV2);
            return;
        }

        string remainingText = remaining == 0
            ? "❌ Ліміт вичерпано на сьогодні"
            : $"🔄 Ще {remaining} раз сьогодні";

        await _bot.SendMessage(chatId,
            $"📞 Твій номер:\n\n`{Escape(number)}`\n\n_{remainingText}_",
            parseMode: ParseMode.MarkdownV2);
    }

    private static InlineKeyboardMarkup MainKeyboard(bool isAdmin)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            [InlineKeyboardButton.WithCallbackData("📋 Номер", "get_number")]
        };
        if (isAdmin)
            rows.Add([InlineKeyboardButton.WithCallbackData("🔄 Оновити номери з файлу", "reload_numbers")]);

        return new InlineKeyboardMarkup(rows);
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("_", "\\_").Replace("*", "\\*")
         .Replace("[", "\\[").Replace("]", "\\]").Replace("(", "\\(")
         .Replace(")", "\\)").Replace("~", "\\~").Replace("`", "\\`")
         .Replace(">", "\\>").Replace("#", "\\#").Replace("+", "\\+")
         .Replace("-", "\\-").Replace("=", "\\=").Replace("|", "\\|")
         .Replace("{", "\\{").Replace("}", "\\}").Replace(".", "\\.")
         .Replace("!", "\\!");
}
