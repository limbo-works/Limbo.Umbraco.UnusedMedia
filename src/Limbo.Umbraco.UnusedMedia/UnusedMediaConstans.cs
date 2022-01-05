using Skybrud.Essentials.Reflection;

namespace Limbo.Umbraco.UnusedMedia {
    
    public static class UnusedMediaConstans {

        public static readonly string Version = ReflectionUtils.GetInformationalVersion(typeof(UnusedMediaConstans).Assembly);

        public static class Directories {

            public const string AppData = "~/App_Data/Limbo.Umbraco.UnusedMedia";

        }

        public static class Urls {

            public const string AppPlugins = "/App_Plugins/Limbo.Umbraco.UnusedMedia/";

        }

    }

}