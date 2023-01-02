using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class CandlestickPatternDetector
    {
        private static bool IsDoji(List<IPrice> prices)
        {
            // Doji is a candlestick pattern with a small body and long wicks on both sides
            // The body of the doji should be very small compared to the wicks
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Max(Math.Abs(prices[i].High - prices[i].Close), Math.Abs(prices[i].High - prices[i].Open));
                if (bodySize / wickSize < 0.1m)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsDragonflyDoji(List<IPrice> prices)
        {
            // Dragonfly doji is a candlestick pattern with a small body at the top of the price range
            // The open and close prices are equal and the wicks on both sides are long
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Max(Math.Abs(prices[i].High - prices[i].Close), Math.Abs(prices[i].Low - prices[i].Close));
                if (bodySize < 0.1m && prices[i].Close == prices[i].Open && wickSize > bodySize)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsGravestoneDoji(List<IPrice> prices)
        {
            // Gravestone doji is a candlestick pattern with a small body at the bottom of the price range
            // The open and close prices are equal and the wicks on both sides are long
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Max(Math.Abs(prices[i].High - prices[i].Open), Math.Abs(prices[i].Low - prices[i].Open));
                if (bodySize < 0.1m && prices[i].Close == prices[i].Open && wickSize > bodySize)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsHammer(List<IPrice> prices)
        {
            // Hammer is a candlestick pattern with a small body and a long lower wick
            // The open and close prices can be either above or below the body
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Abs(prices[i].Low - Math.Min(prices[i].Close, prices[i].Open));
                if (bodySize < 0.1m && wickSize > bodySize)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInverseHammer(List<IPrice> prices)
        {
            // Inverse hammer is a candlestick pattern with a small body and a long upper wick
            // The open and close prices can be either above or below the body
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Abs(prices[i].High - Math.Max(prices[i].Close, prices[i].Open));
                if (bodySize < 0.1m && wickSize > bodySize)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsShootingStar(List<IPrice> prices)
        {
            // Shooting star is a candlestick pattern with a small body and a long upper wick
            // The open and close prices are below the body
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Abs(prices[i].High - prices[i].Open);
                if (bodySize < 0.1m && wickSize > bodySize && prices[i].Close < prices[i].Open)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsMorningStar(List<IPrice> prices)
        {
            // Morning star is a candlestick pattern with a small body and a long lower wick
            // The open and close prices are above the body
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Abs(prices[i].Low - prices[i].Open);
                if (bodySize < 0.1m && wickSize > bodySize && prices[i].Close > prices[i].Open)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsEveningStar(List<IPrice> prices)
        {
            // Evening star is a candlestick pattern with a small body and a long upper wick
            // The open and close prices are below the body
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Abs(prices[i].High - prices[i].Close);
                if (bodySize < 0.1m && wickSize > bodySize && prices[i].Close < prices[i].Open)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsThreeWhiteSoldiers(List<IPrice> prices)
        {
            // Three white soldiers is a candlestick pattern with three long white candles in a row
            // The open price of each candle is within the body of the previous candle
            // The close price of each candle is at the high of the day
            for (int i = 0; i < prices.Count - 2; i++)
            {
                bool isThreeWhiteSoldiers = true;
                for (int j = 0; j < 3; j++)
                {
                    if (prices[i + j].Close < prices[i + j].Open || prices[i + j].Open > prices[i + j + 1].Close || prices[i + j].Close < prices[i + j + 1].Open)
                    {
                        isThreeWhiteSoldiers = false;
                        break;
                    }
                }
                if (isThreeWhiteSoldiers)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsThreeBlackCrows(List<IPrice> prices)
        {
            // Three black crows is a candlestick pattern with three long black candles in a row
            // The open price of each candle is within the body of the previous candle
            // The close price of each candle is at the low of the day
            for (int i = 0; i < prices.Count - 2; i++)
            {
                bool isThreeBlackCrows = true;
                for (int j = 0; j < 3; j++)
                {
                    if (prices[i + j].Close > prices[i + j].Open || prices[i + j].Open < prices[i + j + 1].Close || prices[i + j].Close > prices[i + j + 1].Open)
                    {
                        isThreeBlackCrows = false;
                        break;
                    }
                }
                if (isThreeBlackCrows)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBullishEngulfing(List<IPrice> prices)
        {
            // Bullish engulfing is a candlestick pattern with a small black candle followed by a large white candle
            // The open price of the white candle is within the body of the black candle
            // The close price of the white candle is at the high of the day
            for (int i = 0; i < prices.Count - 1; i++)
            {
                decimal bodySize1 = Math.Abs(prices[i].Close - prices[i].Open);
                decimal bodySize2 = Math.Abs(prices[i + 1].Close - prices[i + 1].Open);
                if (prices[i].Close < prices[i].Open && prices[i + 1].Close > prices[i + 1].Open && prices[i + 1].Open < prices[i].High && prices[i + 1].Close > prices[i].Close && bodySize1 < bodySize2)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBearishEngulfing(List<IPrice> prices)
        {
            // Bearish engulfing is a candlestick pattern with a small white candle followed by a large black candle
            // The open price of the black candle is within the body of the white candle
            // The close price of the black candle is at the low of the day
            for (int i = 0; i < prices.Count - 1; i++)
            {
                decimal bodySize1 = Math.Abs(prices[i].Close - prices[i].Open);
                decimal bodySize2 = Math.Abs(prices[i + 1].Close - prices[i + 1].Open);
                if (prices[i].Close < prices[i].Open && prices[i + 1].Close > prices[i + 1].Open && prices[i + 1].Open < prices[i].High && prices[i + 1].Close < prices[i].Close && bodySize1 < bodySize2)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPiercingLine(List<IPrice> prices)
        {
            // Piercing line is a candlestick pattern with a long black candle followed by a white candle
            // The open price of the white candle is below the midpoint of the body of the black candle
            // The close price of the white candle is above the midpoint of the body of the black candle
            for (int i = 0; i < prices.Count - 1; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal midpoint = prices[i].Close - (bodySize / 2);
                if (prices[i].Close < prices[i].Open && prices[i + 1].Close > prices[i + 1].Open && prices[i + 1].Open < midpoint && prices[i + 1].Close > midpoint)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsHangingMan(List<IPrice> prices)
        {
            // Hanging man is a candlestick pattern with a small black candle and a long wick on the bottom
            // The open and close prices of the candle are at the high of the day
            // The low price of the candle is significantly lower than the high price
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                if (prices[i].Close < prices[i].Open && prices[i].Low < prices[i].Close - (bodySize * 2))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsDarkCloudCover(List<IPrice> prices)
        {
            // Dark cloud cover is a candlestick pattern with a long white candle followed by a black candle
            // The open price of the black candle is above the high of the white candle
            // The close price of the black candle is within the body of the white candle
            for (int i = 0; i < prices.Count - 1; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                if (prices[i].Close > prices[i].Open && prices[i + 1].Close < prices[i + 1].Open && prices[i + 1].Open > prices[i].High && prices[i + 1].Close < prices[i].Close + bodySize)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSpinningTop(List<IPrice> prices)
        {
            // Spinning top is a candlestick pattern with a small body and long wicks on both ends
            // The open, close, and high prices of the candle are within a small range
            // The low price of the candle is significantly lower or higher than the other prices
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal range = Math.Abs(prices[i].High - prices[i].Low);
                if (range > (bodySize * 2) && bodySize < (range / 2))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsRisingThreeMethods(List<IPrice> prices)
        {
            // Rising three methods is a candlestick pattern with a long white candle followed by three black candles followed by a long white candle
            // The close price of each candle is at the high of the day
            for (int i = 0; i < prices.Count - 4; i++)
            {
                bool isRisingThreeMethods = true;
                for (int j = 0; j < 5; j++)
                {
                    if (j == 0 && prices[i + j].Close < prices[i + j].Open ||
                        j == 1 && prices[i + j].Close > prices[i + j].Open ||
                        j == 2 && prices[i + j].Close > prices[i + j].Open ||
                        j == 3 && prices[i + j].Close > prices[i + j].Open ||
                        j == 4 && prices[i + j].Close < prices[i + j].Open)
                    {
                        isRisingThreeMethods = false;
                        break;
                    }
                }
                if (isRisingThreeMethods)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsFallingThreeMethods(List<IPrice> prices)
        {
            // Falling three methods is a candlestick pattern with a long black candle followed by three white candles followed by a long black candle
            // The close price of each candle is at the low of the day
            for (int i = 0; i < prices.Count - 4; i++)
            {
                bool isFallingThreeMethods = true;
                for (int j = 0; j < 5; j++)
                {
                    if (j == 0 && prices[i + j].Close > prices[i + j].Open ||
                        j == 1 && prices[i + j].Close < prices[i + j].Open ||
                        j == 2 && prices[i + j].Close < prices[i + j].Open ||
                        j == 3 && prices[i + j].Close < prices[i + j].Open ||
                        j == 4 && prices[i + j].Close > prices[i + j].Open)
                    {
                        isFallingThreeMethods = false;
                        break;
                    }
                }
                if (isFallingThreeMethods)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBearishHarami(List<IPrice> prices)
        {
            // Bearish harami is a candlestick pattern with a long white candle followed by a small black candle
            // The open price of the black candle is above the close price of the white candle
            // The close price of the black candle is below the open price of the white candle
            for (int i = 0; i < prices.Count - 1; i++)
            {
                if (prices[i].Close > prices[i].Open &&
                    prices[i + 1].Close < prices[i + 1].Open &&
                    prices[i + 1].Close > prices[i].Open &&
                    prices[i + 1].Open < prices[i].Close)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBullishHarami(List<IPrice> prices)
        {
            // Bullish harami is a candlestick pattern with a long black candle followed by a small white candle
            // The open price of the white candle is below the close price of the black candle
            // The close price of the white candle is above the open price of the black candle
            for (int i = 0; i < prices.Count - 1; i++)
            {
                if (prices[i].Close < prices[i].Open &&
                    prices[i + 1].Close > prices[i + 1].Open &&
                    prices[i + 1].Open > prices[i].Close &&
                    prices[i + 1].Close < prices[i].Open)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBearishHaramiCross(List<IPrice> prices)
        {
            // Bearish harami cross is a candlestick pattern with a long white candle followed by a small black candle with a small body
            // The open price of the black candle is above the close price of the white candle
            // The close price of the black candle is below the open price of the white candle
            // The body of the black candle is small (close to open)
            for (int i = 0; i < prices.Count - 1; i++)
            {
                if (prices[i].Close > prices[i].Open &&
                    prices[i + 1].Open < prices[i].Close &&
                    prices[i + 1].Close > prices[i].Open &&
                    Math.Abs(prices[i + 1].Close - prices[i + 1].Open) < 0.1m)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBullishHaramiCross(List<IPrice> prices)
        {
            // Bullish harami cross is a candlestick pattern with a long black candle followed by a small white candle with a small body
            // The open price of the white candle is below the close price of the black candle
            // The close price of the white candle is above the open price of the black candle
            // The body of the white candle is small (close to open)
            for (int i = 0; i < prices.Count - 1; i++)
            {
                if (prices[i].Close < prices[i].Open &&
                    prices[i + 1].Open > prices[i].Close &&
                    prices[i + 1].Close < prices[i].Open &&
                    Math.Abs(prices[i + 1].Close - prices[i + 1].Open) < 0.1m)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
