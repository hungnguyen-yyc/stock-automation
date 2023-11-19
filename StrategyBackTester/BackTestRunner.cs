using Stock.Shared.Models.Parameters;
using Stock.Strategies.Parameters;
using Stock.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrategyBackTester
{
    internal class BackTestRunner
    {
        public void Run()
        {
            var parameter = new KamaSarMfiKeltnerChannelParameter
            {
                Kama14Parameter = new KamaParameter
                {
                    KamaPeriod = 14,
                    KamaFastPeriod = 5,
                    KamaSlowPeriod = 30
                },
                Kama75Parameter = new KamaParameter
                {
                    KamaPeriod = 75,
                    KamaFastPeriod = 5,
                    KamaSlowPeriod = 30
                },
                SarParameter = new SarParameter
                {
                    SarMaxAcceleration = 0.2,
                    SarAcceleration = 0.1,
                    SarInitial = 0.1
                },
                MfiParameter = new MfiParameter
                {
                    MfiPeriod = 10,
                    UpperLimit = 80,
                    LowerLimit = 20,
                    MiddleLimit = 50
                },
                KeltnerParameter = new KeltnerParameter
                {
                    KeltnerPeriod = 20,
                    KeltnerMultiplier = 2,
                    KeltnerAtrPeriod = 10
                }
            };
            var orders = KamaSarMfiKeltnerChannelStrategy.Run("AAPL", parameter);

            var result = orders.Select(x => x.ToString());
            var fileName = $"{nameof(KamaSarMfiKeltnerChannelStrategy)}-orders-{orders.FirstOrDefault()?.Ticker}-{DateTime.Now:yyyyMMdd}.txt";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.AppendAllLines(fileName, result);
        }
    }
}
