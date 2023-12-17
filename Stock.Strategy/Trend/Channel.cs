using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies.Trend
{
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
