using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace PhoneBot;

public class DataService
{
    private readonly HttpClient _httpClient;
    private readonly string _numbersUrl;

    public DataService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _numbersUrl = config["NumbersUrl"] ?? throw new ArgumentNullException(nameof(config), "NumbersUrl is missing!");
    }

    public async Task InitAsync()
    {
        await LoadPhonesAsync();
    }

    public async Task LoadPhonesAsync()
    {
        try 
        {
            var response = await _httpClient.GetAsync(_numbersUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Помилка завантаження бази: {response.StatusCode}");
                return;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            // Тут твоя логіка обробки (парсингу) номерів
            Console.WriteLine("Базу номерів успішно завантажено!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критична помилка при завантаженні: {ex.Message}");
        }
    }
    
    // Тут залишається метод TryIssuePhoneAsync, який у тебе вже є
    public async Task<(string? number, int remaining, bool limitReached)> TryIssuePhoneAsync(long userId)
    {
        // Твій код логіки видачі номера
        return (null, 0, false); 
    }
}