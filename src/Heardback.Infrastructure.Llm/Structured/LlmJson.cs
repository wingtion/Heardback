using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Heardback.Infrastructure.Llm.Structured;

/// <summary>
/// Strict JSON reading for model answers, and the matching JSON schema generated from the answer type so the
/// two cannot drift apart.
/// </summary>
internal static class LlmJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static readonly ConcurrentDictionary<Type, JsonElement> Schemas = new();

    private static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
    };

    /// <summary>The answer schema: every property required, unknown properties refused, enums by name.</summary>
    public static JsonElement SchemaFor(Type answerType) =>
        Schemas.GetOrAdd(answerType, type =>
            JsonSerializer.SerializeToElement(JsonSchemaExporter.GetJsonSchemaAsNode(Options, type, ExporterOptions)));

    /// <summary>Reads <paramref name="content"/> as <typeparamref name="T"/>; false on any deviation from the type.</summary>
    public static bool TryRead<T>(string content, out T? value)
        where T : class
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(content, Options);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            AllowDuplicateProperties = false,
            NumberHandling = JsonNumberHandling.Strict,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        options.MakeReadOnly();
        return options;
    }
}
