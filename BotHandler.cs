using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
            await _bot.SendMessage(msg.Chat.Id, "Бот готовий. Натисни кнопку:", 
                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Отримати номер", "get_number")));
        }
    }

    public async Task HandleCallbackAsync(CallbackQuery cb)
    {
        if (cb.Data == "get_number")
        {
            var (number, _) = await _data.TryIssuePhoneAsync(cb.From.Id);
            string text = number != null ? $"Твій номер: `{number}`" : "База порожня";
            await _bot.AnswerCallbackQuery(cb.Id, text, showAlert: true);
        }
    }
}