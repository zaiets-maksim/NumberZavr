using Telegram.Bot;

namespace PhoneBot;

public class DataService
{
    private readonly string _numbersUrl;
    private readonly ITelegramBotClient _bot;
    private readonly long _chatId;
    private readonly int _messageId;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int DailyLimit = 2;

    private List<string> _phones = new();
    private readonly Dictionary<long, UserUsage> _usages = new();

    public DataService(IConfiguration config, ITelegramBotClient bot)
    {
        _bot = bot;
    
        // Перевірка на null з детальним повідомленням
        var chatStr = config["StateChatId"] ?? throw new InvalidOperationException("StateChatId is NOT set in environment variables!");
        var msgStr = config["StateMessageId"] ?? throw new InvalidOperationException("StateMessageId is NOT set in environment variables!");

        _chatId = long.Parse(chatStr);
        _messageId = int.Parse(msgStr);
    
        _numbersUrl = config["NumbersUrl"] ?? throw new InvalidOperationException("NumbersUrl is NOT set!");
    }

    public async Task InitAsync() => await LoadPhonesAsync();

    public async Task LoadPhonesAsync()
    {
        try 
        {
            var response = await _httpClient.GetAsync(_numbersUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Помилка завантаження бази: {response.StatusCode}");
                return; // Не падаємо, просто логуємо
            }
            var content = await response.Content.ReadAsStringAsync();
            // ... твій код парсингу ...
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критична помилка при завантаженні: {ex.Message}");
        }
    }

    public async Task<(string? number, int remaining, bool limitReached)> TryIssuePhoneAsync(long userId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_phones.Count == 0) return (null, 0, false);
            var today = DateTime.UtcNow.Date;
            if (!_usages.TryGetValue(userId, out var usage)) usage = new UserUsage { CountToday = 0, LastResetDate = today };
            if (usage.LastResetDate < today) { usage.CountToday = 0; usage.LastResetDate = today; }

            if (usage.CountToday >= DailyLimit) return (null, 0, true);

            var number = _phones[(int)(userId % _phones.Count)];
            usage.CountToday++;
            _usages[userId] = usage;

            await _bot.EditMessageText(_chatId, _messageId, $"📊 База: {_phones.Count} номерів\n🕒 Останнє оновлення: {DateTime.Now:HH:mm}");
            return (number, DailyLimit - usage.CountToday, false);
        }
        finally { _lock.Release(); }
    }
}