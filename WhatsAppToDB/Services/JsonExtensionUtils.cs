using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class JsonExtensionUtils
{
    /// <summary>
    /// Processes a collection list (or a single object) and extracts unmapped fields.
    /// For a list, it aggregates all unmapped keys found across all items in the array.
    /// </summary>
    public static Dictionary<string, object?> GetUnmappedProperties(object instance, string jsonContent)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (string.IsNullOrWhiteSpace(jsonContent)) return new Dictionary<string, object?>();

        var rootNode = JsonNode.Parse(jsonContent);
        if (rootNode == null) return new Dictionary<string, object?>();

        var unmappedDictionary = new Dictionary<string, object?>();

        // Scenario A: The instance is a List or Array (IEnumerable) and the JSON is a raw Array [...]
        if (instance is IEnumerable listInstance && rootNode is JsonArray jsonArray)
        {
            // Convert to a generic object list so we can match indexes
            var listItems = listInstance.Cast<object>().ToList();
            
            // Iterate over the items that match structurally by index
            for (int i = 0; i < Math.Min(listItems.Count, jsonArray.Count); i++)
            {
                var itemInstance = listItems[i];
                var itemJsonNode = jsonArray[i];

                if (itemInstance != null && itemJsonNode is JsonObject itemJsonObject)
                {
                    // Inspect this explicit item's properties
                    var itemUnmapped = ExtractUnmappedFromObject(itemInstance, itemJsonObject);
                    
                    // Merge properties into the main output dictionary (suffixes duplicates with index numbers)
                    foreach (var kvp in itemUnmapped)
                    {
                        string key = listItems.Count > 1 ? $"{kvp.Key}_item{i}" : kvp.Key;
                        unmappedDictionary[key] = kvp.Value;
                    }
                }
            }
            return unmappedDictionary;
        }

        // Scenario B / Standard: The instance is a single concrete object instance and JSON is an Object {...}
        if (rootNode is JsonObject jsonObject)
        {
            return ExtractUnmappedFromObject(instance, jsonObject);
        }

        return unmappedDictionary;
    }

    /// <summary>
    /// Base engine inspecting a single instantiated object against a parsed JsonObject block.
    /// </summary>
    private static Dictionary<string, object?> ExtractUnmappedFromObject(object instance, JsonObject jsonObject)
    {
        var targetType = instance.GetType();
        var csharpProperties = targetType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unmapped = new Dictionary<string, object?>();

        foreach (var kvp in jsonObject)
        {
            if (!csharpProperties.Contains(kvp.Key))
            {
                unmapped[kvp.Key] = ConvertJsonNode(kvp.Value);
            }
        }

        return unmapped;
    }

    private static object? ConvertJsonNode(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue value)
        {
            var element = value.GetValue<JsonElement>();
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }
        return node.ToJsonString();
    }

/// <summary>
    /// Compares a single item from a list against its corresponding item in a JSON array by index 
    /// and extracts any properties that are missing from the C# object definition.
    /// </summary>
    public static Dictionary<string, object?> GetUnmappedPropertiesForIndex(object itemInstance, string jsonContent, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(itemInstance);
        if (string.IsNullOrWhiteSpace(jsonContent) || arrayIndex < 0) return new Dictionary<string, object?>();

        var rootNode = JsonNode.Parse(jsonContent);
        if (rootNode is JsonArray jsonArray && arrayIndex < jsonArray.Count)
        {
            var targetNode = jsonArray[arrayIndex];
            if (targetNode is JsonObject jsonObject)
            {
                var targetType = itemInstance.GetType();
                var csharpProperties = targetType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var unmapped = new Dictionary<string, object?>();
                foreach (var kvp in jsonObject)
                {
                    if (!csharpProperties.Contains(kvp.Key))
                    {
                        unmapped[kvp.Key] = ConvertJsonNode(kvp.Value);
                    }
                }
                return unmapped;
            }
        }

        return new Dictionary<string, object?>();
    }

    private static object? ConvertJsonNode1(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue value)
        {
            var element = value.GetValue<JsonElement>();
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }
        return node.ToJsonString();
    }

}