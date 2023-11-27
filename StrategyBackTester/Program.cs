using Skender.Stock.Indicators;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace StrategyBackTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var testRunner = new SwingPointBackTestRunner();
            testRunner.Run();
        }
    }
}