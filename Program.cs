using CStafford.Moneytree.Application;
using CStafford.Moneytree.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddAutoMapper(typeof(Program));
        services.AddScoped<BinanceApiService>()
                .AddScoped<DownloadTicks>();
    })
    .Build();

using var scope = host.Services.CreateAsyncScope();
await scope.ServiceProvider.GetService<DownloadTicks>().Run();
await host.RunAsync();