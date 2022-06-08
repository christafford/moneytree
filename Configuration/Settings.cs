using Microsoft.Extensions.Configuration;

namespace CStafford.Moneytree.Configuration
{
    public static class Settings
    {
        private static Lazy<IConfigurationRoot> _config = new Lazy<IConfigurationRoot>(() =>
            new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build());

        public static string GetApiKey() => _config.Value["Binance:ApiKey"];
        public static string GetApiSecret() => _config.Value["Binance:ApiKeySecret"];
    }
}