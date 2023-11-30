namespace StrategyBackTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var swingbacktest = new SwingPointBackTestRunner();
            swingbacktest.Run().Wait();
        }
    }
}