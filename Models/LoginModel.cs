using System.Text.Json;
using System.Text.Json.Serialization;

namespace agaverosActividades.Models
{
    public class StringToBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.False) return false;
            if (reader.TokenType == JsonTokenType.String)
            {
                return bool.TryParse(reader.GetString(), out var result) && result;
            }
            return false;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public class LoginModel
    {
        public string VchLogin { get; set; } = string.Empty;
        public string VchPassword { get; set; } = string.Empty;
    }

    public class LogeoModel
    {
        [JsonConverter(typeof(StringToBoolConverter))]
        public bool BitExiste { get; set; }

        public string VchNombre { get; set; } = string.Empty;

        [JsonConverter(typeof(StringToBoolConverter))]
        public bool BitEsAdministrador { get; set; }
    }
}