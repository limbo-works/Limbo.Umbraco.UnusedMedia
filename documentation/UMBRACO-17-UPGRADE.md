# Umbraco 17 upgrade

Recap of the migration of **Limbo.Umbraco.UnusedMedia** from Umbraco 13 (`net8.0`) to Umbraco 17 (`net10.0`), performed on branch `v17/dev`.

This is a rewrite, not a version bump. Umbraco 14 replaced the AngularJS backoffice with a Web Components backoffice and moved the backoffice APIs to the Management API; every part of this package that touched either had to change.

---

## 1. Target framework and dependencies

`src/Limbo.Umbraco.UnusedMedia/Limbo.Umbraco.UnusedMedia.csproj`

| Package | Before | After | Why |
|---|---|---|---|
| *(TargetFramework)* | `net8.0` | `net10.0` | Umbraco 17 targets `net10.0`. |
| `Umbraco.Cms.Web.Website` | `13.0.0` | `[17.0.0,17.9.9)` | Requested range. |
| `Umbraco.Cms.Web.BackOffice` | `13.0.0` | **removed** | The package does not exist past Umbraco 13. |
| `Umbraco.Cms.Api.Management` | – | `[17.0.0,17.9.9)` | Replacement — provides `ManagementApiControllerBase` and backoffice routing. |
| `Skybrud.Essentials` | `1.1.66` | `1.1.68` | Has a `net10.0` target. |
| `Skybrud.Essentials.Umbraco` | `13.0.7` | **removed** | No stable Umbraco 17 release exists (only `17.0.0-alpha003`, which falls *outside* the requested `[17.0.0,17.9.9)` range under SemVer precedence). Only three members were used; all were swapped for Umbraco/Examine equivalents. |
| `Skybrud.Essentials.AspNetCore` | `1.0.2` | **removed** | Pulls in `Microsoft.AspNetCore.Mvc.NewtonsoftJson`, which conflicts with the System.Text.Json based Management API. Its two query-string helpers were inlined. |
| `Limbo.Forms` | `1.0.2` | **removed** | Its field models are Newtonsoft-decorated, so `[JsonProperty]` names would be silently ignored by the Management API serializer. Replaced with own filter models. |
| `Newtonsoft.Json` | `13.0.3` | **removed** | Package is now fully System.Text.Json. |

Package version bumped to `17.0.0`; `PackageProjectUrl`, `DocumentationUrl` and `debug.bat`'s local feed path moved from v13 to v17.

> **Note on resolution:** a version *range* makes NuGet resolve the **lowest** applicable version, so a consuming project that doesn't pin Umbraco itself gets `17.0.0`. That is the correct behaviour for a library package — the consuming site decides which 17.x it runs.

## 2. Package registration: C# → `umbraco-package.json`

Umbraco 14 removed `IDashboard`, `IManifestFilter` and the whole `PackageManifest` C# registration path for extensions. Deleted:

- `Dashboards/UnusedMediaDashboard.cs`
- `Manifests/UnusedMediaManifest.cs`
- `Notifications/Handlers/ServerVariablesParsingHandler.cs` — the new backoffice has no `Umbraco.Sys.ServerVariables`.

Added `wwwroot/umbraco-package.json`, declaring the dashboard (conditioned on `Umb.Condition.SectionAlias` = `Umb.Section.Content`) and the two localization extensions.

`Composers/UnusedMediaComposer.cs` lost its `Dashboards()`, `ManifestFilters()` and `AddNotificationHandler` calls. Provider collection and service registrations are unchanged.

### Dashboard access control changed meaning

`Dashboard:AllowedGroups` used to be enforced through `IDashboard.AccessRules`, which only **hid the dashboard in the UI** — the v13 API endpoints were reachable by any backoffice user.

That interface is gone, so the check moved to `UnusedMediaBackOfficeHelper.IsAllowed(IUser)` (virtual) and is now enforced **on every endpoint** of the controller (`403` for disallowed users). The dashboard element calls the new `config` endpoint and renders an "access denied" message instead of the table. This is a behaviour change: the setting is now a real permission rather than a UI hint.

## 3. Backoffice API: `UmbracoAuthorizedApiController` → Management API

`Controllers/BackOffice/UnusedMediaBackOfficeController.cs`

- Base class `UmbracoAuthorizedApiController` → `ManagementApiControllerBase`.
- Routing `[Route("umbraco/backoffice/api/UnusedMediaBackOffice/[action]")]` → `[VersionedApiBackOfficeRoute("unused-media")]`, plus `[ApiExplorerSettings(GroupName = "Unused Media")]` so the endpoints show up in the Management API OpenAPI document.
- Authorization is now explicit: `[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]`.
- All actions return `IActionResult` with proper `[ProducesResponseType]` annotations instead of `object`.
- `GetImportMap` (which hand-rolled an ES module import map) was **deleted** — see §5.

