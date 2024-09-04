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
    
    public string ToQueryString(string instrumentType, bool eod)
    {
        var isEod = eod ? 1 : 0;
        var queryString = $"?instrumentType={instrumentType}&optionType=both&minVolume={MinVolume}&minOpenInterest={MinOpenInterest}&minDTE={MinExpirationDays}&fields={Fields}&limit={Limit}&eod={isEod}";
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