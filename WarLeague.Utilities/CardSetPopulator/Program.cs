using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CardSetPopulator;

class Program
{
    static async Task Main(string[] args)
    {
        var appCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            appCts.Cancel();
            eventArgs.Cancel = true;
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (!appCts.IsCancellationRequested)
            {
                appCts.Cancel();
            }
        };
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        serviceCollection.AddSingleton<CardsetDownloader>();
        serviceCollection.AddSingleton<BanlistDownloader>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        try
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            if (config.GetSection("CardSet").GetValue<bool>("Download"))
            {
                var cardsetDownloader = serviceProvider.GetRequiredService<CardsetDownloader>();
                await cardsetDownloader.DownloadJson(appCts.Token);
            }
            if (config.GetSection("Banlist").GetValue<bool>("Download"))
            {
                var banlistDownloader = serviceProvider.GetRequiredService<BanlistDownloader>();
                await banlistDownloader.DownloadJson(appCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // ok bye
        }
    }
}