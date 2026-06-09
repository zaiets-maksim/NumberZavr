using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NumberZavr;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
    })
    .ConfigureServices((context, services) =>
    {
        var cfg = context.Configuration;

        var owner = cfg["GitHubOwner"];
        var repo = cfg["GitHubRepo"];

        var github = new GitHubStateService(
            owner,
            repo
        );

        services.AddSingleton(github);
        services.AddSingleton<DataService>();
        services.AddHostedService<BotWorker>();
    })
    .Build();

await host.RunAsync();