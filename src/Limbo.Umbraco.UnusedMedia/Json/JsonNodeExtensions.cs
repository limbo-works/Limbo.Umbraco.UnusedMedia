// [CHANGE: Umbraco 17 upgrade - Newtonsoft.Json replaced by System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Limbo.Umbraco.UnusedMedia.Json;

/// <summary>
/// Various extension methods for working with <see cref="JsonObject"/>.
///
/// These replace the <c>Skybrud.Essentials.Json.Newtonsoft</c> extension methods that this package used prior to
/// the Umbraco 17 upgrade - Skybrud.Essentials only ships Newtonsoft based JSON helpers.
/// </summary>
public static class JsonNodeExtensions {

    /// <summary>
    /// Returns the value of the property with the specified <paramref name="propertyName"/> as a string, or
    /// <see langword="null"/> if the property doesn't exist or has a null value.
    /// </summary>
    public static string? GetString(this JsonObject json, string propertyName) {

        // [CHANGE: code review fix - "GetValue<string>()" throws "InvalidOperationException" when the property
        // holds a number, boolean, object or array. The Newtonsoft based helper this replaces was lenient, and
        // callers such as "GetRequiredGuid" document that they throw "JsonParseException".] Related: BlockList/UnusedMediaBlockListParser.cs
        if (!json.TryGetPropertyValue(propertyName, out JsonNode? node)) return null;
        if (node is not JsonValue value) return null;

        return value.TryGetValue(out string? result) ? result : value.ToString();

    }

    /// <summary>
    /// Returns the value of the property with the specified <paramref name="propertyName"/> as a string.
    /// </summary>
    /// <exception cref="JsonParseException">If the property doesn't exist or has a null or empty value.</exception>
    public static string GetRequiredString(this JsonObject json, string propertyName) {
        string? value = json.GetString(propertyName);
        if (string.IsNullOrWhiteSpace(value)) throw new JsonParseException($"Required property '{propertyName}' is missing or empty.");
        return value;
    }

    /// <summary>
    /// Returns the value of the property with the specified <paramref name="propertyName"/> as a GUID, or
    /// <see langword="null"/> if the property doesn't exist or doesn't hold a valid GUID.
    /// </summary>
    public static Guid? GetGuid(this JsonObject json, string propertyName) {
        return Guid.TryParse(json.GetString(propertyName), out Guid guid) ? guid : null;
    }

    /// <summary>
    /// Returns the value of the property with the specified <paramref name="propertyName"/> as a GUID.
    /// </summary>
    /// <exception cref="JsonParseException">If the property doesn't exist or doesn't hold a valid GUID.</exception>
    public static Guid GetRequiredGuid(this JsonObject json, string propertyName) {
        Guid? value = json.GetGuid(propertyName);
        if (value is null) throw new JsonParseException($"Required property '{propertyName}' is missing or not a valid GUID.");
        return value.Value;
    }

    /// <summary>
    /// Returns the value of the property with the specified <paramref name="propertyName"/> as a
    /// <see cref="JsonArray"/>, or <see langword="null"/> if the property doesn't exist or isn't an array.
    /// </summary>
    public static JsonArray? GetArray(this JsonObject json, string propertyName) {
        return json.TryGetPropertyValue(propertyName, out JsonNode? node) ? node as JsonArray : null;
    }

    /// <summary>
    /// Maps each <see cref="JsonObject"/> item of the array of the property with the specified
    /// <paramref name="propertyName"/> using the specified <paramref name="callback"/>. If the property doesn't
    /// exist or isn't an array, an empty list is returned.
    /// </summary>
    public static IReadOnlyList<T> GetArrayItems<T>(this JsonObject json, string propertyName, Func<JsonObject, T> callback) {
        JsonArray? array = json.GetArray(propertyName);
        if (array is null) return [];
        List<T> temp = [];
        foreach (JsonNode? item in array) {
            if (item is JsonObject obj) temp.Add(callback(obj));
        }
        return temp;
    }

    /// <summary>
    /// Maps each <see cref="JsonObject"/> item of the array of the property with the specified
    /// <paramref name="propertyName"/> using the specified <paramref name="callback"/>.
    /// </summary>
    /// <exception cref="JsonParseException">If the property doesn't exist or isn't an array.</exception>
    public static IReadOnlyList<T> GetRequiredArrayItems<T>(this JsonObject json, string propertyName, Func<JsonObject, T> callback) {
        if (json.GetArray(propertyName) is null) throw new JsonParseException($"Required property '{propertyName}' is missing or not an array.");
        return json.GetArrayItems(propertyName, callback);
    }

    /// <summary>
    /// Attempts to parse the specified <paramref name="input"/> into a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="input">The JSON string to parse.</param>
    /// <param name="result">When this method returns, holds the parsed <see cref="JsonObject"/> if successful; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> was parsed as a JSON object; otherwise <see langword="false"/>.</returns>
    public static bool TryParseJsonObject(string? input, [NotNullWhen(true)] out JsonObject? result) {

        result = null;

        if (string.IsNullOrWhiteSpace(input)) return false;
        if (input.TrimStart()[0] != '{') return false;

        try {
            result = JsonNode.Parse(input) as JsonObject;
            return result is not null;
        } catch {
            return false;
        }

    }

}

/// <summary>
/// Exception thrown when a JSON structure doesn't match what this package expects.
/// </summary>
public class JsonParseException : Exception {

    /// <summary>
    /// Initializes a new exception with the specified <paramref name="message"/>.
    /// </summary>
    public JsonParseException(string message) : base(message) { }

}
