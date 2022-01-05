using System;
using Newtonsoft.Json;
using Skybrud.Essentials.Strings.Extensions;
using Skybrud.Essentials.Time;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace Limbo.Umbraco.UnusedMedia.Models.References {

    public class ContentReference {

        [JsonProperty("id")]
        public int Id { get; }
        
        [JsonProperty("key")]
        public Guid Key { get; }
        
        [JsonProperty("name")]
        public string Name { get; }
        
        [JsonProperty("url")]
        public string Url { get; }
        
        [JsonProperty("umbracoUrl")]
        public string UmbracoUrl { get; }
        
        [JsonProperty("updateDate")]
        public EssentialsTime UpdateDate { get; }

        public ContentReference(IPublishedContent content) {
            Id = content.Id;
            Key = content.Key;
            Name = content.Name;
            Url = content.Url();
            UpdateDate = content.UpdateDate;
            UmbracoUrl = $"/umbraco/#/{content.ItemType.ToLower()}/{content.ItemType.ToLower()}/edit/{content.Id}";
        }

    }

}