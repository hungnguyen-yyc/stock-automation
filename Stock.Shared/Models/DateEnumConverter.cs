using Newtonsoft.Json;

namespace Stock.Shared.Models;

internal class DateEnumConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(DateEnum) || t == typeof(DateEnum?);

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        if (value == "-0001-11-30")
        {
            return DateEnum.The00011130;
        }
        throw new Exception("Cannot unmarshal type DateEnum");
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        var value = (DateEnum)untypedValue;
        if (value == DateEnum.The00011130)
        {
            serializer.Serialize(writer, "-0001-11-30");
            return;
        }
        throw new Exception("Cannot marshal type DateEnum");
    }

    public static readonly DateEnumConverter Singleton = new DateEnumConverter();
}