using Newtonsoft.Json;
using Skybrud.Essentials.Time;

namespace Limbo.Umbraco.UnusedMedia.Models.Used {

    public class UserMediaReportSummary {

        [JsonProperty("createDate")]
        public EssentialsTime CreateDate { get; }

        public UserMediaReportSummary(IUsedMediaReport report) {
            CreateDate = report.CreateDate;
        }

    }

}