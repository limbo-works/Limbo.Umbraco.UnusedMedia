using System;
using System.Collections.Generic;
using System.Linq;

namespace Limbo.Umbraco.UnusedMedia.Extensions {
    
    public static class UnusedMediaExtensions {

        public static HashSet<TOut> ToHashSet<TIn, TOut>(this IEnumerable<TIn> collection, Func<TIn, TOut> callback) {
            return new HashSet<TOut>(collection.Select(callback));
        }

    }

}