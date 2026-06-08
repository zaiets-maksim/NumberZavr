using Telegram.Bot;

namespace PhoneBot;

public class DataService
{
    private readonly ITelegramBotClient _bot;
    private readonly long _chatId;
    private readonly int _messageId;
    private List<string> _phones = new();
    private int _globalCounter = 0;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DataService(ITelegramBotClient bot, long chatId, int messageId)
    {
        _bot = bot;
        _chatId = chatId;
        _messageId = messageId;
    }

    public async Task InitializeAsync(string numbersUrl)
    {
        // 1. Завантажуємо список номерів
        using var http = new HttpClient();
        var raw = await http.GetStringAsync(numbersUrl);
        _phones = raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => new string(s.Where(char.IsDigit).ToArray()))
            .Distinct().ToList();

        // 2. Підтягуємо поточний прогрес з Telegram
        var msg = await _bot.GetMessage(_chatId, _messageId);
        if (msg.Text != null && msg.Text.StartsWith("State: "))
        {
            _globalCounter = int.Parse(msg.Text.Replace("State: ", ""));
        }
    }

    public async Task<(string? number, int remaining)> TryIssuePhoneAsync(long userId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_phones.Count == 0) return (null, 0);

            // Отримуємо номер по черзі
            string number = _phones[_globalCounter % _phones.Count];
            _globalCounter++;

            // Оновлюємо стан у повідомленні
            await _bot.EditMessageText(_chatId, _messageId, $"State: {_globalCounter}");

            return (number, 2); 
        }
        finally { _lock.Release(); }
    }
}