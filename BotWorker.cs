using Microsoft.Extensions.Hosting;

namespace NumberZavr;

public class BotWorker : BackgroundService
{
    private readonly DataService _data;

    public BotWorker(DataService data)
    {
        _data = data;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _data.InitAsync();
        Console.WriteLine("[BOT] Started");

        while (!stoppingToken.IsCancellationRequested)
        {
            // тут твоя логика Telegram bot polling / updates
            await Task.Delay(1000, stoppingToken);
        }
    }
}