namespace Limbo.Umbraco.UnusedMedia.Services {
    
    public class UnusedMediaOptions {

        /// <summary>
        /// Gets or sets one or more IDs which the returned media should have in it's path.
        /// </summary>
        public int[] Path { get; set; }

        /// <summary>
        /// Gets or sets a text based query the returned results should match.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets an array of creator IDs the returned results should match.
        /// </summary>
        public int[] CreatorIds { get; set; }

        /// <summary>
        /// Gets or sets an array of writer IDs the returned results should match.
        /// </summary>
        public int[] WriterIds { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount of results to be returned.
        /// </summary>
        public int Limit { get; set; }
        
        /// <summary>
        /// Gets or sets the page to be returned.
        /// </summary>
        public int Page { get; set; }

        public UnusedMediaOptions() {
            Limit = 15;
            Page = 1;
        }

    }

}