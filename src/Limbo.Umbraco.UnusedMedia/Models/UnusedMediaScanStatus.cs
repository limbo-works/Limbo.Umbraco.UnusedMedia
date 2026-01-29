using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models;

/// <summary>
/// Represents the current status of an unused media scan background task.
/// </summary>
public class UnusedMediaScanStatus {

    /// <summary>
    /// Gets or sets the ID of the task.
    /// </summary>
    [JsonProperty("taskId")]
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the current status (e.g., "Queued", "In Progress", "Completed", "Failed").
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current progress as a percentage or count.
    /// </summary>
    [JsonProperty("progress")]
    public long Progress { get; set; }

    /// <summary>
    /// Gets or sets the total number of items to process.
    /// </summary>
    [JsonProperty("total")]
    public long Total { get; set; }

    /// <summary>
    /// Gets or sets a message related to the status (e.g., error message).
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets a list of media items that have been processed.
    /// </summary>
    [JsonProperty("processedMedia")]
    public List<string> ProcessedMedia { get; set; } = new();

    /// <summary>
    /// Gets a list of errors encountered during the process.
    /// </summary>
    [JsonProperty("errors")]
    public List<string> Errors { get; set; } = new();

}
