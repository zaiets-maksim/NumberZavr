using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PhoneBot;

public class BotHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly DataService _data;

    public BotHandler(ITelegramBotClient bot, DataService data)
    {
        _bot = bot;
        _data = data;
    }

    public async Task HandleMessageAsync(Message msg)
    {
        if (msg.Text == "/start")
        {
            var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Отримати номер", "get_number"));
            // Фікс: прибрано Async суфікс
            await _bot.SendMessage(msg.Chat.Id, "Бот готовий. Натисни кнопку:", replyMarkup: keyboard);
        }
    }

    public async Task HandleCallbackAsync(CallbackQuery cb)
    {
        if (cb.Data == "get_number" && cb.Message != null)
        {
            var (number, _) = await _data.TryIssuePhoneAsync(cb.From.Id);
            string text = number != null ? $"Твій номер: `{number}`" : "База порожня";
            // Фікс: прибрано Async суфікс
            await _bot.AnswerCallbackQuery(cb.Id, text, showAlert: true);
        }
    }
}