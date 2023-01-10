using Newtonsoft.Json.Linq;
using StockSignalScanner;
using StockSignalScanner.Models;

namespace TestProject
{
    public class UnitTest1
    {
        // [Fact]
        public void TestGetStochastic()
        {
            //var file = File.ReadAllText(@"C:\Users\hnguyen\source\repos\StockSignalScanner\StockSignalScanner\testdata.json");
            //var stocks = JArray.Parse(file).ToObject<List<HistoricalPrice>>().OrderBy(p => p.Date).ToList();

            //// Act
            //var result = Program.GetMACD(stocks, 12, 26, 9);

            //// Assert
            //Assert.Equal(-3.67m, Math.Round(result.macdValues.Last(), 2));
            //Assert.Equal(-2.57m, Math.Round(result.signalValues.Last(), 2));
        }

        [Fact]
        public void TestCompare()
        {
            var olds = Directory.GetFiles(@"C:\Users\hnguyen\Documents\stock-scan-logs\2023-01-06 - Copy");
            var news = Directory.GetFiles(@"C:\Users\hnguyen\Documents\stock-scan-logs\2023-01-06");

            for (int i = 0; i < olds.Length; i++)
            {
                var oldText = File.ReadAllText(olds[i]);
                var newText = File.ReadAllText(news[i]);
                Assert.Equal(oldText, newText);
            }

            //var file = File.ReadAllText(@"C:\Users\hnguyen\source\repos\StockSignalScanner\StockSignalScanner\testdata.json");
            //var stocks = JArray.Parse(file).ToObject<List<HistoricalPrice>>().OrderBy(p => p.Date).ToList();

            //// Act
            //var result = Program.GetMACD(stocks, 12, 26, 9);

            //// Assert
            //Assert.Equal(-3.67m, Math.Round(result.macdValues.Last(), 2));
            //Assert.Equal(-2.57m, Math.Round(result.signalValues.Last(), 2));
        }
    }
}