using PhoneBot;

namespace NumberZavr;

public class DataService
{
    private const string PhonesFile = "data.txt";

    private List<string> _phones = new();

    private int _index;
    private int _usage;

    private readonly object _lock = new();

    private readonly GitHubStateService _github;

    private string _lastSha = "";

    private BotState _cache = new();
    private bool _dirty = false;

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
            _cache.ActiveUsers ??= new();
            _lastSha = sha;

            Console.WriteLine($"[STATE] Loaded from GitHub: index={_index}, users={_cache.ActiveUsers.Count}, phones tracked={_cache.PhoneLastUsed.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STATE] Failed to load from GitHub: {ex.Message}");
            _index = 0;
            _usage = 0;
            _cache = new BotState();
        }
    }

    public async Task<string?> GetPhoneAsync()
    {
        string? phone = null;

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
                    _cache.CurrentPhoneIndex = _index;
                    _cache.CurrentPhoneUsage = 0;
                    _dirty = true;
                    break;
                }
            }
        }

        if (phone != null)
            await SaveStateAsync();

        return phone;
    }

    public async Task ResetStateAsync()
    {
        lock (_lock)
        {
            _cache.ActiveUsers ??= new();

            _index = 0;
            _usage = 0;
            _cache.CurrentPhoneIndex = 0;
            _cache.CurrentPhoneUsage = 0;
            _cache.PhoneLastUsed = new Dictionary<string, DateTime>();
            _dirty = true;
        }

        // Сброс — важная операция, сохраняем немедленно
        await SaveStateAsync();
    }

    public List<long> GetActiveUsers()
    {
        lock (_lock)
        {
            _cache.ActiveUsers ??= new();
            return _cache.ActiveUsers.ToList();
        }
    }

    public async Task AddUserAsync(long userId)
    {
        bool isNew;
        lock (_lock)
        {
            _cache.ActiveUsers ??= new();
            isNew = _cache.ActiveUsers.Add(userId);
            if (isNew)
                _dirty = true;
        }

        if (isNew)
            await SaveStateAsync();
    }

    public async Task SaveStateAsync()
    {
        BotState snapshot;
        bool needsSave;

        lock (_lock)
        {
            needsSave = _dirty;
            if (!needsSave)
                return;

            // Снимаем копию состояния
            snapshot = new BotState
            {
                CurrentPhoneIndex = _cache.CurrentPhoneIndex,
                CurrentPhoneUsage = _cache.CurrentPhoneUsage,
                PhoneLastUsed = new Dictionary<string, DateTime>(_cache.PhoneLastUsed ?? new()),
                ActiveUsers = new HashSet<long>(_cache.ActiveUsers ?? new())
            };
        }

        try
        {
            var (latest, sha) = await _github.GetAsync();
            _lastSha = sha;

            var ok = await _github.TrySaveAsync(snapshot, sha);

            if (ok)
            {
                lock (_lock)
                {
                    _dirty = false;
                }
                Console.WriteLine("[STATE] Saved to GitHub");
            }
            else
            {
                Console.WriteLine("[GITHUB] Save failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[GITHUB ERROR] " + ex.Message);
        }
    }
}