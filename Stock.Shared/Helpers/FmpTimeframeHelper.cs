using Stock.Shared.Models;

namespace Stock.Shared.Helpers
{
    /// <summary>
    /// fmp stands for financialmodelingprep
    /// api documentation: https://financialmodelingprep.com/developer/docs/
    /// </summary>
    public class FmpTimeframeHelper
    {
        public static string GetTimeframe(Timeframe timeframe)
        {
            switch (timeframe)
            {
                case Timeframe.Minute15:
                    return "15min";
                case Timeframe.Minute30:
                    return "30min";
                case Timeframe.Hour1:
                    return "1hour";
                case Timeframe.Hour4:
                    return "4hour";
                case Timeframe.Daily:
                    return "1day";
                default:
                    return "1day";
            }
        }
    }
}