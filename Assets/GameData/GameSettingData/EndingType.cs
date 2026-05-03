using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum EndingType
{
    None,
    Type1,
    Type2,
    Type3,
    Type4,
    Type5
}
