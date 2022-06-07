using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var apiKey = config["Binance:ApiKey"];
var apiSecret = config["Binance:ApiKeySecret"];

var client = new BinanceClient(new BinanceClientOptions()
{
    ApiCredentials = new ApiCredentials(apiKey, apiSecret),
    SpotApiOptions = new BinanceApiClientOptions
    {
        BaseAddress = "http://api.binance.us",
        RateLimitingBehaviour = RateLimitingBehaviour.Fail
    }
});

var candlesticks = client.SpotApi.CommonSpotClient.GetKlinesAsync(
    "BTCUSD",
    TimeSpan.FromMinutes(1),
    DateTime.Now.AddDays(-2),
    DateTime.Now.AddDays(-1)).Result;

Console.WriteLine($"Found {candlesticks.Data.Count()} candlesticks (should be {60*24}");