using CStafford.Moneytree.Application;
using CStafford.Moneytree.Configuration;
using CStafford.Moneytree.Infrastructure;
using CStafford.Moneytree.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddYamlFile("appsettings.yaml")
    .AddEnvironmentVariables()
    .Build();

var password = config.GetValue<string>("MoneyTree:DbPassword");
var userId = config.GetValue<string>("MoneyTree:DbUsername");
var server = config.GetValue<string>("MoneyTree:DbServer");
var port = config.GetValue<string>("MoneyTree:DbPort");

var connectionString = $"Server={server};Port={port};Database=MoneyTree;User={userId};Password={password};SSL Mode=None;AllowPublicKeyRetrieval=True;default command timeout=0;";

var builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services
            .Configure<Settings>(config.GetSection("MoneyTree"))
            .AddDbContext<MoneyTreeDbContext>(options => options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
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
            .AddScoped<BinanceApiService>()
            .AddScoped<DownloadTicks>()
            .AddLogging(x => x.AddConsole());
    })
    .UseConsoleLifetime();

var host = builder.Build();

using var scope = host.Services.CreateAsyncScope();
await scope.ServiceProvider.GetService<DownloadTicks>().Run();

await host.RunAsync();