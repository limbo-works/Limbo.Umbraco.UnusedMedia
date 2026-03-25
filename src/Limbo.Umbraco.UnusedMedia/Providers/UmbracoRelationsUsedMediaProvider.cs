using Limbo.Umbraco.UnusedMedia.Helpers;

namespace Limbo.Umbraco.UnusedMedia.Providers;

public class UmbracoRelationsUsedMediaProvider : UsedMediaProvider {

    private readonly SqlHelper _sqlHelper;

    public UmbracoRelationsUsedMediaProvider(SqlHelper sqlHelper) {
        _sqlHelper = sqlHelper;
    }

    public override HashSet<Guid> ScanForUsedMediaKeys() {
        return [.._sqlHelper.GetUsedMediaGuidsFromRelations()];
    }

}