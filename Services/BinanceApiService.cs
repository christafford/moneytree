using AutoMapper;
using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CStafford.Moneytree.Configuration;
using CStafford.Moneytree.Models;

namespace CStafford.Moneytree.Services
{
    public class BinanceApiService
    {
        private BinanceClient _client;
        private IMapper _mapper;

        public BinanceApiService(IMapper mapper)
        {
            _mapper = mapper;
            _client = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Settings.GetApiKey(), Settings.GetApiSecret()),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = "http://api.binance.us",
                    RateLimitingBehaviour = RateLimitingBehaviour.Wait
                }
            });
        }

        public async Task<IEnumerable<Tick>> GetTicks(string symbol, DateTime startTime)
        {
            var klinesResponse = await _client.SpotApi.CommonSpotClient.GetKlinesAsync(
                symbol,
                TimeSpan.FromMinutes(1),
                startTime: startTime,
                limit: 1000
            );

            if (klinesResponse.Data == null)
            {
                throw new Exception(klinesResponse.ToString());
            }

            return klinesResponse.Data.Select(x =>
            {
                var tick = _mapper.Map<Tick>(x);
                tick.Symbol = symbol;
                return tick;
            });
        }
    }
}