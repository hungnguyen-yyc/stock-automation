namespace StrategyBackTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunSwingPointBackTest().Wait();
        }

        static async Task RunSwingPointBackTest()
        {
            while (true)
            {
                try
                {
                    var swingbacktest = new SwingPointBackTestRunner();
                    await swingbacktest.Run();
                    await Task.Delay(TimeSpan.FromMinutes(15));
#if !DEBUG
                    if (DateTime.Now.Hour >= 14 && DateTime.Now.Minute >= 30)
                    {
                        break;
                    }
#endif
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}