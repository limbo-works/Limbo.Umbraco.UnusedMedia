using System;
using System.Collections.Generic;
using System.Reflection;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.PublishedCache;

namespace Limbo.Umbraco.UnusedMedia.Extensions {

    internal static class UnusedMediaExtensions {

        internal static IEnumerable<IPublishedContent> GetAll(this IPublishedMemberCache publishedMemberCache)  {

            // Get the method via reflection as the MemberCache class is internal
            MethodInfo method = publishedMemberCache.GetType().GetMethod("GetAtRoot");

            // Get all members from the member cache (which is really not cached)
            return (IEnumerable<IPublishedContent>) method?.Invoke(publishedMemberCache, new object[] { false }) ?? Array.Empty<IPublishedContent>();

        }

    }

}