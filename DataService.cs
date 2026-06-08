using System.Text.Json;

namespace PhoneBot;

public class DataService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
    private BotData _data = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DataService(IConfiguration config)
    {
        _filePath = config["DataFilePath"] ?? "data.json";
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _data = new BotData();
            Save();
            return;
        }
        var json = File.ReadAllText(_filePath);
        _data = JsonSerializer.Deserialize<BotData>(json) ?? new BotData();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_data, _opts);
        File.WriteAllText(_filePath, json);
    }

    // ── Phones ──────────────────────────────────────────────────────────────

    public List<PhoneRecord> GetPhones() => _data.Phones;

    public async Task<bool> AddPhoneAsync(string number)
    {
        await _lock.WaitAsync();
        try
        {
            number = number.Trim();
            if (_data.Phones.Any(p => p.Number == number))
                return false;
            _data.Phones.Add(new PhoneRecord { Number = number });
            Save();
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> RemovePhoneAsync(string number)
    {
        await _lock.WaitAsync();
        try
        {
            number = number.Trim();
            var phone = _data.Phones.FirstOrDefault(p => p.Number == number);
            if (phone is null) return false;
            _data.Phones.Remove(phone);
            Save();
            return true;
        }
        finally { _lock.Release(); }
    }

    // ── Usage tracking ───────────────────────────────────────────────────────

    private const int DailyLimit = 2;

    /// <summary>
    /// Returns the next phone number for this user, respecting daily limit.
    /// Returns null if limit reached or no phones in DB.
    /// </summary>
    public async Task<(string? number, int remaining)> TryIssuePhoneAsync(long userId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_data.Phones.Count == 0)
                return (null, 0);

            var today = DateTime.UtcNow.Date;
            var usage = _data.UserUsages.FirstOrDefault(u => u.UserId == userId);

            if (usage is null)
            {
                usage = new UserUsage { UserId = userId, LastResetDate = today };
                _data.UserUsages.Add(usage);
            }

            // Reset counter if a new day
            if (usage.LastResetDate < today)
            {
                usage.CountToday = 0;
                usage.LastResetDate = today;
            }

            if (usage.CountToday >= DailyLimit)
                return (null, 0);

            // Round-robin: pick phone with least total issued
            var phone = _data.Phones
                .OrderBy(p => p.TotalIssued)
                .ThenBy(_ => Guid.NewGuid()) // tie-break randomly
                .First();

            phone.TotalIssued++;
            usage.CountToday++;

            int remaining = DailyLimit - usage.CountToday;
            Save();
            return (phone.Number, remaining);
        }
        finally { _lock.Release(); }
    }
}
