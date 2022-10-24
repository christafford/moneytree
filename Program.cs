using CStafford.MoneyTree;
using CStafford.MoneyTree.Application;
using CStafford.MoneyTree.Configuration;
using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args.Any(x => x == "--port"))
{
    await PortFromOld.Port();
    return;
}

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddYamlFile("appsettings.yaml")
    .AddEnvironmentVariables()
    .Build();

var builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services
            .Configure<Settings>(config.GetSection("MoneyTree"))
            .AddDbContext<MoneyTreeDbContext>(options => options.UseMySql(
                Constants.ConnectionString,
                ServerVersion.AutoDetect(Constants.ConnectionString),
                mySqlOptions =>
                {
                    mySqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(1),
                            errorNumbersToAdd: null)
                        .EnableStringComparisonTranslations(true);
                }))
            .AddAutoMapper(typeof(Program))
            .AddSingleton<BinanceApiService>()
            .AddSingleton<DownloadTicks>()
            .AddSingleton<Computer>()
            .AddSingleton<Simulator>()
            .AddSingleton<TradeForReal>()
            .AddLogging(x =>
            {
                x.AddConsole();
                x.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });
    })
    .UseConsoleLifetime();

var host = builder.Build();

using var scope = host.Services.CreateAsyncScope();

if (args.Any(x => x == "--download"))
{
    await scope.ServiceProvider.GetService<DownloadTicks>().Run();
}
if (args.Any(x => x == "--simulator"))
{
    await scope.ServiceProvider.GetService<Simulator>().Run();
}
else if (args.Any(x => x == "--real"))
{
    scope.ServiceProvider.GetService<TradeForReal>().Run();
}