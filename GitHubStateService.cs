using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PhoneBot;

namespace NumberZavr;

public class GitHubStateService
{
    private readonly HttpClient _http = new();

    private readonly string _owner;
    private readonly string _repo;
    private readonly string _token;

    private const string FilePath = "state.json";

    public GitHubStateService(string owner, string repo, string token)
    {
        _owner = owner;
        _repo = repo;
        _token = token;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NumberZavrBot");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
    }

    private string Url =>
        $"https://api.github.com/repos/{_owner}/{_repo}/contents/{FilePath}";

    public async Task<(BotState state, string sha)> GetAsync()
    {
        var json = await _http.GetStringAsync(Url);

        using var doc = JsonDocument.Parse(json);

        var content = doc.RootElement.GetProperty("content").GetString();
        var sha = doc.RootElement.GetProperty("sha").GetString();

        var decoded = Encoding.UTF8.GetString(
            Convert.FromBase64String(content!.Trim())
        );

        var state = JsonSerializer.Deserialize<BotState>(decoded) ?? new BotState();

        return (state, sha!);
    }

    public async Task<bool> TrySaveAsync(BotState state, string sha, int retries = 3)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

                var payload = new
                {
                    message = "update state",
                    content = base64,
                    sha = sha
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var resp = await _http.PutAsync(Url, content);

                if (resp.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                await Task.Delay(300);
            }
        }

        return false;
    }
}