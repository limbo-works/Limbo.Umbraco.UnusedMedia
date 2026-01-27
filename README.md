# Limbo.Umbraco.UnusedMedia

A powerful Umbraco package designed to help content editors and administrators identify and manage unused media items within their Umbraco installation. This tool provides a dedicated dashboard in the Umbraco backoffice to scan for media that is not referenced in content properties or redirects, helping to keep your media library clean and optimize site performance.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage (Backoffice Dashboard)](#usage-backoffice-dashboard)
- [Technical Details](#technical-details)
- [Extensibility](#extensibility)

## Features

- **Scan for Unused Media**: Initiates a comprehensive scan of your Umbraco content and redirects to identify media files that are not actively in use.

- **Recursive Folder Protection**: Automatically detects if a media folder is in use (e.g., selected in a Media Picker). If a folder is used, all media items and subfolders within it are implicitly considered "used" and protected from deletion.

- **Progress Reporting**: Provides real-time updates on the scanning process directly within the dashboard.

- **Detailed Report**: Displays a list of potentially unused media items, including their name, last update date, and size.

- **Direct Deletion**: Allows for direct deletion of identified unused media items from the dashboard.

- **Clear Scan Data**: Option to clear the stored scan results.

- **Extensible Providers**: Designed with extensibility in mind, allowing developers to add custom logic for determining if a media item is "used."

## Installation

The package can be installed via NuGet Package Manager.

### .NET CLI
```bash
dotnet add package Limbo.Umbraco.UnusedMedia
```

### NuGet Package Manager
```powershell
Install-Package Limbo.Umbraco.UnusedMedia
```

## Configuration

Currently, this package is designed to work out-of-the-box with minimal configuration. All settings are managed internally or via dependency injection. Future versions might introduce `appsettings.json` options for fine-tuning scan behavior.

## Usage (Backoffice Dashboard)

Once installed, a new dashboard will be available in the Umbraco backoffice:

1. Navigate to the **Settings** section in the Umbraco backoffice.
2. Locate and click on the **Unused Media** dashboard.

### Dashboard Elements

#### Scan Date
Displays the date and time of the latest completed scan.

#### Media Count
Shows the number of media items found in the last scan.

#### Media Folders Scanned
*(Currently not actively calculated, will be enhanced in future versions if needed for detailed reporting).*

#### Scan Media Button
- Click this button to start a new scan for unused media.
- The button will show "Scanning media..." during the process and become disabled.

#### Clear Scan Button
Clears the current scan results displayed in the dashboard.

#### Progress Section
*Visible during scan:*

- **Progress Bar**: Visual representation of the scan's progress.
- **Processed**: Shows the number of media items processed versus the total.
- **Log Container**: Displays a running log of processed media items and any errors encountered during the scan.

#### Unused Media Table
Lists all media items identified as unused:

- **Name**: The name of the media item, with a link to open it in the media section.
- **Last Updated**: The date and time the media item was last updated.
- **Size**: The file size of the media item.
- **Delete Button**: Allows you to delete the specific media item directly from the report. A confirmation overlay will appear before deletion.

## Technical Details

The Limbo.Umbraco.UnusedMedia package leverages Umbraco's core services and modern .NET features to provide its functionality. It employs a robust scanning algorithm to ensure no active media is accidentally flagged.

### Scanning Logic

The scan process operates in two distinct passes to handle complex usage scenarios (such as used folders):

1. **Explicit Usage Pass**: The service gathers all media items directly referenced in content, redirects, or relations.

2. **Implicit Usage Pass**: The service analyzes the paths of the explicitly used items. If a folder is found to be in use, the service calculates the paths of all its descendants. Any media file residing within a "used" folder is then marked as implicitly used and removed from the cleanup list.

### Key Components

#### `UnusedMediaBackOfficeController.cs`
- An `UmbracoAuthorizedApiController` that exposes REST endpoints for the backoffice dashboard.
- Handles requests for starting/stopping scans, retrieving scan status, fetching unused media reports, and deleting media.

#### `UnusedMediaService.cs`
- The core service responsible for orchestrating the unused media scan.
- Registered as a **Singleton** in the Dependency Injection container.
- **Logic Update**: Implements the two-pass scanning strategy. It maintains a list of `usedFolderPaths` to protect files inside used folders.
- Uses `IBackgroundTaskQueue` and `IServiceScopeFactory` to safely consume scoped services (like `IPublishedContentQuery`) within its singleton lifetime.
- Manages the `UnusedMediaScanStatus` for progress reporting.

#### `DeepScanProvider.cs`
- A provider that scans all content properties in Umbraco for references to media UDIs.
- It uses `IPublishedContentQuery` to traverse the content tree. To prevent DI lifetime mismatches, `IPublishedContentQuery` is resolved from a new service scope created via `IServiceScopeFactory`.
- Exposes a `GetUsedMediaUdis()` method to return the full set of used IDs for batch processing in the service.
- Employs a `Lazy<HashSet<string>>` to cache the list of used media UDIs after the initial scan.

#### `RedirectsProvider.cs`
- A provider that scans redirects (specifically from the "Skybrud.Umbraco.Redirects" package's `SkybrudRedirects` table) for media UDIs referenced in destination URLs.
- Uses `IScopeProvider` for database access.
- Exposes a `GetUsedMediaUdis()` method to return the full set of used IDs for batch processing in the service.
- Also uses a `Lazy<HashSet<string>>` for caching used media UDIs.

#### `SqlHelper.cs`
- Provides direct SQL access for querying `umbracoNode` and `umbracoRelation` tables to efficiently retrieve all media GUIDs and check media usage in relations.
- Utilizes `IScopeProvider` for safe database operations.

#### `UnusedMediaScanStatus.cs`
A model to track the progress and status of a background scan task, including processed items and errors.

#### `UnusedMediaReport.cs`
A model representing the result of an unused media scan, containing a list of `UnusedMediaItems` and metadata about the scan.

#### `UnusedMediaItem.cs`
A model representing a single unused media item, including its ID, name, update date, and size.

#### Background Task Infrastructure
- **`IBackgroundTaskQueue.cs`**, **`BackgroundTaskQueue.cs`**, **`QueuedHostedService.cs`**: Implement a generic background task queuing mechanism, allowing `UnusedMediaService` to offload long-running scan operations to a background thread without blocking the UI.

#### `MediaCleanupBackgroundService.cs`
A recurring hosted service that periodically triggers a scan for unused media by calling `UnusedMediaService.StartScan()`.

### Dependency Injection (DI)

The package is fully integrated with Umbraco's DI container. Services are registered in `UnusedMediaComposer.cs`. Special care is taken to handle lifetime mismatches (e.g., singleton services consuming scoped dependencies) by using `IServiceScopeFactory` to create temporary scopes when needed.

## Extensibility

The architecture allows for easy extension:

### Adding New Usage Providers
You can create new classes (e.g., `MyCustomUsageProvider.cs`) that implement logic to determine if a media item is used (e.g., checking custom properties, external systems). Register your new provider as a **Transient** service in `UnusedMediaComposer.cs` and inject it into `UnusedMediaService.cs` to incorporate its logic into the overall scan.

### Customizing Scan Behavior
The `UnusedMediaService` can be extended or replaced via DI to alter how the scan is performed or how results are processed.

---

*This README aims to provide a thorough understanding of the Limbo.Umbraco.UnusedMedia package.*