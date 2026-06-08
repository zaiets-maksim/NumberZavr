using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PhoneBot;

public class BotHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly DataService _data;

    public BotHandler(ITelegramBotClient bot, DataService data) { _bot = bot; _data = data; }

    public async Task HandleCallbackAsync(CallbackQuery cb)
    {
        if (cb.Message is null) return;
        await _bot.AnswerCallbackQuery(cb.Id);

        if (cb.Data == "get_number")
        {
            var (number, remaining, limitReached) = await _data.TryIssuePhoneAsync(cb.From.Id);
            
            string response = limitReached 
                ? "🚫 *Ліміт вичерпано!*\nТи вже отримав 2 номери на сьогодні."
                : (number == null ? "😕 *База порожня.*" : $"📞 *Твій номер:* `{number}`\n🔄 *Ще {remaining} разів сьогодні*");
                
            await _bot.SendMessage(cb.Message.Chat.Id, Escape(response), parseMode: ParseMode.MarkdownV2);
        }
    }

    private static string Escape(string s) => s.Replace(".", "\\.").Replace("-", "\\-").Replace("!", "\\!").Replace("*", "\\*").Replace("`", "\\`");
}