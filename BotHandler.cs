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

    // States for admin add/remove flows
    private static readonly Dictionary<long, string> _pendingAction = new();

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

    // ── Messages ─────────────────────────────────────────────────────────────

    private async Task HandleMessageAsync(Message msg)
    {
        if (msg.From is null) return;
        long userId = msg.From.Id;
        long chatId = msg.Chat.Id;
        string text = msg.Text?.Trim() ?? "";

        // Admin is in "add phone" flow
        if (_pendingAction.TryGetValue(userId, out var action))
        {
            _pendingAction.Remove(userId);
            if (action == "add")
            {
                bool added = await _data.AddPhoneAsync(text);
                string reply = added
                    ? $"✅ Номер *{Escape(text)}* додано\\."
                    : $"⚠️ Номер *{Escape(text)}* вже є в базі\\.";
                await _bot.SendMessage(chatId, reply, parseMode: ParseMode.MarkdownV2,
                    replyMarkup: MainKeyboard(isAdmin: true));
                return;
            }
            if (action == "remove")
            {
                bool removed = await _data.RemovePhoneAsync(text);
                string reply = removed
                    ? $"🗑️ Номер *{Escape(text)}* видалено\\."
                    : $"⚠️ Номер *{Escape(text)}* не знайдено в базі\\.";
                await _bot.SendMessage(chatId, reply, parseMode: ParseMode.MarkdownV2,
                    replyMarkup: MainKeyboard(isAdmin: true));
                return;
            }
        }

        if (text is "/start" or "/menu")
        {
            bool isAdmin = userId == _adminId;
            await _bot.SendMessage(chatId,
                "👋 Привіт\\! Натисни *Номер*, щоб отримати номер телефону\\.\n📋 Ліміт: *2 рази / 24 год*",
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: MainKeyboard(isAdmin));
        }
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

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

            case "settings":
                if (!isAdmin)
                {
                    await _bot.SendMessage(chatId, "🚫 Доступ заборонено\\.", parseMode: ParseMode.MarkdownV2);
                    return;
                }
                await _bot.SendMessage(chatId, "⚙️ *Налаштування*\nОберіть дію:",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: SettingsKeyboard());
                break;

            case "settings_add":
                if (!isAdmin) return;
                _pendingAction[userId] = "add";
                await _bot.SendMessage(chatId,
                    "📝 Введи номер телефону, який хочеш додати:\n_Формат: \\+380501234567_",
                    parseMode: ParseMode.MarkdownV2,
                    replyMarkup: new ForceReplyMarkup());
                break;

            case "settings_remove":
                if (!isAdmin) return;
                var phones = _data.GetPhones();
                if (phones.Count == 0)
                {
                    await _bot.SendMessage(chatId, "📭 База номерів порожня\\.",
                        parseMode: ParseMode.MarkdownV2);
                    return;
                }
                // Show list as inline buttons
                var rows = phones.Select(p =>
                    new[] { InlineKeyboardButton.WithCallbackData(p.Number, $"delete_{p.Number}") }
                ).ToList();
                rows.Add([InlineKeyboardButton.WithCallbackData("◀ Назад", "back")]);
                await _bot.SendMessage(chatId, "🗑️ Обери номер для видалення:",
                    replyMarkup: new InlineKeyboardMarkup(rows));
                break;

            case "back":
                await _bot.SendMessage(chatId, "🏠 Головне меню",
                    replyMarkup: MainKeyboard(isAdmin));
                break;

            default:
                if (cb.Data?.StartsWith("delete_") == true)
                {
                    if (!isAdmin) return;
                    string num = cb.Data["delete_".Length..];
                    bool removed = await _data.RemovePhoneAsync(num);
                    string reply = removed
                        ? $"🗑️ Номер *{Escape(num)}* видалено\\."
                        : $"⚠️ Номер *{Escape(num)}* не знайдено\\.";
                    await _bot.SendMessage(chatId, reply, parseMode: ParseMode.MarkdownV2,
                        replyMarkup: MainKeyboard(isAdmin: true));
                }
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
                ? "😕 База номерів порожня\\. Зверніться до адміністратора\\."
                : "⏳ Ти вже отримав свій ліміт номерів сьогодні\\.\n_Повертайся завтра\\!_";
            await _bot.SendMessage(chatId, msg, parseMode: ParseMode.MarkdownV2);
            return;
        }

        string remainingText = remaining == 0
            ? "❌ Ліміт вичерпано на сьогодні"
            : $"🔄 Ще {remaining} раз сьогодні";

        // Phone number as code block — tap to copy on mobile
        string reply = $"📞 Твій номер:\n\n`{Escape(number)}`\n\n_{remainingText}_";
        await _bot.SendMessage(chatId, reply, parseMode: ParseMode.MarkdownV2);
    }

    // ── Keyboards ─────────────────────────────────────────────────────────────

    private static InlineKeyboardMarkup MainKeyboard(bool isAdmin)
    {
        var row = new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("📋 Номер", "get_number")
        };
        if (isAdmin)
            row.Add(InlineKeyboardButton.WithCallbackData("⚙️ Налаштування", "settings"));

        return new InlineKeyboardMarkup([row]);
    }

    private static InlineKeyboardMarkup SettingsKeyboard() =>
        new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("➕ Додати номер",  "settings_add") },
            new[] { InlineKeyboardButton.WithCallbackData("➖ Видалити номер", "settings_remove") },
            new[] { InlineKeyboardButton.WithCallbackData("◀ Назад",         "back") }
        });

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Escape special chars for MarkdownV2
    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("_", "\\_").Replace("*", "\\*")
         .Replace("[", "\\[").Replace("]", "\\]").Replace("(", "\\(")
         .Replace(")", "\\)").Replace("~", "\\~").Replace("`", "\\`")
         .Replace(">", "\\>").Replace("#", "\\#").Replace("+", "\\+")
         .Replace("-", "\\-").Replace("=", "\\=").Replace("|", "\\|")
         .Replace("{", "\\{").Replace("}", "\\}").Replace(".", "\\.")
         .Replace("!", "\\!");
}
