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

        // здесь твоя логика Telegram bot polling / webhook handler
        // пока заглушка:

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}