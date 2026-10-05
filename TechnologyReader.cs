using System.Text.Json;

namespace SprocketMaterialSelector;

internal static class TechnologyReader
{
    // Native Technology assets permit trailing commas. This option belongs only
    // to this reader; response catalogues retain their strict JSON parser.
    internal static JsonDocument Parse(string json) => JsonDocument.Parse(json,
        new JsonDocumentOptions { AllowTrailingCommas = true });

    internal static bool TryMaterial(JsonElement root, out JsonElement type,
        out JsonElement properties, out JsonElement rha)
    {
        type = properties = rha = default;
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty("type", out type) &&
            root.TryGetProperty("properties", out properties) && properties.ValueKind == JsonValueKind.Object &&
            properties.TryGetProperty("rhaFactor", out rha);
        // Invalid numeric material fields still go through the existing validation
        // and warning path; non-material engine/suspension assets are skipped.
    }
}
