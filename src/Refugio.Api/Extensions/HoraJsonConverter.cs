using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refugio.Api.Extensions;

/// <summary>Horários no formato combinado com o app: "08:00".</summary>
public sealed class HoraJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] FormatosAceitos = ["HH:mm", "HH:mm:ss"];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var texto = reader.GetString();
        if (TimeOnly.TryParseExact(texto, FormatosAceitos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            return hora;

        throw new JsonException("Use o formato HH:mm.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
}
