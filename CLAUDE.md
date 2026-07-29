# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A single-project Umbraco package (`Limbo.Umbraco.UnusedMedia`, **net10.0, Umbraco 17**) shipped as a NuGet package. It adds a Content-section backoffice dashboard listing media that no registered provider reports as "used". There is no test project and no consuming Umbraco site in this repo — the package is verified by building it into a local NuGet feed and installing it in a separate Umbraco site.

The branch was migrated from Umbraco 13 to 17; `documentation/UMBRACO-17-UPGRADE.md` is the authoritative record of what changed and why, including the parts that are compile-verified but not runtime-tested. Read it before touching the backoffice API or the dashboard element.

## Commands

```bash
# Build
dotnet build src/Limbo.Umbraco.UnusedMedia/Limbo.Umbraco.UnusedMedia.csproj

# Debug pack (Windows batch; version gets a `build<UTCtimestamp>` suffix)
./debug.bat      # → dotnet build ... -c Debug /t:rebuild /t:pack -p:PackageOutputPath=c:\nuget\Umbraco17

# Release pack → ./releases/nuget
./release.bat    # → dotnet build ... -c Release /t:rebuild /t:pack -p:PackageOutputPath=../../releases/nuget
```

On macOS/Linux run the equivalent `dotnet build … /t:pack -p:PackageOutputPath=…` directly. Note `dotnet build src/Limbo.Umbraco.UnusedMedia` (directory form) fails on this machine — pass the `.csproj` path.

Version lives in `<VersionPrefix>` in the csproj; bump it there for a release. Umbraco package references are pinned to the range `[17.0.0,17.9.9)`, so a restore resolves the *lowest* match (17.0.0) — that is intended for a library package.

## Branching

One long-lived branch per Umbraco major: `v13/main` is the repo default, `v1/main` and `v2/main` are legacy, `v17/dev` is the Umbraco 17 line. Do not assume a `main` branch exists.

## Architecture

### Inverted detection model

Nothing scans for *unused* media. Providers scan for **used** media keys; "unused" is the set difference computed at request time.

- `Providers/UsedMediaProvider` — abstract base, single method `HashSet<Guid> ScanForUsedMediaKeys()`.
- Built-ins registered in `Composers/UnusedMediaComposer`: `ContentCacheUsedMediaProvider` (walks the published content cache, regex-matches `umb://media/<guid>` and bare GUIDs in *source* values), `RedirectsUsedMediaProvider` (Skybrud.Umbraco.Redirects `SkybrudRedirects` table, no-ops if absent), `UmbracoRelationsUsedMediaProvider` (via `Helpers/SqlHelper`).
- Providers are collected with Umbraco's `LazyCollectionBuilderBase` (`UsedMediaProviderCollectionBuilder` → `UsedMediaProviderCollection`).
- `UnusedMediaService` runs each provider and caches its `UsedMediaReport` in `AppCaches.RuntimeCache` for **10 minutes**, keyed by provider full type name. The dashboard's "scan" button rebuilds one provider's report; providers are identified over the wire by `Type.GetFullNameWithAssembly()`.
- The difference/filter/sort/paging is all in `UnusedMediaBackOfficeHelper.GetUnusedMedia`.

Consequence: a provider that fails to see a reference causes a **false positive**, and the dashboard's delete action moves media to the recycle bin. Be conservative when touching provider scanning logic.

### UmbracoContext is not ambient

Management API requests carry no `IUmbracoContext`. Anything reading the published caches must wrap itself:

```csharp
using UmbracoContextReference ctx = _umbracoContextFactory.EnsureUmbracoContext();
using IServiceScope scope = _serviceScopeFactory.CreateScope();
IPublishedContentQuery query = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
```

Both `UnusedMediaBackOfficeHelper` and `ContentCacheUsedMediaProvider` do this. `IPublishedContentQuery` is scoped, hence the extra service scope. Materialize results (`.ToList()`) *inside* the scope — building a `UnusedMediaItem` reads properties and URLs off the cache.

### Backoffice API

`Controllers/BackOffice/UnusedMediaBackOfficeController` is a `ManagementApiControllerBase` routed via `[VersionedApiBackOfficeRoute("unused-media")]` → `/umbraco/management/api/v1/unused-media/…`, authorized with `AuthorizationPolicies.SectionAccessContent`.

`Dashboard:AllowedGroups` is enforced per-endpoint through `UnusedMediaBackOfficeHelper.IsAllowed(IUser)` (returns 403), not through a manifest condition. The `config` endpoint exists so the dashboard element can hide itself for disallowed users.

### JSON

