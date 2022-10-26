using AutoMapper;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CStafford.MoneyTree.Configuration;
using CStafford.MoneyTree.Models;
using Microsoft.Extensions.Options;

namespace CStafford.MoneyTree.Services
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

        public async Task<decimal> GetAsset(string coin)
        {
            var balances = await _client.SpotApi.Account.GetAccountInfoAsync();

            if (balances.Error != null)
            {
                throw new Exception(balances.Error.Message);
            }

            return balances.Data.Balances.First(x => x.Asset == coin).Available;
        }

        public async Task<(string coin, decimal available)> GetPrimary()
        {
            var balances = await _client.SpotApi.Account.GetAccountInfoAsync();

            if (balances.Error != null)
            {
                throw new Exception(balances.Error.Message);
            }

            return balances.Data.Balances
                .Where(x => x.Asset != "USD")
                .OrderByDescending(x => x.Available)
                .Select(x => (x.Asset, x.Available))
                .First();
        }

        public async Task<(decimal usdValue, decimal qtyBought)> DoBuy(string coin, decimal usdToSpend)
        {
            while (true)
            {
                var result = await _client.SpotApi.Trading.PlaceOrderAsync(
                    coin + "USD",
                    OrderSide.Buy,
                    SpotOrderType.Market,
                    quoteQuantity: usdToSpend
                );

                if (result.Error?.Message.Contains("Account has insufficient balance") ?? false)
                {
                    usdToSpend *= 0.99m;
                    Console.WriteLine($"Insufficient balance - trying again at 1% less: {usdToSpend.ToString("C")}");
                    continue;
                }
                
                if (result.Error != null)
                {
                    throw new Exception($"DoBuy: {usdToSpend.ToString("C")} for {coin + "USD"}: {result.Error.Message}");
                }

                return (usdToSpend, usdToSpend / result.Data.Price);
            }
        }

        public async Task<(decimal usdValue, decimal qtySold)> DoSell(string coin, decimal qtyToSell)
        {
            var result = await _client.SpotApi.Trading.PlaceOrderAsync(
                coin + "USD",
                OrderSide.Sell,
                SpotOrderType.Market,
                qtyToSell
            );

            if (result.Error != null)
            {
                throw new Exception($"DoSell: {qtyToSell} for {coin + "USD"}: {result.Error.Message}");
            }

            return (result.Data.Price * qtyToSell, result.Data.Quantity);
        }
    }
}