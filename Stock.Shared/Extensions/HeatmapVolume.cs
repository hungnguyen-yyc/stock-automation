using Skender.Stock.Indicators;
using Stock.Shared.Models;

namespace Stock.Shared.Extensions
{
    public enum HeatmapVolumeThresholdStatus
    {
        ExtraHigh,
        High,
        Medium,
        Normal,
        Low
    }

    public class HeatmapVolumeResult : ResultBase
    {
        public HeatmapVolumeResult(DateTime date, decimal volume, HeatmapVolumeThresholdStatus thresholdStatus)
        {
            Date = date;
            Volume = volume;
            ThresholdStatus = thresholdStatus;
        }

        public decimal Volume { get; }

        public HeatmapVolumeThresholdStatus ThresholdStatus { get; }
    }

    public static class HeatmapVolume
    {
        public static List<HeatmapVolumeResult> GetHeatmapVolume(this List<Price> list, 
            int maLength = 610, 
            int sdLength = 610, 
            double extraHighMultiplier = 4,
            double highMultiplier = 2.5,
            double mediumMultiplier = 1,
            double normalMultiplier = -0.5)
        {
            if (list.Count < maLength)
                throw new Exception("List count is less than MA length");

            if (list.Count < sdLength)
                throw new Exception("List count is less than SD length");

            var result = new List<HeatmapVolumeResult>();
            var mean = list.GetSma(CandlePart.Volume, maLength).ToArray();
            var standardDeviation = list.GetStdDev(CandlePart.Volume, sdLength).ToArray();

            for (var i = 0; i < list.Count; i++)
            {
                var price = list[i];
                var volume = (double)price.Volume;
                var meanVolume = mean[i].Sma;
                var stdVolume = standardDeviation[i].StdDev;
                var stdBar = (volume - meanVolume) / stdVolume;

                if (stdBar > extraHighMultiplier)
                    result.Add(new HeatmapVolumeResult(price.Date, price.Volume, HeatmapVolumeThresholdStatus.ExtraHigh));
                else if (stdBar > highMultiplier)
                    result.Add(new HeatmapVolumeResult(price.Date, price.Volume, HeatmapVolumeThresholdStatus.High));
                else if (stdBar > mediumMultiplier)
                    result.Add(new HeatmapVolumeResult(price.Date, price.Volume, HeatmapVolumeThresholdStatus.Medium));
                else if (stdBar > normalMultiplier)
                    result.Add(new HeatmapVolumeResult(price.Date, price.Volume, HeatmapVolumeThresholdStatus.Normal));
                else
                    result.Add(new HeatmapVolumeResult(price.Date, price.Volume, HeatmapVolumeThresholdStatus.Low));
            }

            return result;
        }
    }
}
