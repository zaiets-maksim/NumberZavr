using System.Text.Json;

namespace PhoneBot;

public class DataService
{
    private readonly string _numbersUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int DailyLimit = 2;

    private List<string> _phones = new();
    // In-memory limits — reset on restart
    private readonly Dictionary<long, UserUsage> _usages = new();

    public DataService(IConfiguration config)
    {
        _numbersUrl = config["NumbersUrl"] ?? throw new Exception("NumbersUrl not configured");
    }

    public async Task InitAsync()
    {
        await LoadPhonesAsync();
    }

    public async Task LoadPhonesAsync()
    {
        using var http = new HttpClient();
        var rawText = await http.GetStringAsync(_numbersUrl);
    
        _phones = rawText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanPhoneNumber)
            .Where(phone => !string.IsNullOrEmpty(phone))
            .Distinct() // Щоб уникнути дублікатів
            .ToList();
                     
        Console.WriteLine($"[PhoneBot] Loaded {_phones.Count} phones");
    }
    
    private string CleanPhoneNumber(string input)
    {
        return new string(input.Where(char.IsDigit).ToArray());
    }

    public List<string> GetPhones() => _phones;

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

            // Round-robin
            int idx = (int)(userId % _phones.Count);
            var number = _phones[idx];

            usage.CountToday++;
            int remaining = DailyLimit - usage.CountToday;
            return (number, remaining);
        }
        finally { _lock.Release(); }
    }
}
