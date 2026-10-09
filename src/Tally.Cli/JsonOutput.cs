using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Tally.Core;

namespace Tally.Cli;

/// <summary>One line of JSON per answer. Quantities are strings so no digit is lost to floating point.</summary>
internal static class JsonOutput
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        // The output is read by programs and people, never embedded in HTML, so quotes and angle brackets stay readable.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Write(Action<Utf8JsonWriter> body)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
        {
            body(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static void Amount(Utf8JsonWriter writer, Amount amount)
    {
        writer.WriteStartObject();
        writer.WriteString("quantity", amount.Quantity.ToString(CultureInfo.InvariantCulture));
        writer.WriteString("commodity", amount.Commodity);
        writer.WriteEndObject();
    }

    public static void Balance(Utf8JsonWriter writer, string name, Balance balance)
    {
        writer.WriteStartArray(name);
        foreach (var amount in balance.Amounts)
        {
            Amount(writer, amount);
        }

        writer.WriteEndArray();
    }

    public static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