**System.Text.Json only.** Response models use `[JsonPropertyName]`; enums go through `Json/CamelCaseEnumConverter<TEnum>`. `Json/JsonNodeExtensions` supplies the `GetRequiredString`/`GetRequiredGuid`/array-mapping helpers that Skybrud.Essentials only offers for Newtonsoft. Do not reintroduce `[JsonProperty]` — the Management API serializer ignores it, which fails silently.

Skybrud.Essentials still drags Newtonsoft in transitively; that is not a licence to use it.

### Extensibility contract (public API — treat as semver-relevant)

Consumers extend via `builder.UnusedMedia().AddProvider<T>()` / `.ReplaceProvider<TOld, TNew>()` (`Composing/UnusedMediaBuilder*`). Many helper/provider/parser methods are deliberately `virtual` for subclass override (`UnusedMediaBackOfficeHelper.CreateOptions/IsMatch/CreateCell/CreateFilters/IsAllowed`, `ContentCacheUsedMediaProvider.AppendMediaKeys`, `UnusedMediaBlockListParser.Parse*`). Do not narrow these.

`ContentCacheUsedMediaProvider` keeps a two-argument convenience constructor purely so downstream subclasses keep compiling; the three-argument one is primary.

### Dependencies-object pattern

Services take a single `*Dependencies` class (`UnusedMediaServiceDependencies`, `UnusedMediaBackOfficeHelperDependencies`) rather than a long ctor arg list, so adding a dependency doesn't break subclasses. Follow this when adding one.

### Block list parsing

`BlockList/UnusedMediaBlockListParser` re-parses raw block-list JSON into `Models/BlockList/*` rather than using Umbraco's value converters, because the provider works from source values. It implements the **Umbraco 14+ format**: `contentKey`/`settingsKey` (GUIDs, not UDIs), `contentData[].key`, and property values inside a `values[]` array of `{alias, culture, segment, value, editorAlias}`. Nested block lists are nested JSON objects, not encoded strings.

A layout item referencing missing content data is skipped, not thrown on — one corrupt property must not hide the whole report. Parse failures are caught and logged per-property in `ContentCacheUsedMediaProvider`.

### Backoffice frontend

New-backoffice Web Components, no build step:

- `wwwroot/umbraco-package.json` declares the dashboard (condition: `Umb.Condition.SectionAlias` = `Umb.Section.Content`) and the two `localization` extensions. This replaces the deleted `IDashboard` / `IManifestFilter` / `ServerVariablesParsingNotification` C# registrations, which no longer exist in Umbraco 17.
- `wwwroot/Scripts/dashboard.js` is a hand-written ES module: `UmbElementMixin(LitElement)`, bare `@umbraco-cms/backoffice/*` imports resolved by the backoffice's own import map, `umbHttpClient` + `tryExecute` for authenticated calls, `UMB_NOTIFICATION_CONTEXT` and `umbConfirmModal` for UI chrome.
- There is **no npm/vite/TypeScript toolchain and no generated OpenAPI client** — deliberately, to keep the repo build-step-free. If you add one, that's a project-wide decision, not a drive-by.
- Never use raw `fetch` for these endpoints; it will 401. Go through the `apiGet`/`apiPost` wrappers in `dashboard.js`.
- **Every request must declare `security: [{ scheme: "bearer", type: "http" }]`.** The backoffice HTTP client applies the access token only when that key is present (`config.security && await applyAuth(...)`), and every operation in Umbraco's own generated client passes it. Omitting it sends the request anonymously → 401 → the backoffice's 401 interceptor concludes the session expired and **logs the user out**. That is what `apiGet`/`apiPost` exist to guarantee; don't call `umbHttpClient` directly. Note the official docs example for `umbHttpClient.get` omits `security` and is wrong for authenticated endpoints.

### Localization

Client-side only. `wwwroot/Lang/en.js` and `da-dk.js` export a `unusedMedia` key group and are registered as `localization` extensions; keys are referenced as `unusedMedia_<key>` via `this.localize.term(...)`. The server returns stable aliases plus a `nameKey`/`labelKey` and an English fallback — it does **not** localize. `ILocalizedTextService` and `App_Plugins/*/Lang/*.xml` are not used and will not work.

Add keys to both language files.

## Conventions

- File-scoped namespaces; `#region Properties / Constructors / Member methods` grouping; XML doc comments on public members (the package publishes docs).
- Nullable and implicit usings are enabled; collection expressions (`[]`, `[.. x]`) are used throughout.
- Skybrud.Essentials is the utility layer of choice (`StringUtils.ParseInt32Set`, `SortOrder`, `.ToHashSet(...)`) — but note the non-obsolete namespace is `Skybrud.Essentials.Collections.Enumerables.Extensions`.
- The build is currently warning-free. Keep it that way; `IPublishedContent.Children` (the property) and several Skybrud extension namespaces are obsolete and will break in Umbraco 18.
