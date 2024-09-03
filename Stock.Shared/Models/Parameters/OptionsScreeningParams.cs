namespace Stock.Shared.Models.Parameters;

public class OptionsScreeningParams
{
    public static string INSTRUMENT_TYPE_STOCKS => "stocks";
    public static string INSTRUMENT_TYPE_ETF => "etfs";
    
    public int MinVolume { get; set; }
    public int? MaxVolume { get; set; }
    public int MinOpenInterest { get; set; }
    public int? MaxOpenInterest { get; set; }
    public int MinExpirationDays { get; set; }
    public int? MaxExpirationDays { get; set; }
    public int Limit { get; set; }
    
    public string Fields => "openInterest,volumeOpenInterestRatio,volume,delta";

    public static OptionsScreeningParams Default => new OptionsScreeningParams
    {
        MinVolume = 10000,
        MinOpenInterest = 10000,
        MinExpirationDays = 5,
        Limit = 500
    };
    
    public string ToQueryString(string instrumentType = "stocks")
    {
        var queryString = $"?instrumentType={instrumentType}&optionType=both&minVolume={MinVolume}&minOpenInterest={MinOpenInterest}&minDTE={MinExpirationDays}&fields={Fields}&limit={Limit}&eod=0";
        if (MaxVolume is > 0)
        {
            queryString += $"&maxVolume={MaxVolume}";
        }
        if (MaxOpenInterest is > 0)
        {
            queryString += $"&maxOpenInterest={MaxOpenInterest}";
        }
        if (MaxExpirationDays is > 0)
        {
            queryString += $"&maxDTE={MaxExpirationDays}";
        }
        return queryString;
    }
}