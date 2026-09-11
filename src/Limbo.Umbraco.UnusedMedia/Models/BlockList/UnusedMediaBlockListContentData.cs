// [CHANGE: Umbraco 17 upgrade - block content data is keyed by GUID and holds a "values" array as of Umbraco 14] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Diagnostics.CodeAnalysis;
using Skybrud.Essentials.Strings;

namespace Limbo.Umbraco.UnusedMedia.Models.BlockList;

/// <summary>
/// Class representing a single entry of the <c>contentData</c> or <c>settingsData</c> array of a block list value.
///
/// As of Umbraco 14, entries are identified by a GUID <see cref="Key"/> rather than a UDI, and their property values
/// live in a <c>values</c> array of <c>{ alias, culture, segment, value, editorAlias }</c> objects rather than being
/// flattened onto the entry itself. <see cref="Properties"/> exposes those values keyed by their property alias.
/// </summary>
public class UnusedMediaBlockListContentData {

    /// <summary>
    /// Gets the key of the element type of this entry.
    /// </summary>
    public Guid ContentTypeKey { get; }

    /// <summary>
    /// Gets or sets the key of this entry.
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    /// Gets the property values of this entry, keyed by their property alias.
    /// </summary>
    public Dictionary<string, object?> Properties { get; }

    /// <summary>
    /// Gets the property editor alias of each property value, keyed by their property alias. Umbraco 14 added the
    /// <c>editorAlias</c> field to each entry of the <c>values</c> array, which lets subclasses decide how to
    /// interpret a value without having to look up the element type.
    /// </summary>
    public Dictionary<string, string?> EditorAliases { get; }

    public UnusedMediaBlockListContentData(Guid contentTypeKey, Guid key, Dictionary<string, object?> properties)
        : this(contentTypeKey, key, properties, []) { }

    public UnusedMediaBlockListContentData(Guid contentTypeKey, Guid key, Dictionary<string, object?> properties, Dictionary<string, string?> editorAliases) {
        ContentTypeKey = contentTypeKey;
        Key = key;
        Properties = properties;
        EditorAliases = editorAliases;
    }

    #region Member methods

    /// <summary>
    /// Returns the property editor alias of the property with the specified alias, or <see langword="null"/> if the property doesn't exist.
    /// </summary>
    /// <param name="propertyAlias">The alias of the property.</param>
    /// <returns>The property editor alias if successful; otherwise, <see langword="null"/>.</returns>
    public string? GetEditorAlias(string propertyAlias) {
        return EditorAliases.GetValueOrDefault(propertyAlias);
    }

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
