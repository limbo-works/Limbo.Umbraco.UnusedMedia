// [CHANGE: Umbraco 17 upgrade - Newtonsoft.Json replaced by System.Text.Json] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Limbo.Umbraco.UnusedMedia.Json;

/// <summary>
/// System.Text.Json converter serializing <typeparamref name="TEnum"/> values as camel cased strings.
///
/// The Umbraco 17 Management API serializes responses using System.Text.Json, so the Newtonsoft based
/// <c>EnumCamelCaseConverter</c> previously used by this package no longer has any effect.
/// </summary>
/// <typeparam name="TEnum">The type of the enum.</typeparam>
public class CamelCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum> where TEnum : struct, Enum {

    /// <summary>
    /// Initializes a new instance of the converter.
    /// </summary>
    public CamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase) { }

}