### Endpoint map

| Before | After |
|---|---|
| `GET /umbraco/backoffice/api/UnusedMediaBackOffice/GetUnusedMedia` | `GET /umbraco/management/api/v1/unused-media` |
| `GET …/GetFilters` | `GET /umbraco/management/api/v1/unused-media/filters` |
| `GET …/GetSites` | `GET /umbraco/management/api/v1/unused-media/sites` |
| `POST …/StartScan?provider=` | `POST /umbraco/management/api/v1/unused-media/scan?provider=` |
| `POST …/TrashMedia` | `POST /umbraco/management/api/v1/unused-media/trash` |
| `GET …/importmap.js` | *removed* |
| – | `GET /umbraco/management/api/v1/unused-media/config` *(new)* |

The `TrashMedia` body is now the typed `TrashMediaRequest` (`mediaKey` / `mediaKeys`) instead of a raw `JObject`, and the local `HelloExtensions.TryGetGuidArray` helper is gone.

## 4. Newtonsoft.Json → System.Text.Json

Umbraco 17 serializes Management API responses with System.Text.Json, which ignores `[JsonProperty]`. Every response model was converted to `[JsonPropertyName]`:

`UnusedMediaResult`, `UnusedMediaItem`, `UnusedMediaItemCell`, `UnusedMediaColumn`, `UsedMediaReport`, `UsedMediaReportSummary`, `UnusedSiteItem`, and all `Models/BlockList/*`.

Supporting changes:

- **New** `Json/CamelCaseEnumConverter<TEnum>` replaces Skybrud's Newtonsoft `EnumCamelCaseConverter` (used for `SortOrder` and `UnusedMediaColumnType`).
- **New** `Json/JsonNodeExtensions` replaces the `Skybrud.Essentials.Json.Newtonsoft` extension methods (`GetRequiredString`, `GetRequiredGuid`, array mapping, `TryParseJsonObject`). Skybrud.Essentials only ships Newtonsoft JSON helpers.
- `Models/BlockList/UnusedMediaJsonObjectBase` no longer derives from Skybrud's `JsonObjectBase`; it wraps a `JsonObject` and exposes it as `JsonObject`.
- **`EssentialsTime` → `DateTimeOffset`** on `IUsedMediaReport.CreateDate`, `UsedMediaReport`, `UsedMediaReportSummary` and `UnusedMediaItem.CreateDate`/`UpdateDate`. `EssentialsTime` has no System.Text.Json support and would have serialized as an object graph; `DateTimeOffset` gives the client plain ISO 8601.

## 5. Block list parsing rewritten for the Umbraco 14 format

`BlockList/UnusedMediaBlockListParser.cs` + `Models/BlockList/*`

The stored block editor JSON changed shape in Umbraco 14:

| Umbraco 13 | Umbraco 14+ |
|---|---|
| `layout["Umbraco.BlockList"][].contentUdi` (string UDI) | `.contentKey` (GUID) |
| `…settingsUdi` | `.settingsKey` |
| `contentData[].udi` | `contentData[].key` (GUID) |
| property values flattened onto the `contentData` entry | `contentData[].values[]` of `{ alias, culture, segment, value, editorAlias }` |
| nested block list = JSON-**encoded string** | nested block list = nested JSON **object** |

Consequences for the public model:

- `UnusedMediaBlockListLayoutItem.ContentUdi`/`SettingsUdi` (`string`) → `ContentKey` (`Guid`) / `SettingsKey` (`Guid?`); same on `UnusedMediaBlockListItem`.
- `UnusedMediaBlockListContentData.Udi` (`string`) → `Key` (`Guid`), and a new `EditorAliases` dictionary exposes the per-property `editorAlias` that Umbraco 14 added.
- Nested block detection no longer sniffs the `{"layout":{"Umbraco.BlockList":` string prefix; it checks for a nested `layout` object.
- A layout item pointing at missing `contentData` now **skips that block** instead of throwing `WtfException`. A single corrupt property should not be able to hide every unused media item from the report.

`Models/BlockList/UnusedMediaBlockListUtils.cs` (already `[Obsolete]`) was **deleted** — its static methods take `JObject` and cannot survive the System.Text.Json move.

## 6. Published cache access

Management API requests do **not** come with an ambient `IUmbracoContext` the way the old `/umbraco/backoffice/` routes did. Both places that read the published caches now ensure one explicitly:

```csharp
using UmbracoContextReference contextReference = _umbracoContextFactory.EnsureUmbracoContext();
using IServiceScope scope = _serviceScopeFactory.CreateScope();
IPublishedContentQuery query = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();
```

