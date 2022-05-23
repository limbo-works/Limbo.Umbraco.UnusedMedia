using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limbo.Umbraco.UnusedMedia.Constants
{
    public static class RegexPatterns
    {
        public const string MatchMediaPathPattern = @"(\/media\/[0-9A-Za-z]+\/.+?\.[a-z0-9]{1,10})";
        public const string MatchUmbracoMediaLink = @"(umb:\/\/media\/[0-9A-Fa-f]{8}[0-9A-Fa-f]{4}[0-9A-Fa-f]{4}[0-9A-Fa-f]{4}[0-9A-Fa-f]{12})";
        public const string MatchUmbracoContentLink = @"(umb:\/\/document\/[0-9A-Fa-f]{8}[0-9A-Fa-f]{4}[0-9A-Fa-f]{4}[0-9A-Fa-f]{4}[0-9A-Fa-f]{12})";
    }
}
