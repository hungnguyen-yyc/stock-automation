namespace StockSignalScanner.Models
{
    public class StockMeta : SymbolInfo
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }
        public string Exchange { get; set; }
    }

    public class SymbolInfo
    {
        public string Symbol { get; set; }
        public string ExchangeShortName { get; set; }
    }
}
