using System.Diagnostics.CodeAnalysis;
using Skybrud.Essentials.Strings;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

public class UnusedMediaBlockListContentData {

    public Guid ContentTypeKey { get; }

    public string Udi { get; set; }

    public Dictionary<string, object?> Properties { get; }

    public UnusedMediaBlockListContentData(Guid contentTypeKey, string udi, Dictionary<string, object?> properties) {
        ContentTypeKey = contentTypeKey;
        Udi = udi;
        Properties = properties;
    }

    #region Member methods

    /// <summary>
    /// Returns the value of the property with the specified alias as a boolean. If the property doesn't exist, has no value, or cannot be parsed as a boolean, this method returns <see langword="false"/>.
    /// </summary>
    /// <param name="propertyAlias">The alias of the property.</param>
    /// <returns>The boolean value if successful; otherwise, <see langword="false"/>.</returns>
    public bool GetBoolean(string propertyAlias) {
        if (!Properties.TryGetValue(propertyAlias, out object? value)) return false;
        return value is true || StringUtils.ParseBoolean(value?.ToString());
    }

    /// <summary>
    /// Returns the value of the property with the specified alias as a string, or <see langword="null"/> if the property doesn't exist or has no value.
    /// </summary>
    /// <param name="propertyAlias">The alias of the property.</param>
    /// <returns>A string value if successful; otherwise, <see langword="null"/>.</returns>
    public string? GetString(string propertyAlias) {
        return !Properties.TryGetValue(propertyAlias, out object? value) ? null : value?.ToString();
    }

    /// <summary>
    /// Attempts to get the value of the property with the specified alias as a boolean. Returns <c>true</c> if the property exists and can be parsed as a boolean; otherwise, <see langword="false"/>. The value is returned via the <paramref name="result"/> out parameter.
    /// </summary>
    /// <param name="propertyAlias">The alias of the property.</param>
    /// <param name="result">When this method returns, holds the boolean value if successful; otherwise, <see langword="false"/>.</param>
    /// <returns>A boolean value indicating whether the operation was successful.</returns>
    public bool TryGetBoolean(string propertyAlias, out bool result) {
        result = false;
        if (!Properties.TryGetValue(propertyAlias, out object? value)) return false;
        if (value is bool b) {
            result = b;
            return true;
        }
        return StringUtils.TryParseBoolean(value?.ToString(), out result);
    }

    /// <summary>
    /// Attempts to get the value of the property with the specified alias as a string. Returns <c>true</c> if the property exists and has a value that can be returned as a string; otherwise, <see langword="false"/>. The value is returned via the <paramref name="result"/> out parameter.
    /// </summary>
    /// <param name="propertyAlias"></param>
    /// <param name="result">When this method returns, holds the boolean value if successful; otherwise, <see langword="false"/>.</param>
    /// <returns>A boolean value indicating whether the operation was successful.</returns>
    public bool TryGetString(string propertyAlias, [NotNullWhen(true)] out string? result) {
        result = GetString(propertyAlias);
        return result is not null;
    }

    #endregion

}