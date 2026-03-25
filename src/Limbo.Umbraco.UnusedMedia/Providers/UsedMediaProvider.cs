namespace Limbo.Umbraco.UnusedMedia.Providers;

public abstract class UsedMediaProvider {

    public abstract HashSet<Guid> ScanForUsedMediaKeys();

}