namespace Stock.Shared
{
    public enum CryptoToTradeEnum
    {
        Btc,
        Eth,
        Doge,
        Shib,
        Sol,
        Sui
    }
    
    public class CryptosToTrade
    {
        public static readonly Dictionary<CryptoToTradeEnum, string> CryptoEnumToName = new()
        {
            { CryptoToTradeEnum.Btc, "BTC" },
            { CryptoToTradeEnum.Eth, "ETH" },
            { CryptoToTradeEnum.Doge, "DOGE" },
            { CryptoToTradeEnum.Shib, "SHIB" },
            { CryptoToTradeEnum.Sol, "SOL" },
            { CryptoToTradeEnum.Sui, "SUI" }
        };
        
        public static readonly Dictionary<string, CryptoToTradeEnum> CryptoNameToEnum = new()
        {
            { "BTC", CryptoToTradeEnum.Btc },
            { "ETH", CryptoToTradeEnum.Eth },
            { "DOGE", CryptoToTradeEnum.Doge },
            { "SHIB", CryptoToTradeEnum.Shib },
            { "SOL", CryptoToTradeEnum.Sol },
            { "SUI", CryptoToTradeEnum.Sui }
        };
        
        
        // This list is used for fetching data from Barchart
        public static readonly List<string> BarchartCryptoNames = new List<string>
        {
            "^BTCUSD",
            "^ETHUSD",
            "^DOGEUSD",
            "^SHIBUSD",
            "^SOLUSD",
            "^SUIUSDT"
        };
        
        public static readonly Dictionary<CryptoToTradeEnum, string> CryptoEnumToBarchartName = new()
        {
            { CryptoToTradeEnum.Btc, "^BTCUSD" },
            { CryptoToTradeEnum.Eth, "^ETHUSD" },
            { CryptoToTradeEnum.Doge, "^DOGEUSD" },
            { CryptoToTradeEnum.Shib, "^SHIBUSD" },
            { CryptoToTradeEnum.Sol, "^SOLUSD" },
            { CryptoToTradeEnum.Sui, "^SUIUSDT" }
        };

        public static readonly IReadOnlyDictionary<string, CryptoToTradeEnum> BARCHART_CRYPTO_MAP =
            new Dictionary<string, CryptoToTradeEnum>
            {
                { "^BTCUSD", CryptoToTradeEnum.Btc },
                { "^ETHUSD", CryptoToTradeEnum.Eth },
                { "^DOGEUSD", CryptoToTradeEnum.Doge },
                { "^SHIBUSD", CryptoToTradeEnum.Shib },
                { "^SOLUSD", CryptoToTradeEnum.Sol },
                { "^SUIUSDT", CryptoToTradeEnum.Sui }
            };
        
        public static readonly IReadOnlyDictionary<CryptoToTradeEnum, string> CryptoEnumToBinanceName = new Dictionary<CryptoToTradeEnum, string>
        {
            { CryptoToTradeEnum.Btc, "BTCUSDT" },
            { CryptoToTradeEnum.Eth, "ETHUSDT" },
            { CryptoToTradeEnum.Doge, "DOGEUSDT" },
            { CryptoToTradeEnum.Shib, "SHIBUSDT" },
            { CryptoToTradeEnum.Sol, "SOLUSDT" },
            { CryptoToTradeEnum.Sui, "SUIUSDT" }
        };
    }
}