- `UnusedMediaBackOfficeHelper` — `IUmbracoContextAccessor` + `umbracoContext.Media.GetAtRoot()` → `IUmbracoContextFactory` + `IPublishedContentQuery.MediaAtRoot()`. The `BjernerSaysNoException` guard on a null media cache is gone with it. Items are now materialized (`.ToList()`) *inside* the context scope, since building each item reads properties and URLs off the cache.
- `ContentCacheUsedMediaProvider` — gained an `IUmbracoContextFactory` dependency. The three-argument constructor is the primary one; the old two-argument `(IServiceScopeFactory, ILogger<>)` signature is kept as a convenience overload that resolves the factory from `StaticServiceProvider`, so existing subclasses still compile.

Other API swaps:

- `parent.Children` → `parent.Children()` (the property is obsolete and scheduled for removal in Umbraco 18).
- `Skybrud.Essentials.Collections.Extensions` → `Skybrud.Essentials.Collections.Enumerables.Extensions` (obsolete namespace).
- `IPublishedContent.GetInt32("umbracoBytes")` (Skybrud.Essentials.Umbraco) → `Value<int>("umbracoBytes")`.
- `ExamineIndexes.MembersIndex` + `IExamineManager.GetRequiredIndex(...)` (Skybrud.Essentials.Umbraco) → `Constants.UmbracoIndexes.MembersIndexName` + `IExamineManager.TryGetIndex(...)`.
- `UmbracoEntityTypes.Member` (Skybrud.Essentials.Umbraco) → `Constants.UdiEntityType.Member`.

The scanning logic itself — providers report *used* media keys, "unused" is the set difference, results cached per provider for 10 minutes in `AppCaches.RuntimeCache` — is unchanged.

## 7. Filters: Limbo.Forms → own models

**New** `Models/Filters/`: `UnusedMediaFilter` (abstract), `UnusedMediaTextFilter`, `UnusedMediaDropDownFilter`, `UnusedMediaFilterItem`.

`UnusedMediaBackOfficeHelper.CreateFilters` now returns `List<UnusedMediaFilter>` instead of `List<FieldBase>`; `AppendTextFilter`, `AppendFoldersFilters`, `AppendCreatorsAndWritersFilters` and `AppendChildren` changed signatures accordingly. All remain `virtual`.

Each filter and item carries both a `labelKey`/`placeholderKey` (localization key) and a plain-text fallback — see §8.

## 8. Localization moved to the client

The new backoffice does not read `App_Plugins/*/Lang/*.xml`, and `ILocalizedTextService` is a server-side service with no path into the new UI. Changes:

- Deleted `wwwroot/Lang/en-US.xml` and `wwwroot/Lang/da-DK.xml`.
- Added `wwwroot/Lang/en.js` and `wwwroot/Lang/da-dk.js` (ES modules exporting a `unusedMedia` key group), registered as `localization` extensions in `umbraco-package.json`.
- `ILocalizedTextService` and the `Localize(...)` helpers were removed from `UnusedMediaBackOfficeHelper`/`UnusedMediaBackOfficeHelperDependencies`.
- The API now returns **stable aliases plus a localization key and an English fallback** (`UnusedMediaColumn.NameKey`, `UnusedMediaFilter.LabelKey`), and the dashboard resolves them via `this.localize.term(...)`.

A side benefit: the Danish strings that were hard-coded in the old dashboard element ("Er du sikker på…", "af … medier …") are now proper localization keys in both languages.

## 9. Backoffice frontend rewritten

Deleted:

- `wwwroot/Views/Dashboard.html` + `wwwroot/Scripts/DashboardController.js` — the AngularJS shim that hosted the custom element.
- `wwwroot/Scripts/External/lit-all.min.js` — the vendored copy of lit.
- `wwwroot/Scripts/UnusedMediaService.js`, `wwwroot/Scripts/Events/Index.js`, `wwwroot/Scripts/DashboardElement.js`.

Added `wwwroot/Scripts/dashboard.js`, a single ES module. Feature parity with the old dashboard (filters, stats, report list with per-provider rescan, sortable table, row selection, single + bulk trash, pagination) with these implementation changes:

| Before | After |
|---|---|
| server-generated import map + vendored lit (`@limbo/unused-media/lit`) | bare `@umbraco-cms/backoffice/external/lit`, resolved by the backoffice's own import map |
| `LitElement` | `UmbElementMixin(LitElement)` — gives `this.localize` and `this.consumeContext` |
| hand-rolled `fetch` wrapper + manual `)]}'` XSSI strip | `umbHttpClient` + `tryExecute` from `@umbraco-cms/backoffice/resources` (handles bearer token, base URL, refresh) |
| `angular.element(document).injector()` for `localizationService`, `notificationsService`, `overlayService` | `UMB_NOTIFICATION_CONTEXT` and `umbConfirmModal` |
| `UnusedMediaDashboardLoadEvent` window event for third-party customization | *removed* — it relied on the AngularJS bootstrap ordering |
| media link `/umbraco/#/media/media/edit/{id}` | `/umbraco/section/media/workspace/media/edit/{key}` |
| SHA1 cache buster appended to every asset URL | none needed — assets are versioned static web assets |

