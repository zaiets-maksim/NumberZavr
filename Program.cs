using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NumberZavr;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: false);
    })
    .ConfigureServices((context, services) =>
    {
        var cfg = context.Configuration;

        var github = new GitHubStateService(
            cfg["GitHubOwner"],
            cfg["GitHubRepo"],
            cfg["GitHubToken"]
        );

        services.AddSingleton(github);
        services.AddSingleton<DataService>();
        services.AddHostedService<BotWorker>();
    })
    .Build();

await host.RunAsync();