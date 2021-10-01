using System.Collections.Generic;
using Newtonsoft.Json;

namespace Limbo.Umbraco.UnusedMedia.Models {
    
    public class UnusedMediaResult {

        [JsonProperty("total")]
        public int Total { get; }
        
        [JsonProperty("unused")]
        public int Unused { get; }
        
        [JsonProperty("unusedPercent")]
        public double UnusedPercent { get; }
        
        [JsonProperty("used")]
        public int Used { get; }
        
        [JsonProperty("usedPercent")]
        public double UsedPercent { get; }
        
        [JsonProperty("limit")]
        public int Limit { get; }
        
        [JsonProperty("offset")]
        public int Offset { get; }
        
        [JsonProperty("page")]
        public int Page { get; }
        
        [JsonProperty("pages")]
        public int Pages { get; }

        [JsonProperty("items")]
        public IEnumerable<UnusedMediaItem> Items { get; }
        
        public UnusedMediaResult(int total, int unused, int limit, int offset, int page, int pages, IEnumerable<UnusedMediaItem> items) {
            
            Total = total;
            Unused = unused;
            Limit = limit;
            Offset = offset;
            Page = page;
            Pages = pages;
            Used = total - unused;
            Items = items;
            
            UnusedPercent = total == 0 ? -1 : unused / (double) total * 100;
            UsedPercent = total == 0 ? -1 : Used / (double) total * 100;

        }

    }

}