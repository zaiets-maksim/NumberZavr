using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PhoneBot;

// Stores per-user daily limits as a JSON string in a pinned Telegram group message
public class DataService
{
    private readonly ITelegramBotClient _bot;
    private readonly long _groupId;
    private int _pinnedMessageId;
    private readonly string _numbersUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = false };

    private const int DailyLimit = 2;

    // In-memory cache of limits: userId -> (count, date)
    private Dictionary<long, UserUsage> _usages = new();
    // In-memory list of phone numbers (read from GitHub)
    private List<string> _phones = new();

    public DataService(ITelegramBotClient bot, IConfiguration config)
    {
        _bot = bot;
        _groupId = long.Parse(config["GroupId"] ?? throw new Exception("GroupId not configured"));
        _pinnedMessageId = int.Parse(config["PinnedMessageId"] ?? "0");
        _numbersUrl = config["NumbersUrl"] ?? throw new Exception("NumbersUrl not configured");
    }

    // Call once on startup
    public async Task InitAsync()
    {
        await LoadPhonesAsync();
        await LoadLimitsAsync();
    }

    // ── Phones (read-only from GitHub raw txt) ────────────────────────────────

    public async Task LoadPhonesAsync()
    {
        using var http = new HttpClient();
        var text = await http.GetStringAsync(_numbersUrl);
        _phones = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(l => l.Trim())
                      .Where(l => l.Length > 0)
                      .ToList();
    }

    public List<string> GetPhones() => _phones;

    // ── Limits (read/write via pinned Telegram message) ───────────────────────

    private async Task LoadLimitsAsync()
    {
        try
        {
            // Try to get pinned message from group
            var chat = await _bot.GetChat(_groupId);
            if (chat.PinnedMessage is { } pinned)
            {
                _pinnedMessageId = pinned.MessageId;
                var json = pinned.Text ?? "{}";
                _usages = JsonSerializer.Deserialize<Dictionary<long, UserUsage>>(json) ?? new();
                return;
            }
        }
        catch { }

        // No pinned message found — create one
        if (_pinnedMessageId == 0)
        {
            var msg = await _bot.SendMessage(_groupId, "{}");
            _pinnedMessageId = msg.MessageId;
            await _bot.PinChatMessage(_groupId, _pinnedMessageId);
        }

        _usages = new();
    }

    private async Task SaveLimitsAsync()
    {
        var json = JsonSerializer.Serialize(_usages, _opts);
        try
        {
            await _bot.EditMessageText(_groupId, _pinnedMessageId, json);
        }
        catch
        {
            // If message doesn't exist, create new pinned
            var msg = await _bot.SendMessage(_groupId, json);
            _pinnedMessageId = msg.MessageId;
            await _bot.PinChatMessage(_groupId, _pinnedMessageId);
        }
    }

    // ── Issue phone ───────────────────────────────────────────────────────────

    public async Task<(string? number, int remaining)> TryIssuePhoneAsync(long userId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_phones.Count == 0)
                return (null, 0);

            var today = DateTime.UtcNow.Date;

            if (!_usages.TryGetValue(userId, out var usage))
            {
                usage = new UserUsage { CountToday = 0, LastResetDate = today };
                _usages[userId] = usage;
            }

            if (usage.LastResetDate < today)
            {
                usage.CountToday = 0;
                usage.LastResetDate = today;
            }

            if (usage.CountToday >= DailyLimit)
                return (null, 0);

            // Round-robin by index
            int idx = (int)(userId % _phones.Count);
            var number = _phones[idx];

            usage.CountToday++;
            int remaining = DailyLimit - usage.CountToday;

            await SaveLimitsAsync();
            return (number, remaining);
        }
        finally { _lock.Release(); }
    }
}
