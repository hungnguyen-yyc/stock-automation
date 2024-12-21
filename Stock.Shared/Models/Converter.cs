using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Stock.Shared.Models;

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            DateUnionConverter.Singleton,
            DateEnumConverter.Singleton,
            TypeEnumConverter.Singleton,
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        },
    };
}