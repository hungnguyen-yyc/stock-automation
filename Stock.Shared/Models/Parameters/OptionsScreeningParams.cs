namespace Stock.Shared.Models.Parameters;

public class OptionsScreeningParams
{
    public int MinVolume { get; set; }
    public int? MaxVolume { get; set; }
    public int MinOpenInterest { get; set; }
    public int? MaxOpenInterest { get; set; }
    public int? MinDelta { get; set; }
    public int? MaxDelta { get; set; }
    public int MinExpirationDays { get; set; }
    public int? MaxExpirationDays { get; set; }
    
    public string Fields => "openInterest,volumeOpenInterestRatio,volume,delta";

    public static OptionsScreeningParams Default => new OptionsScreeningParams
    {
        MinVolume = 10000,
        MinOpenInterest = 10000,
        MinExpirationDays = 10
    };
    
    public string ToQueryString()
    {
        var queryString = $"?instrumentType=stocks&optionType=both&minVolume={MinVolume}&minOpenInterest={MinOpenInterest}&minExpirationDays={MinExpirationDays}&fields={Fields}";
        if (MaxVolume is > 0)
        {
            queryString += $"&maxVolume={MaxVolume}";
        }
        if (MaxOpenInterest is > 0)
        {
            queryString += $"&maxOpenInterest={MaxOpenInterest}";
        }
        if (MinDelta is > 0)
        {
            queryString += $"&minDelta={MinDelta}";
        }
        if (MaxDelta is > 0)
        {
            queryString += $"&maxDelta={MaxDelta}";
        }
        if (MaxExpirationDays is > 0)
        {
            queryString += $"&maxExpirationDays={MaxExpirationDays}";
        }
        return queryString;
    }
}