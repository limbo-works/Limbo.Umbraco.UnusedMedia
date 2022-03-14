using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Skybrud.Essentials.Time;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace Limbo.Umbraco.UnusedMedia.Models.Used {
    
    /// <summary>
    /// Class representing a report over media in use in the member cache at the time the report was generated.
    /// </summary>
    public class MemberCacheUsedMediaReport : IUsedMediaReport {

        #region Properties

        /// <summary>
        /// Gets a timestamp when generation of the report started.
        /// </summary>
        [JsonProperty("start")]
        public EssentialsTime Start { get; }

        /// <summary>
        /// Gets a timestamp when generation of the report was completed.
        /// </summary>
        [JsonProperty("completed")]
        public EssentialsTime Completed { get; }
        
        [JsonProperty("createDate")]
        public EssentialsTime CreateDate => Completed;
        
        /// <summary>
        /// Gets the time it took to generate the report.
        /// </summary>
        [JsonProperty("duration")]
        [JsonConverter(typeof(Skybrud.Essentials.Json.Converters.Time.TimeSpanSecondsConverter))]
        public TimeSpan Duration { get; }
        
        /// <summary>
        /// Gets a dictionary over the media in use, and which member is referring to it.
        /// </summary>
        [JsonProperty("media")]
        public Dictionary<Guid, HashSet<Guid>> Media { get; }

        #endregion

        #region Constructors

        public MemberCacheUsedMediaReport(EssentialsTime start, EssentialsTime completed, TimeSpan duration, Dictionary<Guid, HashSet<Guid>> media) {
            Start = start;
            Completed = completed;
            Duration = duration;
            Media = media;
        }

        #endregion

        #region Member methods

        /// <summary>
        /// Returns an array with the keys of the content nodes referring to the specifed <paramref name="media"/>.
        /// </summary>
        /// <param name="media">The media.</param>
        /// <returns>An instance of <see cref="HashSet{Guid}"/> representing the content keys.</returns>
        public HashSet<Guid> GetContentKeys(IMedia media) {
            if (media == null) throw new ArgumentNullException(nameof(media));
            return GetContentKeys(media.Key);
        }
        
        /// <summary>
        /// Returns an array with the keys of the content nodes referring to the media with the specified <paramref name="mediaKey"/>.
        /// </summary>
        /// <param name="mediaKey">The key of the media.</param>
        /// <returns>An instance of <see cref="HashSet{Guid}"/> representing the content keys.</returns>
        public HashSet<Guid> GetContentKeys(Guid mediaKey) {
            return Media.TryGetValue(mediaKey, out HashSet<Guid> result) ? result : new HashSet<Guid>();
        }

        /// <summary>
        /// Returns whether the specified <paramref name="media"/> is currently in use, according to the report.
        /// </summary>
        /// <param name="media">The media.</param>
        /// <returns><c>true</c> if <paramref name="media"/> is in use; otherwise <c>false</c>.</returns>
        public virtual bool IsInUse(IPublishedContent media) {
            return Media.ContainsKey(media.Key);
        }

        #endregion

    }

}