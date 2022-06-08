using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models.References {

    public class ReferenceGroup {

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("view")]
        public string View { get; set; }

        [JsonProperty("references")]
        public object[] References { get; set; }

    }

}