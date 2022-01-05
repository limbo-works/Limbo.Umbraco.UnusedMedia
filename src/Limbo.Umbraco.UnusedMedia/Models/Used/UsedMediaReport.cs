using Skybrud.Essentials.Time;
using Umbraco.Core.Models.PublishedContent;

namespace Limbo.Umbraco.UnusedMedia.Models.Used {
    
    public class UsedMediaReport : IUsedMediaReport {
        
        private readonly ContentCacheUsedMediaReport _report;

        public EssentialsTime CreateDate { get; }

        public UsedMediaReport(ContentCacheUsedMediaReport report) {
            _report = report;
            CreateDate = report.Completed;
        }

        /// <summary>
        /// Returns whether the specified <paramref name="media"/> is currently in use, according to the report.
        /// </summary>
        /// <param name="media">The media.</param>
        /// <returns><c>true</c> if <paramref name="media"/> is in use; otherwise <c>false</c>.</returns>
        public virtual bool IsInUse(IPublishedContent media) {
            return _report.Media.ContainsKey(media.Key);
        }

    }

}