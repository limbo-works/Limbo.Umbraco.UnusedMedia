using System.Collections.Generic;
using System.Linq;
using Limbo.Umbraco.UnusedMedia.Models.References;
using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models.Api {
    
    public class DeleteMediaResponse {
        
        /// <summary>
        /// Gets or sets whether the user is allowed to delete (trash) a given media. If set to <c>false</c>, the
        /// confirm button will be disabled in the UI. Default is <c>true</c>.
        /// </summary>
        [JsonProperty("allowDelete")]
        public bool AllowDelete { get; set; }
        
        /// <summary>
        /// Gets or sets whether the user should be prompted with a <strong>I know what I'm doing</strong> toggle
        /// before being able to click the confirm button. The toggle is only shown when <see cref="AllowDelete"/> is
        /// also <c>true</c>. Default is <c>false</c>.
        /// </summary>
        [JsonProperty("showToggle")]
        public bool ShowToggle { get; set; }

        /// <summary>
        /// Gets the total amount of references found for the media in question.
        /// </summary>
        [JsonProperty("total")]
        public int Total => Groups.Count == 0 ? 0 : Groups.Sum(x => x.References.Length);

        /// <summary>
        /// Gets the groups for the different types of references.
        /// </summary>
        [JsonProperty("groups")]
        public List<ReferenceGroup> Groups { get; }

        public DeleteMediaResponse(ReferenceResult result) {
            AllowDelete = true;
            Groups = result.Groups;
        }

    }

}