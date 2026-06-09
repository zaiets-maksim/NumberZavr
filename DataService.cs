using PhoneBot;

namespace NumberZavr;

public class DataService
{
    private const string PhonesFile = "data.txt";
    private const int MaxUsage = 1;

    private List<string> _phones = new();

    private int _index;
    private int _usage;

    private readonly object _lock = new();

    private readonly GitHubStateService _github;

    private string _lastSha = "";

    private BotState _cache = new();

    public DataService(GitHubStateService github)
    {
        _github = github;
    }

    public async Task InitAsync()
    {
        await LoadPhonesAsync();
        await LoadStateAsync();
    }

    public async Task LoadPhonesAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data.txt");
        var rawText = await File.ReadAllTextAsync(path);

        _phones = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();
        
        Console.WriteLine("BASE DIR: " + AppContext.BaseDirectory);
        Console.WriteLine("FILE EXISTS: " + File.Exists("data.txt"));
        Console.WriteLine("FILE EXISTS ABS: " + File.Exists(Path.Combine(AppContext.BaseDirectory, "data.txt")));
    }

    private async Task LoadStateAsync()
    {
        try
        {
            var (state, sha) = await _github.GetAsync();

            _index = state.CurrentPhoneIndex;
            _usage = state.CurrentPhoneUsage;

            _cache = state;
            _cache.PhoneLastUsed ??= new();
            _lastSha = sha;
        }
        catch
        {
            _index = 0;
            _usage = 0;
            _cache = new BotState();
        }
    }

    public async Task<string?> GetPhoneAsync()
    {
        string? phone = null;
        BotState newState;

        lock (_lock)
        {
            _cache.PhoneLastUsed ??= new();

            if (_phones.Count == 0)
                return null;

            int startIndex = _index;
            for (int i = 0; i < _phones.Count; i++)
            {
                int checkIndex = (startIndex + i) % _phones.Count;
                var candidate = _phones[checkIndex];

                if (!_cache.PhoneLastUsed.TryGetValue(candidate, out var lastUsed) ||
                    DateTime.UtcNow - lastUsed >= TimeSpan.FromHours(24))
                {
                    phone = candidate;
                    _index = (checkIndex + 1) % _phones.Count;
                    _cache.PhoneLastUsed[candidate] = DateTime.UtcNow;
                    break;
                }
            }

            newState = new BotState
            {
                CurrentPhoneIndex = _index,
                CurrentPhoneUsage = 0,
                PhoneLastUsed = _cache.PhoneLastUsed
            };
        }

        if (phone == null)
            return null;

        try
        {
            var (latest, sha) = await _github.GetAsync();
            _lastSha = sha;

            latest.PhoneLastUsed ??= new();

            foreach (var kvp in latest.PhoneLastUsed)
            {
                if (!newState.PhoneLastUsed.TryGetValue(kvp.Key, out var localTime) || kvp.Value > localTime)
                {
                    if (kvp.Key != phone)
                    {
                        newState.PhoneLastUsed[kvp.Key] = kvp.Value;
                    }
                }
            }

            var ok = await _github.TrySaveAsync(newState, sha);

            if (!ok)
                Console.WriteLine("[GITHUB] Save failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[GITHUB ERROR] " + ex.Message);
        }

        return phone;
    }

    public async Task ResetStateAsync()
    {
        var newState = new BotState
        {
            CurrentPhoneIndex = 0,
            CurrentPhoneUsage = 0,
            PhoneLastUsed = new Dictionary<string, DateTime>()
        };

        lock (_lock)
        {
            _index = 0;
            _usage = 0;
            _cache = newState;
        }

        try
        {
            var (latest, sha) = await _github.GetAsync();
            _lastSha = sha;

            var ok = await _github.TrySaveAsync(newState, sha);
            if (!ok)
                Console.WriteLine("[GITHUB] Reset failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[GITHUB ERROR] " + ex.Message);
        }
    }
}