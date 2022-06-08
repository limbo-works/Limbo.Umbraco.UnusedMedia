using System;
using System.Linq;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Limbo.Umbraco.UnusedMedia.Models {

    public class UnusedMediaItem {

        [JsonProperty("id")]
        public int Id { get; }

        [JsonProperty("key")]
        public Guid Key { get; }

        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("url")]
        public string Url { get; }

        [JsonProperty("path")]
        public string[] Path { get; }

        [JsonProperty("creatorId")]
        public int CreatorId { get; }

        [JsonProperty("creatorName")]
        public string CreatorName { get; }

        [JsonProperty("writerId")]
        public int WriterId { get; }

        [JsonProperty("writerName")]
        public string WriterName { get; }

        public UnusedMediaItem(IPublishedContent media) {
            Id = media.Id;
            Key = media.Key;
            Name = media.Name;
            Url = media.Url();
            Path = media.Ancestors().Select(x => x.Name).Reverse().ToArray();
            CreatorId = media.CreatorId;
            CreatorName = media.CreatorName();
            WriterId = media.WriterId;
            WriterName = media.WriterName();
        }

    }

}