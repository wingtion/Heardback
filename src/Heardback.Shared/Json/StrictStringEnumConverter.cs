using System.Text.Json.Serialization;

namespace Heardback.Shared.Json;

/// <summary>
/// Reads and writes an enum by name only. The default converter also accepts numbers, including ones that are
/// not defined, which would be stored and break every query that expects a known value.
/// </summary>
public sealed class StrictStringEnumConverter<TEnum>() : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
