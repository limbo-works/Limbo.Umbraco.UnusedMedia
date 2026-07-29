# Limbo Unused Media

An Umbraco package that helps content editors and administrators identify and clean up media that isn't referenced anywhere on the site. It adds a dashboard to the **Content** section listing media that no registered provider reports as being in use.

**This branch targets Umbraco 17.** See [documentation/UMBRACO-17-UPGRADE.md](documentation/UMBRACO-17-UPGRADE.md) for what changed coming from v13.

| Package version | Umbraco | .NET |
|---|---|---|
| 17.x | 17 | net10.0 |
| 13.x | 13 | net8.0 |

## Table of Contents

- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [How it works](#how-it-works)
- [Extensibility](#extensibility)
- [HTTP API](#http-api)

## Installation

```bash
dotnet add package Limbo.Umbraco.UnusedMedia
```

## Configuration

All settings live under `Limbo:UnusedMedia` in `appsettings.json`. All of them are optional.

```json
{
  "Limbo": {
    "UnusedMedia": {
      "IgnoredFolderIds": [ 1234 ],
      "Dashboard": {
        "AllowedGroups": [ "admin", "webadministratorer" ],
        "PerPage": 15
      }
    }
  }
}
```

| Setting | Default | Description |
|---|---|---|
| `IgnoredFolderIds` | *(empty)* | Media folder IDs to ignore. Media with one of these IDs in its path is never listed as unused. |
| `Dashboard:AllowedGroups` | *(empty)* | User group aliases allowed to use the dashboard. Empty means everyone with access to the Content section. Enforced on the API endpoints, not just in the UI. |
| `Dashboard:PerPage` | `15` | Rows per page. |

`Dashboard:ElementName` is obsolete as of 17.0.0 — the dashboard element is declared in the package's `umbraco-package.json`.

## Usage

Go to **Content** → **Unused media**.

- **Filters** — free text on the media name, plus folder, creator and last editor.
- **Stats** — how many of your media items are not directly in use, and when the underlying reports were generated.
- **Reports** — click the report date to expand the list of providers, each with its own "scan again" button.
- **Table** — sortable by name, last updated and size. Each row links to the media item and to the file itself.
- **Delete** — trash a single item, or select several and trash them in bulk. Media is moved to the recycle bin, not deleted permanently, and the action is audited against the current user.

## How it works

Nothing scans for *unused* media. Registered providers each scan for media that **is** used, and "unused" is the set difference computed when the dashboard asks for the list.

Built-in providers:

| Provider | What it looks at |
|---|---|
| `ContentCacheUsedMediaProvider` | Every property of every item in the published content cache, matching `umb://media/<guid>` and bare GUIDs in the raw source values. |
| `RedirectsUsedMediaProvider` | The `SkybrudRedirects` table (from *Skybrud.Umbraco.Redirects*), if present. No-ops if the table doesn't exist. |
| `UmbracoRelationsUsedMediaProvider` | The `umbracoRelation` table. |

Each provider's result is cached for 10 minutes in `AppCaches.RuntimeCache`, keyed by the provider type. The "scan again" button rebuilds a single provider's report and replaces its cache entry.

> **Because the list is a set difference, a provider that fails to see a reference produces a false positive** — and the dashboard's delete action moves media to the recycle bin. If you store media references somewhere the built-in providers can't see (a custom property editor format, an external system, a headless consumer), write a provider for it.

Folders are never listed, and media under an `IgnoredFolderIds` path is excluded entirely.

## Extensibility

Register your own provider:

```csharp
public class MyUsedMediaProvider : UsedMediaProvider {
    public override HashSet<Guid> ScanForUsedMediaKeys() {
        // return the keys of every media item your system references
    }
}

public class MyComposer : IComposer {
    public void Compose(IUmbracoBuilder builder) {
        builder.UnusedMedia().AddProvider<MyUsedMediaProvider>();
        // or replace a built-in one:
        // builder.UnusedMedia().ReplaceProvider<ContentCacheUsedMediaProvider, MyProvider>();
    }
}
```

Most of `UnusedMediaBackOfficeHelper` is `virtual` — override `CreateOptions`, `IsMatch`, `CreateCell`, `CreateFilters`, `GetSites` or `IsAllowed` and re-register the helper to change what the dashboard shows. `ContentCacheUsedMediaProvider.AppendMediaKeys(UnusedMediaBlockListItem, …)` is the hook for pulling media keys out of block list blocks in a format-aware way; by default it does nothing, since the regex sweep over the raw value already catches ordinary references.

## HTTP API

The endpoints are part of the Management API and require an authenticated backoffice user with access to the Content section.

| Method | Route |
|---|---|
| `GET` | `/umbraco/management/api/v1/unused-media` |
| `GET` | `/umbraco/management/api/v1/unused-media/config` |
| `GET` | `/umbraco/management/api/v1/unused-media/filters` |
| `GET` | `/umbraco/management/api/v1/unused-media/sites` |
| `POST` | `/umbraco/management/api/v1/unused-media/scan?provider={alias}` |
| `POST` | `/umbraco/management/api/v1/unused-media/trash` |

## Building

```bash
# Debug build + pack to a local feed
./debug.bat

# Release build + pack to ./releases/nuget
./release.bat
```

On macOS/Linux, run the equivalent `dotnet build … /t:pack -p:PackageOutputPath=…` directly.

## License

MIT — see [LICENSE.md](LICENSE.md).
