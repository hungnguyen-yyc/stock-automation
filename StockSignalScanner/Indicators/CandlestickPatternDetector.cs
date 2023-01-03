using StockSignalScanner.Models;

namespace StockSignalScanner.Indicators
{
    internal class CandlestickPatternDetector
    {
        public static IEnumerable<CandlestickPatternType> Detect(IList<IPrice> prices)
        {
            var res = new List<CandlestickPatternType>();
            if (IsDoji(prices))
            {
                res.Add(CandlestickPatternType.Doji);
            }
            if (IsDragonflyDoji(prices))
            {
                res.Add(CandlestickPatternType.DragonflyDoji);
            }
            if (IsGravestoneDoji(prices))
            {
                res.Add(CandlestickPatternType.GravestoneDoji);
            }
            if (IsHammer(prices))
            {
                res.Add(CandlestickPatternType.Hammer);
            }
            if (IsInvertedHammer(prices))
            {
                res.Add(CandlestickPatternType.InvertedHammer);
            }
            if (IsShootingStar(prices))
            {
                res.Add(CandlestickPatternType.ShootingStar);
            }
            if (IsMorningStar(prices))
            {
                res.Add(CandlestickPatternType.MorningStar);
            }
            if (IsEveningStar(prices))
            {
                res.Add(CandlestickPatternType.EveningStar);
            }
            if (IsThreeBlackCrows(prices))
            {
                res.Add(CandlestickPatternType.ThreeBlackCrows);
            }
            if (IsThreeWhiteSoldiers(prices))
            {
                res.Add(CandlestickPatternType.ThreeWhiteSoldiers);
            }
            if (IsBullishEngulfing(prices))
            {
                res.Add(CandlestickPatternType.BullishEngulfing);
            }
            if (IsBearishEngulfing(prices))
            {
                res.Add(CandlestickPatternType.BearishEngulfing);
            }
            if (IsPiercingLine(prices))
            {
                res.Add(CandlestickPatternType.PiercingLine);
            }
            if (IsHangingMan(prices))
            {
                res.Add(CandlestickPatternType.HangingMan);
            }
            if (IsDarkCloudCover(prices))
            {
                res.Add(CandlestickPatternType.DarkCloudCover);
            }
            if (IsSpinningTop(prices))
            {
                res.Add(CandlestickPatternType.SpinningTop);
            }
            if (IsRisingThreeMethods(prices))
            {
                res.Add(CandlestickPatternType.RisingThreeMethods);
            }
            if (IsFallingThreeMethods(prices))
            {
                res.Add(CandlestickPatternType.FallingThreeMethods);
            }
            if (IsBearishHarami(prices))
            {
                res.Add(CandlestickPatternType.BearishHarami);
            }
            if (IsBullishHarami(prices))
            {
                res.Add(CandlestickPatternType.BullishHarami);
            }
            if (IsBearishHaramiCross(prices))
            {
                res.Add(CandlestickPatternType.BearishHaramiCross);
            }
            if (IsBullishHaramiCross(prices))
            {
                res.Add(CandlestickPatternType.BullishHaramiCross);
            }
            return res;
        }

