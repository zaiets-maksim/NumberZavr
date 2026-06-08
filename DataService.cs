namespace PhoneBot;

public class DataService
{
    private readonly string _numbersUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<string> _phones = new();

    private int _currentPhoneIndex;
    private int _currentPhoneUsage;

    private const int MaxUsagePerPhone = 2;

    public DataService(IConfiguration config)
    {
        _numbersUrl = config["NumbersUrl"]
                      ?? throw new Exception("NumbersUrl not configured");
    }

    public async Task InitAsync()
    {
        await LoadPhonesAsync();
    }

    public async Task LoadPhonesAsync()
    {
        using var http = new HttpClient();

        var rawText = await http.GetStringAsync(_numbersUrl);

        _phones = rawText
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanPhoneNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        _currentPhoneIndex = 0;
        _currentPhoneUsage = 0;

        Console.WriteLine($"[PhoneBot] Loaded {_phones.Count} phones");
    }

    private static string CleanPhoneNumber(string input)
    {
        return new string(input.Where(char.IsDigit).ToArray());
    }

    public async Task<string?> GetPhoneAsync()
    {
        await _lock.WaitAsync();

        try
        {
            if (_currentPhoneIndex >= _phones.Count)
                return null;

            string phone = _phones[_currentPhoneIndex];

            _currentPhoneUsage++;

            if (_currentPhoneUsage >= MaxUsagePerPhone)
            {
                _currentPhoneUsage = 0;
                _currentPhoneIndex++;
            }

            return phone;
        }
        finally
        {
            _lock.Release();
        }
    }
}