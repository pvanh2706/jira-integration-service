using System.Globalization;
using System.Text.Json;
using JiraIntegrationService.Api.Application.Configuration.Models;
using JiraIntegrationService.Api.Common;

namespace JiraIntegrationService.Api.Application.Issues.Mapping;

public sealed class JiraFieldValueBuilder : IJiraFieldValueBuilder
{
    public object? BuildValue(FieldMappingConfig mapping, JsonElement? value)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        var convertedValue = ConvertValue(mapping, value);
        if (convertedValue is null)
        {
            return null;
        }

        var valueShape = NormalizeValueShape(mapping.ValueShape);
        return valueShape switch
        {
            "raw" => convertedValue,
            "name" => BuildObject("name", convertedValue),
            "id" => BuildObject("id", convertedValue),
            "value" => BuildObject("value", convertedValue),
            "arrayOfName" => BuildObjectArray("name", convertedValue, mapping),
            "arrayOfId" => BuildObjectArray("id", convertedValue, mapping),
            _ => throw new RequestValidationException(
                $"valueShape '{mapping.ValueShape}' is not supported for mapping '{mapping.SourcePath}'.")
        };
    }

    private static object? ConvertValue(FieldMappingConfig mapping, JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var valueType = NormalizeValueType(mapping.ValueType);
        var element = value.Value;

        return valueType switch
        {
            "string" or "date" => ConvertToString(element),
            "number" => ConvertToNumber(element, mapping),
            "boolean" => ConvertToBoolean(element, mapping),
            "object" => element.ValueKind == JsonValueKind.Object
                ? element.Clone()
                : throw InvalidValueType(mapping, "object"),
            "array" => ConvertToArray(element, mapping),
            _ => throw new RequestValidationException(
                $"valueType '{mapping.ValueType}' is not supported for mapping '{mapping.SourcePath}'.")
        };
    }

    private static string? ConvertToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => NormalizeOptional(element.GetString()),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => element.GetRawText()
        };
    }

    private static decimal ConvertToNumber(JsonElement element, FieldMappingConfig mapping)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String
            && decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out number))
        {
            return number;
        }

        throw InvalidValueType(mapping, "number");
    }

    private static bool ConvertToBoolean(JsonElement element, FieldMappingConfig mapping)
    {
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return element.GetBoolean();
        }

        if (element.ValueKind == JsonValueKind.String
            && bool.TryParse(element.GetString(), out var value))
        {
            return value;
        }

        throw InvalidValueType(mapping, "boolean");
    }

    private static IReadOnlyList<object?> ConvertToArray(JsonElement element, FieldMappingConfig mapping)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw InvalidValueType(mapping, "array");
        }

        return element
            .EnumerateArray()
            .Select(ConvertArrayItem)
            .Where(item => item is not null)
            .ToArray();
    }

    private static object? ConvertArrayItem(JsonElement item)
    {
        return item.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => NormalizeOptional(item.GetString()),
            JsonValueKind.Number when item.TryGetDecimal(out var number) => number,
            JsonValueKind.True or JsonValueKind.False => item.GetBoolean(),
            JsonValueKind.Object or JsonValueKind.Array => item.Clone(),
            _ => item.GetRawText()
        };
    }

    private static Dictionary<string, object?> BuildObject(string propertyName, object? value)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [propertyName] = value
        };
    }

    private static IReadOnlyList<Dictionary<string, object?>> BuildObjectArray(
        string propertyName,
        object value,
        FieldMappingConfig mapping)
    {
        if (value is not IEnumerable<object?> values || value is string)
        {
            throw InvalidValueType(mapping, "array");
        }

        return values
            .Where(item => item is not null)
            .Select(item => BuildObject(propertyName, item))
            .ToArray();
    }

    private static RequestValidationException InvalidValueType(
        FieldMappingConfig mapping,
        string expectedType)
    {
        return new RequestValidationException(
            $"Value for '{mapping.SourcePath}' must be {expectedType}.");
    }

    private static string NormalizeValueType(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "string"
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeValueShape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "raw";
        }

        var normalizedValue = value.Trim();
        if (string.Equals(normalizedValue, "arrayOfName", StringComparison.OrdinalIgnoreCase))
        {
            return "arrayOfName";
        }

        if (string.Equals(normalizedValue, "arrayOfId", StringComparison.OrdinalIgnoreCase))
        {
            return "arrayOfId";
        }

        return normalizedValue.ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
