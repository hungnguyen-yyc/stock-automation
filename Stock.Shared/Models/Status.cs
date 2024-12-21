using Newtonsoft.Json;

namespace Stock.Shared.Models;

public partial class Status
{
    [JsonProperty("code")]
    public long Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}