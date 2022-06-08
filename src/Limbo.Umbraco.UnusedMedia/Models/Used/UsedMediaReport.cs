using System.Collections.Generic;
using System.Linq;
using Skybrud.Essentials.Time;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Limbo.Umbraco.UnusedMedia.Models.Used {

    public class UsedMediaReport : IUsedMediaReport {

        private readonly List<IUsedMediaReport> _reports = new();

        #region Properties

        /// <summary>
        /// Returns the create date of the report. If this report wraps multiple reports internally, <c>null</c> will
        /// be returned insetad as a common create date can't be determined.
        /// </summary>
        public EssentialsTime CreateDate {
            get {
                EssentialsTime[] createDates = _reports.Select(x => x.CreateDate).ToArray();
                return createDates.Length == 1 ? createDates[0] : null;
            }
        }

        #endregion

        #region Constructors

        public UsedMediaReport(IUsedMediaReport report) {
            _reports.Add(report);
        }

        public UsedMediaReport(params IUsedMediaReport[] reports) {
            _reports.AddRange(reports);
        }

        public UsedMediaReport(IEnumerable<IUsedMediaReport> reports) {
            _reports.AddRange(reports);
        }

        #endregion

        #region Member methods

        /// <summary>
        /// Returns whether the specified <paramref name="media"/> is currently in use, according to the report.
        /// </summary>
        /// <param name="media">The media.</param>
        /// <returns><c>true</c> if <paramref name="media"/> is in use; otherwise <c>false</c>.</returns>
        public virtual bool IsInUse(IPublishedContent media) {
            return _reports.Any(x => x.IsInUse(media));
        }

        #endregion

    }

}