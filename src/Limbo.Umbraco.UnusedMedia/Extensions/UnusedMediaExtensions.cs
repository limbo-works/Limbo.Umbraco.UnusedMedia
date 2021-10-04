using System;
using System.Collections.Generic;
using System.Linq;

namespace Limbo.Umbraco.UnusedMedia.Extensions {

    internal static class UnusedMediaExtensions {

        public static HashSet<TOut> ToHashSet<TIn, TOut>(this IEnumerable<TIn> collection, Func<TIn, TOut> callback) {
            // TODO: Use extension method from Skybrud.Essentials once v1.1.31 is released
            return new HashSet<TOut>(collection.Select(callback));
        }

    }

}