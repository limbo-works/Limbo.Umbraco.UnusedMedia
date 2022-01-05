using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models.References {
    
    public class ReferenceResult {

        /// <summary>
        /// Gets or sets whether a toggle should be shown in the delete dialog. If set to <c>true</c>, deletion will
        /// not be possible until the user has activated the toggle.
        /// </summary>
        [JsonProperty("showToggle")]
        public bool ShowToggle { get; set; }

        /// <summary>
        /// Gets or sets whether deleting the selected item should be allowed. If set to <c>false</c>, the delete
        /// dialog will prevent the user from deleting the selected item, and will show a warning with
        /// <see cref="NotAllowedMessage"/>.
        /// </summary>
        [JsonProperty("allowDelete")]
        public bool AllowDelete { get; set; }

        /// <summary>
        /// Gets or sets the message of the alert shown when <see cref="AllowDelete"/> is set to <c>true</c>.
        /// </summary>
        [JsonProperty("notAllowedMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string NotAllowedMessage { get; set; }

        /// <summary>
        /// Gets the total amount of reference found for the selected item.
        /// </summary>
        [JsonProperty("total")]
        public int Total => Groups.Count == 0 ? 0 : Groups.Sum(x => x.References.Length);

        /// <summary>
        /// Gets a grouped list of reference found for the selected item.
        /// </summary>
        [JsonProperty("groups")]
        public List<ReferenceGroup> Groups { get; }

        public ReferenceResult(IEnumerable<ReferenceGroup> groups) {
            AllowDelete = true;
            Groups = groups.ToList();
        }

    }

}