using Skybrud.Essentials.Time;
using Umbraco.Core.Models.PublishedContent;

namespace Limbo.Umbraco.UnusedMedia.Models.Used {
    
    /// <summary>
    /// Interface representing a report over media that is in use.
    /// </summary>
    public interface IUsedMediaReport {

        /// <summary>
        /// Gets a timestamp for when the report was generated.
        /// </summary>
        public EssentialsTime CreateDate { get; }

        /// <summary>
        /// Returns whether the specified <paramref name="media"/> is currently in use, according to the report.
        /// </summary>
        /// <param name="media">The media.</param>
        /// <returns><c>true</c> if <paramref name="media"/> is in use; otherwise <c>false</c>.</returns>
        public bool IsInUse(IPublishedContent media);

    }

}