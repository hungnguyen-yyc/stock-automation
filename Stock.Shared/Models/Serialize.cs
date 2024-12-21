using Newtonsoft.Json;

namespace Stock.Shared.Models;

public static class Serialize
{
    public static string ToJson(this OptionsScreeningResponse self) => JsonConvert.SerializeObject(self, Stock.Shared.Models.Converter.Settings);
}