using CStafford.Moneytree.Services;
using Microsoft.Extensions.Logging;

namespace CStafford.Moneytree.Application
{
    public class DownloadTicks
    {
        private BinanceApiService _api;
        private ILogger<DownloadTicks> _logger;
        
        public DownloadTicks(BinanceApiService api, ILogger<DownloadTicks> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task Run()
        {
            var ticks = await _api.GetTicks("BTCUSD", DateTime.Now.Subtract(TimeSpan.FromDays(1)));
            _logger.LogInformation($"Got {ticks.Count()} ticks - first close: {ticks.First().ClosePrice}");
        }
    }
}