        private static bool IsDoji(IList<IPrice> prices)
        {
            // Doji is a candlestick pattern with a small body and long wicks on both sides
            // The body of the doji should be very small compared to the wicks
            for (int i = 0; i < prices.Count; i++)
            {
                decimal bodySize = Math.Abs(prices[i].Close - prices[i].Open);
                decimal wickSize = Math.Max(Math.Abs(prices[i].High - prices[i].Close), Math.Abs(prices[i].High - prices[i].Open));
                if (wickSize == 0)
                {
                    return bodySize < 0.1m;
                }
                if (bodySize / wickSize < 0.1m)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsDragonflyDoji(IList<IPrice> prices)
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

        private static bool IsGravestoneDoji(IList<IPrice> prices)
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

        private static bool IsHammer(IList<IPrice> prices)
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

        private static bool IsInvertedHammer(IList<IPrice> prices)
        {
            // An inverted hammer is a bullish candlestick pattern that consists of a small real body with a long lower shadow and little or no upper shadow
            // It is formed when the security opens, declines significantly, but then closes the day near the open again
            // An inverted hammer indicates that the security may be starting to reverse its downtrend
            for (int i = 0; i < prices.Count; i++)
            {
                // Check if the candle is a bullish candlestick with a small real body and a long lower shadow and little or no upper shadow
                if (prices[i].Close > prices[i].Open &&
                    prices[i].Close - prices[i].Open < prices[i].High - prices[i].Close &&
                    prices[i].Close - prices[i].Low > 2 * (prices[i].Close - prices[i].Open))
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsShootingStar(IList<IPrice> prices)
        {
            // A shooting star is a bearish candlestick with a long upper shadow, little or no lower shadow, and a small real body near the low of the day
            // It appears after an uptrend
            // Said differently, a shooting star is a type of candlestick that forms when a security opens, advances significantly, but then closes the day near the open again
            // For a candlestick to be considered a shooting star, the formation must appear during a price advance
            // Also, the distance between the highest price of the day and the opening price must be more than twice as large as the shooting star's body
            // There should be little to no shadow below the real body
            for (int i = 0; i < prices.Count; i++)
            {
                // Check if the candle is a bearish candlestick with a long upper shadow, little or no lower shadow, and a small real body near the low of the day
                if (prices[i].Close < prices[i].Open &&
                    prices[i].High - prices[i].Open > 2 * (prices[i].Close - prices[i].Open) &&
                    prices[i].Low - prices[i].Open > 0 &&
                    prices[i].Low - prices[i].Close < prices[i].Close - prices[i].Open)
                {
                    return true;
                }
            }
            return false;
        }



        private static bool IsMorningStar(IList<IPrice> prices)
        {
            // A morning star is a bullish candlestick pattern that consists of three candles
            // The first candle is a large bearish candle located within a defined downtrend
            // The second candle is a small bodied candle (bullish or bearish) that closes below the first red bar
            // The last candle is a large bullish candle that open above the middle candle and close near the middle of the first candle
            for (int i = 0; i < prices.Count - 2; i++)
            {
                // Check if the first candle is a large bearish candle located within a defined downtrend
                if (prices[i].Close > prices[i].Open &&
                    (prices[i + 1].Close <= prices[i + 1].Open || prices[i + 1].Close > prices[i + 1].Open) &&
                    prices[i + 2].Close > prices[i + 2].Open &&
                    prices[i + 1].Close < prices[i].Close &&
                    prices[i + 2].Open > prices[i + 1].Close)
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsEveningStar(IList<IPrice> prices)
        {
            // An evening star is a bearish candlestick pattern that consists of three candles:
            // a large white candlestick, a small-bodied candle, and a red candle
            // The first candle is a large white bullish candlestick located with an uptrend
            // The middle one is a small bodied candle(bullish or bearish) that close above the first candle
            // The last candle is a large bearish candle that open below the second candle and closes near the first candle’s center
            for (int i = 0; i < prices.Count - 2; i++)
            {
                // Check if the first candle is a large white bullish candlestick located within an uptrend
                if (prices[i].Close < prices[i].Open &&
                    (prices[i + 1].Close <= prices[i + 1].Open || prices[i + 1].Close > prices[i + 1].Open) &&
                    prices[i + 2].Close < prices[i + 2].Open &&
                    prices[i + 1].Close > prices[i].Close &&
                    prices[i + 2].Open < prices[i + 1].Close)
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsThreeWhiteSoldiers(IList<IPrice> prices)
        {
            // Three white soldiers is a bullish candlestick pattern that consists of three consecutive long-bodied white candles
            // Each candle in the pattern should open within the real body of the previous candle
            // and close higher than the previous candle's high
            for (int i = 0; i < prices.Count - 2; i++)
            {
                // Check if all three candles in the combination are white candles
                if (prices[i].Close < prices[i].Open &&
                    prices[i + 1].Close < prices[i + 1].Open &&
                    prices[i + 2].Close < prices[i + 2].Open &&
                    prices[i + 1].Open < Math.Max(prices[i].Close, prices[i].Open) &&
                    prices[i + 1].Open > Math.Min(prices[i].Close, prices[i].Open) &&
                    prices[i + 1].Close > Math.Max(prices[i].Close, prices[i].Open))
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsThreeBlackCrows(IList<IPrice> prices)
        {
            // Three black crows is a candlestick pattern with three consecutive black candles
            // Each candle in the pattern should open within the real body of the previous candle
            // and close lower than the previous candle
            for (int i = 0; i < prices.Count - 2; i++)
            {
                // Check if all three candles in the combination are black candles
                if (prices[i].Close > prices[i].Open &&
                    prices[i + 1].Close > prices[i + 1].Open &&
                    prices[i + 2].Close > prices[i + 2].Open &&
                    prices[i + 1].Open > Math.Min(prices[i].Close, prices[i].Open) &&
                    prices[i + 1].Open < Math.Max(prices[i].Close, prices[i].Open) &&
                    prices[i + 1].Close < Math.Min(prices[i].Close, prices[i].Open))
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsBullishEngulfing(IList<IPrice> prices)
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

        private static bool IsBearishEngulfing(IList<IPrice> prices)
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

        private static bool IsPiercingLine(IList<IPrice> prices)
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

        private static bool IsHangingMan(IList<IPrice> prices)
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

        private static bool IsDarkCloudCover(IList<IPrice> prices)
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

        private static bool IsSpinningTop(IList<IPrice> prices)
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

        private static bool IsRisingThreeMethods(IList<IPrice> prices)
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

        private static bool IsFallingThreeMethods(IList<IPrice> prices)
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

        private static bool IsBearishHarami(IList<IPrice> prices)
        {
            // The bearish harami indicator is charted as a long candlestick followed by a smaller body, referred to as a doji,
            // that is completely contained within the vertical range of the previous body
            // To some, a line drawn around this pattern resembles a pregnant woman
            // The word harami comes from an old Japanese word meaning pregnant
            // For a bearish harami to appear, a smaller body on the subsequent doji will close lower within the body of the previous day’s candle,
            // signaling a greater likelihood that a reversal will occur
            for (int i = 0; i < prices.Count - 1; i++)
            {
                // Check if the first candle is a long candlestick and the second candle is a smaller doji body
                // that is completely contained within the vertical range of the first candle and closes lower within the body of the first candle
                if (prices[i].Close > prices[i].Open &&
                    prices[i + 1].Close < prices[i + 1].Open &&
                    prices[i + 1].Close > Math.Min(prices[i].Close, prices[i].Open) &&
                    prices[i + 1].Close < Math.Max(prices[i].Close, prices[i].Open))
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsBullishHarami(IList<IPrice> prices)
        {
            // The bullish harami indicator is charted as a long candlestick followed by a smaller body, referred to as a doji,
            // that is completely contained within the vertical range of the previous body
            // To some, a line drawn around this pattern resembles a pregnant woman
            // The word harami comes from an old Japanese word meaning pregnant
            // For a bullish harami to appear, a smaller body on the subsequent doji will close higher within the body of the previous day’s candle,
            // signaling a greater likelihood that a reversal will occur
            for (int i = 0; i < prices.Count - 1; i++)
            {
                // Check if the first candle is a long candlestick and the second candle is a smaller doji body
                // that is completely contained within the vertical range of the first candle and closes higher within the body of the first candle
                if (prices[i].Close < prices[i].Open &&
                    prices[i + 1].Close > prices[i + 1].Open &&
                    prices[i + 1].Close > Math.Min(prices[i].Close, prices[i].Open) &&
                    prices[i + 1].Close < Math.Max(prices[i].Close, prices[i].Open))
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsBearishHaramiCross(IList<IPrice> prices)
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
                    Math.Abs(prices[i + 1].Close - prices[i + 1].Open) < 0.001m)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBullishHaramiCross(IList<IPrice> prices)
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
                    Math.Abs(prices[i + 1].Close - prices[i + 1].Open) < 0.001m)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
