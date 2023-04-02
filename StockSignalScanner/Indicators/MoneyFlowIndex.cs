using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class MoneyFlowIndex
    {
        public static List<decimal> CalculateMFI(IList<IPrice> prices, int period)
        {
            List<decimal> mfiValues = new List<decimal>();
            decimal typicalPrice = 0;
            decimal moneyFlow = 0;
            decimal positiveMoneyFlowSum = 0;
            decimal negativeMoneyFlowSum = 0;

            for (int i = 0; i < prices.Count; i++)
            {
                IPrice price = prices[i];

                // Calculate typical price
                typicalPrice = (price.High + price.Low + price.Close) / 3;

                // Calculate money flow
                moneyFlow = typicalPrice * price.Volume;

                if (i >= period)
                {
                    decimal previousTypicalPrice = (prices[i - 1].High + prices[i - 1].Low + prices[i - 1].Close) / 3;
                    decimal previousMoneyFlow = previousTypicalPrice * prices[i - 1].Volume;

                    // Calculate the sum of positive and negative money flows for the past period data points
                    positiveMoneyFlowSum = 0;
                    negativeMoneyFlowSum = 0;
                    for (int j = i - period; j < i; j++)
                    {
                        decimal jTypicalPrice = (prices[j].High + prices[j].Low + prices[j].Close) / 3;
                        decimal jMoneyFlow = jTypicalPrice * prices[j].Volume;
                        if (jTypicalPrice > previousTypicalPrice)
                        {
                            positiveMoneyFlowSum += jMoneyFlow;
                        }
                        else if (jTypicalPrice < previousTypicalPrice)
                        {
                            negativeMoneyFlowSum += jMoneyFlow;
                        }
                    }

                    if (typicalPrice > previousTypicalPrice)
                    {
                        positiveMoneyFlowSum += moneyFlow;
                    }
                    else if (typicalPrice < previousTypicalPrice)
                    {
                        negativeMoneyFlowSum += moneyFlow;
                    }

                    if (negativeMoneyFlowSum == 0m)
                    {

                        mfiValues.Add(0);
                    } 
                    else
                    {
                        // Calculate Money Flow Index (MFI)
                        decimal moneyRatio = positiveMoneyFlowSum / negativeMoneyFlowSum;
                        decimal mfi = 100 - (100 / (1 + moneyRatio));
                        mfiValues.Add(mfi);
                    }
                }
                else
                {
                    mfiValues.Add(0);
                }
            }

            return mfiValues;
        }






    }
}
