using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies.Trend
{
    public enum ChannelType
    {
        Ascending,
        Descending,
        Sideways,
        Indeterminate
    }
    
    public class ChannelV2
    {
        public ChannelV2(List<Price> prices)
        {
            Prices = prices;
            
            CalculateChannel();
        }

        public ChannelType ChannelType { get; private set; }
        public List<Price> Prices { get; private set; }

        private void CalculateChannel()
        {
            var highs = Prices.Select(p => p.High).ToArray();
            var lows = Prices.Select(p => p.Low).ToArray();
            var highSlope = CalculateSlope(highs);
            var lowSlope = CalculateSlope(lows);

            ChannelType = highSlope switch
            {
                > 0 when lowSlope > 0 => ChannelType.Ascending,
                < 0 when lowSlope < 0 => ChannelType.Descending,
                0 when lowSlope == 0 => ChannelType.Sideways,
                _ => ChannelType.Indeterminate
            };
        }
        
        private decimal CalculateSlope(decimal[] values)
        {
            var n = values.Length;
            decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (var i = 0; i < n; i++)
            {
                sumX += i;
                sumY += values[i];
                sumXY += i * values[i];
                sumX2 += i * i;
            }

            var m = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return m;
        }
        
        public override string ToString()
        {
            return ChannelType.ToString() + " channel: from " + Prices.First().DateAsString + " to " + Prices.Last().DateAsString;
        }
    }
    
    public class Channel
    {

        public Channel(List<Price> prices, Tuple<Price, Price> upperTrendLine, Tuple<Price, Price> lowerTrendLine)
        {
            Prices = prices;
            UpperTrendLine = upperTrendLine;
            LowerTrendLine = lowerTrendLine;
        }

        public List<Price> Prices { get; }
        public Tuple<Price, Price> UpperTrendLine { get; }

        public Tuple<Price, Price> LowerTrendLine { get; }

        public bool IsUpChannel
        {
            get
            {
                Line upperLine, lowerLine;
                GetUpAndDownChannelLine(out upperLine, out lowerLine);

                var upUpper = upperLine.Start.Y < upperLine.End.Y;
                var upLower = lowerLine.Start.Y < lowerLine.End.Y;
                return upUpper && upLower;
            }
        }

        public bool IsDownChannel
        {
            get
            {
                Line upperLine, lowerLine;
                GetUpAndDownChannelLine(out upperLine, out lowerLine);

                var downUpper = upperLine.Start.Y > upperLine.End.Y;
                var downLower = lowerLine.Start.Y > lowerLine.End.Y;
                return downUpper && downLower;
            }
        }

        public bool IsClosingUpHigher
        {
            get
            {
                if (IsUpChannel)
                {
                    Line upperLine, lowerLine;
                    GetUpAndDownChannelLine(out upperLine, out lowerLine);

                    var upChangePercent = (upperLine.End.Y - upperLine.Start.Y) / upperLine.Start.Y;
                    var downChangePercent = (lowerLine.End.Y - lowerLine.Start.Y) / lowerLine.Start.Y;

                    return upChangePercent < downChangePercent;
                }

                return false;
            }
        }

        public bool IsClosingDownLower
        {
            get
            {
                if (IsDownChannel)
                {
                    Line upperLine, lowerLine;
                    GetUpAndDownChannelLine(out upperLine, out lowerLine);

                    var upChangePercent = (upperLine.End.Y - upperLine.Start.Y) / upperLine.Start.Y;
                    var downChangePercent = (lowerLine.End.Y - lowerLine.Start.Y) / lowerLine.Start.Y;

                    return upChangePercent > downChangePercent;
                }

                return false;
            }
        }

        public bool IsSideway => !IsUpChannel && !IsDownChannel;

        private void GetUpAndDownChannelLine(out Line upperLine, out Line lowerLine)
        {
            var highStartIndex = Prices.IndexOf(UpperTrendLine.Item1);
            var highEndIndex = Prices.IndexOf(UpperTrendLine.Item2);
            var lowStartIndex = Prices.IndexOf(LowerTrendLine.Item1);
            var lowEndIndex = Prices.IndexOf(LowerTrendLine.Item2);

            upperLine = new Line(new Point(highStartIndex, UpperTrendLine.Item1.High), new Point(highEndIndex, UpperTrendLine.Item2.High));
            lowerLine = new Line(new Point(lowStartIndex, LowerTrendLine.Item1.Low), new Point(lowEndIndex, LowerTrendLine.Item2.Low));
        }
    }
}
