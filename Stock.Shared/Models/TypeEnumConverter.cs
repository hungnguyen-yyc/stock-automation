using Newtonsoft.Json;

namespace Stock.Shared.Models;

internal class TypeEnumConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(TypeEnum) || t == typeof(TypeEnum?);

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        switch (value)
        {
            case "Call":
                return TypeEnum.Call;
            case "Put":
                return TypeEnum.Put;
        }
        throw new Exception("Cannot unmarshal type TypeEnum");
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        var value = (TypeEnum)untypedValue;
        switch (value)
        {
            case TypeEnum.Call:
                serializer.Serialize(writer, "Call");
                return;
            case TypeEnum.Put:
                serializer.Serialize(writer, "Put");
                return;
        }
        throw new Exception("Cannot marshal type TypeEnum");
    }

    public static readonly TypeEnumConverter Singleton = new TypeEnumConverter();
}