using System.Collections.Generic;
using System.Linq;

namespace Limbo.Umbraco.UnusedMedia.Services {

    public class UnusedMediaOptions {

        private int[] _path;
        private HashSet<int> _pathHashSet;

        private int[] _creatorIds;
        private HashSet<int> _creatorIdsHashSet;

        private int[] _writerIds;
        private HashSet<int> _writerIdsHashSet;

        #region Properties

        /// <summary>
        /// Gets or sets a text based query the returned results should match.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets one or more IDs which the returned media should have in it's path.
        /// </summary>
        public int[] Path {
            get => _path;
            set {
                _path = value;
                _pathHashSet = value?.ToHashSet();
            }
        }

        public bool HasPath => Path != null && Path.Length > 0;

        /// <summary>
        /// Gets or sets an array of creator IDs the returned results should match.
        /// </summary>
        public int[] CreatorIds {
            get => _creatorIds;
            set {
                _creatorIds = value;
                _creatorIdsHashSet = value?.ToHashSet();
            }
        }

        public bool HasCreatorIds => CreatorIds != null && CreatorIds.Length > 0;

        /// <summary>
        /// Gets or sets an array of writer IDs the returned results should match.
        /// </summary>
        public int[] WriterIds {
            get => _writerIds;
            set {
                _writerIds = value;
                _writerIdsHashSet = value?.ToHashSet();
            }
        }

        public bool HasWriterIds => WriterIds != null && WriterIds.Length > 0;

        /// <summary>
        /// Gets or sets a collection of folder IDs that should be ignored. If one of these IDs are in the path of a
        /// given media, the media will be excluded in the list of unused media.
        /// </summary>
        public HashSet<int> IgnoredFolderIds { get; set; }

        /// <summary>
        /// Gets or sets whether to also generated a report for members referencing media. Default is <c>false</c>.
        /// </summary>
        public bool IncludeMembers { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount of results to be returned.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the page to be returned.
        /// </summary>
        public int Page { get; set; }

        #endregion

        #region Constructors

        public UnusedMediaOptions() {
            IgnoredFolderIds = new HashSet<int>();
            Limit = 15;
            Page = 1;
        }

        #endregion

        #region Member methods

        public bool IsInPath(int id) {
            return _pathHashSet?.Contains(id) ?? false;
        }

        public bool HasCreator(int id) {
            return _creatorIdsHashSet?.Contains(id) ?? false;
        }

        public bool HasWriter(int id) {
            return _writerIdsHashSet?.Contains(id) ?? false;
        }

        #endregion

    }

}