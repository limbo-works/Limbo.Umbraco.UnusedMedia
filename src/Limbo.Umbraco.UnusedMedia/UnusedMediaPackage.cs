using System.Diagnostics;
using Umbraco.Cms.Core.Semver;

namespace Limbo.Umbraco.UnusedMedia;

public class UnusedMediaPackage {

    public const string Alias = "Limbo.Umbraco.UnusedMedia";

    /// <summary>
    /// Gets the friendly name of the package.
    /// </summary>
    public const string Name = "Limbo Unused Media";

    /// <summary>
    /// Gets the version of the package.
    /// </summary>
    public static readonly Version Version = typeof(UnusedMediaPackage).Assembly.GetName().Version ?? new Version(0, 0, 0);

    /// <summary>
    /// Gets the informational version of the package.
    /// </summary>
    public static readonly string InformationalVersion = FileVersionInfo.GetVersionInfo(typeof(UnusedMediaPackage).Assembly.Location).ProductVersion?.Split('+')[0] ?? "0.0.0";

    /// <summary>
    /// Gets the semantic version of the package.
    /// </summary>
    public static readonly SemVersion SemVersion = InformationalVersion;

    /// <summary>
    /// Gets the URL of the GitHub repository for this package.
    /// </summary>
    public const string GitHubUrl = "https://github.com/limbo-works/Limbo.Umbraco.UnusedMedia";

    /// <summary>
    /// Gets the URL of the issue tracker for this package.
    /// </summary>
    public const string IssuesUrl = "https://github.com/limbo-works/Limbo.Umbraco.UnusedMedia/issues";

    /// <summary>
    /// Gets the URL of the documentation for this package.
    /// </summary>
    public const string DocumentationUrl = "https://packages.limbo.works/limbo.umbraco.unusedmedia/v17/";

}
