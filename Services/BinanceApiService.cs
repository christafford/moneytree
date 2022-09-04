using AutoMapper;
using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CStafford.Moneytree.Configuration;
using CStafford.Moneytree.Models;
using Microsoft.Extensions.Options;

namespace CStafford.Moneytree.Services
{
    public class BinanceApiService
    {
        private readonly BinanceClient _client;
        private readonly IMapper _mapper;
        private readonly Settings _config;

        public BinanceApiService(IOptions<Settings> config, IMapper mapper)
        {
            _config = config.Value;
            _mapper = mapper;
            _client = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(_config.BinanceAPIKey, _config.BinanceAPISecret),
                SpotApiOptions = new BinanceApiClientOptions
                {
                    BaseAddress = "http://api.binance.us",
                    RateLimitingBehaviour = RateLimitingBehaviour.Wait
                }
            });
        }

        public async Task<IEnumerable<Models.Tick>> GetTicks(Symbol symbol, DateTime startTime)
        {
            var klinesResponse = await _client.SpotApi.CommonSpotClient.GetKlinesAsync(
                symbol.Name,
                TimeSpan.FromMinutes(1),
                startTime: startTime,
                limit: 1000
            );

            if (klinesResponse.Data == null)
            {
                throw new Exception(klinesResponse.Error?.Message);
            }

            return klinesResponse.Data.Select(_mapper.Map<Models.Tick>);
        }

        public async Task<IEnumerable<Models.Symbol>> GetSymbols()
        {
            var symbolsResponse = await _client.SpotApi.CommonSpotClient.GetSymbolsAsync();

            if (symbolsResponse.Data == null)
            {
                throw new Exception(symbolsResponse.Error?.Message);
            }

            return symbolsResponse.Data.Select(_mapper.Map<Models.Symbol>);
        }
    }
}