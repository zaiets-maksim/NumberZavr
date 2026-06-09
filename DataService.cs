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
        var raw = await File.ReadAllTextAsync(PhonesFile);

        _phones = raw
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();
    }

    private async Task LoadStateAsync()
    {
        try
        {
            var (state, sha) = await _github.GetAsync();

            _index = state.CurrentPhoneIndex;
            _usage = state.CurrentPhoneUsage;

            _cache = state;
            _lastSha = sha;
        }
        catch
        {
            _index = 0;
            _usage = 0;
        }
    }

    public async Task<string?> GetPhoneAsync()
    {
        BotState newState;

        string phone;

        lock (_lock)
        {
            if (_index >= _phones.Count)
                return null;

            phone = _phones[_index];

            _usage++;

            if (_usage >= MaxUsage)
            {
                _usage = 0;
                _index++;
            }

            newState = new BotState
            {
                CurrentPhoneIndex = _index,
                CurrentPhoneUsage = _usage
            };
        }

        // защита от дублей + race fix
        try
        {
            var (latest, sha) = await _github.GetAsync();

            // если state изменился — обновляем локально
            _lastSha = sha;

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
}