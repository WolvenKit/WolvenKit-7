using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WolvenKit.CR2W.SRT
{
    public class JsonByteArrayConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            short[] sByteArray = JsonSerializer.Deserialize<short[]>(ref reader);
            byte[] value = new byte[sByteArray.Length];
            for (int i = 0; i < sByteArray.Length; i++)
            {
                value[i] = (byte)sByteArray[i];
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var val in value)
            {
                writer.WriteNumberValue(val);
            }

            writer.WriteEndArray();
        }
    }

    public class JsonFloatNaNConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && reader.GetString() == "NaN")
            {
                return float.NaN;
            }

            return (float)reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value))
            {
                writer.WriteStringValue("NaN");
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }

    /*public class JsonSVertexPropConverter : JsonConverter<SVertexProperty>
    {
        public override SVertexProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            SVertexProperty value = new SVertexProperty();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return value;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token");

                var propName = reader.GetString();
                reader.Read();

                switch (propName)
                {
                    case nameof(value.PropertyFormat):
                        value.PropertyFormat = JsonSerializer.Deserialize<EVertexFormat>(ref reader);
                        break;
                    case nameof(value.PropertyName):
                        value.PropertyName = JsonSerializer.Deserialize<EVertexProperty>(ref reader);
                        break;
                        break;
                    case nameof(value.ValueOffset):
                        value.ValueOffset = JsonSerializer.Deserialize<sbyte[]>(ref reader);
                        break;
                    case "values":
                        if (value.PropertyFormat == EVertexFormat.VERTEX_FORMAT_FULL_FLOAT || value.PropertyFormat == EVertexFormat.VERTEX_FORMAT_HALF_FLOAT)
                        {
                            value.FloatValues = JsonSerializer.Deserialize<float[]>(ref reader);
                        }
                        else if (value.PropertyFormat == EVertexFormat.VERTEX_FORMAT_BYTE)
                        {
                            value.ByteValues = JsonSerializer.Deserialize<byte[]>(ref reader);
                        }
                        break;
                }
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, SVertexProperty value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(value.PropertyName), value.PropertyName.ToString());
            writer.WriteString(nameof(value.PropertyFormat), value.PropertyFormat.ToString());

            writer.WriteStartArray(nameof(value.ValueOffset));
            foreach (var val in value.ValueOffset)
            {
                writer.WriteNumberValue(val);
            }
            writer.WriteEndArray();

            if (value.PropertyFormat == EVertexFormat.VERTEX_FORMAT_FULL_FLOAT || value.PropertyFormat == EVertexFormat.VERTEX_FORMAT_HALF_FLOAT)
            {
                writer.WriteStartArray("values");
                foreach (var val in value.FloatValues)
                {
                    writer.WriteNumberValue(val);
                }
                writer.WriteEndArray();
            }
            else if (value.PropertyFormat == EVertexFormat.VERTEX_FORMAT_BYTE)
            {
                writer.WriteStartArray("values");
                foreach (var val in value.ByteValues)
                {
                    writer.WriteNumberValue(val);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }*/
}
