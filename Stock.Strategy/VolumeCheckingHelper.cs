using Skender.Stock.Indicators;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;

namespace Stock.Strategies;

public class VolumeCheckingHelper
{
    public bool CheckHeatmapVolume(List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
    {
        // TODO: update params for heatmap volume
        var hmVolumes = ascSortedByDatePrice.GetHeatmapVolume(21, 21);
        var hmvThresholdStatus = hmVolumes.Last().ThresholdStatus;
        var hmVolumeCheck = hmvThresholdStatus != HeatmapVolumeThresholdStatus.Low
                            && hmvThresholdStatus != HeatmapVolumeThresholdStatus.Normal;
        return hmVolumeCheck;
    }
    
    public bool CheckWma921Volume(List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
    {
        var wma9 = ascSortedByDatePrice.GetWma(9);
        var wma21 = ascSortedByDatePrice.GetWma(21);
        var wma9Last = wma9.Last();
        var wma21Last = wma21.Last();
        var wmaCheck = wma9Last.Wma > wma21Last.Wma;
        return wmaCheck;
    }
}