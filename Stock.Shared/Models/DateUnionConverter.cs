using Newtonsoft.Json;

namespace Stock.Shared.Models;

internal class DateUnionConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(DateUnion) || t == typeof(DateUnion?);

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
            case JsonToken.Date:
                var stringValue = serializer.Deserialize<string>(reader);
                DateTimeOffset dt;
                if (DateTimeOffset.TryParse(stringValue, out dt))
                {
                    return new DateUnion { DateTime = dt };
                }
                if (stringValue == "-0001-11-30")
                {
                    return new DateUnion { Enum = DateEnum.The00011130 };
                }
                break;
        }
        throw new Exception("Cannot unmarshal type DateUnion");
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        var value = (DateUnion)untypedValue;
        if (value.DateTime != null)
        {
            serializer.Serialize(writer, value.DateTime.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        if (value.Enum != null)
        {
            if (value.Enum == DateEnum.The00011130)
            {
                serializer.Serialize(writer, "-0001-11-30");
                return;
            }
        }
        throw new Exception("Cannot marshal type DateUnion");
    }

    public static readonly DateUnionConverter Singleton = new DateUnionConverter();
}