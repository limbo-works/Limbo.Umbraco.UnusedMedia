using Umbraco.Cms.Core;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Limbo.Umbraco.UnusedMedia.Helpers;

public class SqlHelper {
    private readonly IScopeProvider _scopeProvider;

    public SqlHelper(IScopeProvider scopeProvider) {
        _scopeProvider = scopeProvider;
    }

    public List<Guid> GetAllMediaGuids() {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("uniqueId")
            .From("umbracoNode")
            .Where("nodeObjectType = @0", Constants.ObjectTypes.Media);
        return scope.Database.Fetch<Guid>(sql);
    }

    public List<Guid> GetUsedMediaGuidsFromRelations() {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("DISTINCT node.uniqueId")
            .From("umbracoRelation rel")
            .InnerJoin("umbracoNode node").On("rel.childId = node.id")
            .InnerJoin("umbracoRelationType rt").On("rel.relType = rt.id")
            .Where("rt.alias = 'umbMedia'");
        return scope.Database.Fetch<Guid>(sql);
    }

    public bool IsMediaUsedInRelations(Guid mediaGuid) {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("COUNT(*)")
            .From("umbracoRelation rel")
            .InnerJoin("umbracoNode node").On("rel.childId = node.id")
            .InnerJoin("umbracoRelationType rt").On("rel.relType = rt.id")
            .Where("rt.alias = 'umbMedia' AND node.uniqueId = @0", mediaGuid);
        return scope.Database.ExecuteScalar<int>(sql) > 0;
    }
}