There is still **no npm/bundler step**: the file is hand-written ES module JavaScript shipped as a static web asset, matching how this repo already worked. That deliberately skips the generated OpenAPI client (which would require a Vite/TypeScript toolchain and a running site to generate against); `umbHttpClient` provides the same authentication guarantees — *provided each request declares `security`*, see below.

### Fixed after first install: requests must declare `security`

The first build of the dashboard logged the user out as soon as the dashboard opened:

```
GET /umbraco/management/api/v1/unused-media/config 401 (Unauthorized)
[Interceptor] 401 Unauthorized - queuing request for re-authentication
[UmbAppAuthController] Authorization timed out, starting authorization flow
```
```
[INF] The response was successfully returned as a challenge response:
      { "error": "missing_token", "error_description": "The security token is missing." }
```

Cause: the backoffice HTTP client is a [hey-api](https://heyapi.dev/) fetch client, and it only applies the access token when the request options carry a `security` key:

```js
c.security && await applyAuth({ ...c, security: c.security });
```

Every operation in Umbraco's own generated client passes `security: [{ scheme: "bearer", type: "http" }]`. The hand-written calls here omitted it (the official `umbHttpClient.get` docs example omits it too), so requests went out with **no `Authorization` header**. The API returned 401, and the backoffice's 401 interceptor interprets any 401 as an expired session — so it tore down the session and restarted the OpenID authorization flow.

Fix: `dashboard.js` now routes every call through local `apiGet`/`apiPost` wrappers that always set `security`. Direct `umbHttpClient` calls are not used.

## 10. Verification status

- ✅ `dotnet build` — succeeds, **0 errors, 0 warnings**.
- ✅ `dotnet pack` (Release) — produces `Limbo.Umbraco.UnusedMedia.17.0.0.nupkg` with exactly three dependencies (`Skybrud.Essentials 1.1.68`, `Umbraco.Cms.Api.Management [17.0.0, 17.9.9)`, `Umbraco.Cms.Web.Website [17.0.0, 17.9.9)`) and the static web assets (`umbraco-package.json`, `Scripts/dashboard.js`, `Lang/*.js`) in the right places.
- ⚠️ **Not runtime-tested.** This repo contains only the package — there is no Umbraco site to install it into, and no test project. Everything below is compile-verified but not exercised against a running backoffice.

### Worth checking first in a real install

1. **Media deep link** — `/umbraco/section/media/workspace/media/edit/{key}` follows the v14+ `section/{section}/workspace/{entityType}/edit/{unique}` convention but was not confirmed against a running backoffice.
2. **Block list parsing** — the new format was implemented from the Management API documentation. The default `AppendMediaKeys(UnusedMediaBlockListItem, …)` is a no-op, so this only affects solutions that subclass `ContentCacheUsedMediaProvider`; the regex sweep over the raw property value (which finds media either way) is untouched.
3. **`umbConfirmModal` rejection** — the confirm modal rejects on cancel; the current code awaits it inside an `async` handler, so a cancel produces an unhandled rejection in the console. Harmless, but worth wrapping if it shows up in logs.
4. **Scan cost** — `ContentCacheUsedMediaProvider` still walks the entire published content tree synchronously inside a request. Unchanged from v13, but the Management API has different timeout characteristics.

## 11. Breaking changes for consumers

Beyond the Umbraco version itself:

- All backoffice endpoint URLs changed (§3).
- `EssentialsTime` → `DateTimeOffset` on the report models.
- `CreateFilters` and friends return `UnusedMediaFilter` instead of Limbo.Forms `FieldBase`.
- Block list model properties renamed from UDI-based to key-based (§5).
- `UnusedMediaBlockListUtils` deleted.
- `UnusedMediaBackOfficeHelper.GetCacheBuster()`, `GetServerVariables()` and `Localize(...)` deleted.
- `UnusedMediaBackOfficeHelperDependencies` constructor signature changed.
- `Dashboard:AllowedGroups` is now enforced server-side (§2).
- `Dashboard:ElementName` is no longer read by the server; to swap in a custom dashboard element, override the `elementName` in your own `umbraco-package.json` extension or exclude ours by alias (`Limbo.UnusedMedia.Dashboard`).

Configuration keys (`Limbo:UnusedMedia:Dashboard:AllowedGroups`, `:PerPage`, `Limbo:UnusedMedia:IgnoredFolderIds`) are otherwise unchanged, and the `UsedMediaProvider` extensibility model (`builder.UnusedMedia().AddProvider<T>()` / `.ReplaceProvider<TOld, TNew>()`) is untouched.
