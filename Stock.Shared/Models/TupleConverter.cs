using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Stock.Shared.Models;

public class TupleConverter<T1, T2> : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof((T1, T2));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        return ((T1)jo["Item1"].ToObject<T1>(), (T2)jo["Item2"].ToObject<T2>());
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var tuple = (ValueTuple<T1, T2>)value;
        var jo = new JObject();
        jo.Add("Item1", JToken.FromObject(tuple.Item1));
        jo.Add("Item2", JToken.FromObject(tuple.Item2));
        jo.WriteTo(writer);
    }
}