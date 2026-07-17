using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gestion_dentretiens.Api.Serialization
{
    /// <summary>
    /// Sérialise toutes les dates dans un format uniforme "yyyy-MM-ddTHH:mm:ss"
    /// (sans décalage horaire), pour que la création et la relecture d'une ressource
    /// renvoient exactement la même forme. En lecture, accepte les formats ISO courants.
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var texte = reader.GetString();
            return DateTime.Parse(texte, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}
