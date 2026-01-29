using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Limbo.Umbraco.UnusedMedia.Helpers;

public class SqlHelper {
    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<SqlHelper> _logger;

    public SqlHelper(IScopeProvider scopeProvider, ILogger<SqlHelper> logger) {
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    public List<Guid> GetAllMediaGuids() {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("uniqueId")
            .From("umbracoNode")
            .Where("nodeObjectType = @0", Constants.ObjectTypes.Media);
        var guids = scope.Database.Fetch<Guid>(sql);
        _logger.LogInformation("Found {Count} total media items in database", guids.Count);
        return guids;
    }

    public List<Guid> GetUsedMediaGuidsFromRelations() {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("DISTINCT node.uniqueId")
            .From("umbracoRelation rel")
            .InnerJoin("umbracoNode node").On("rel.childId = node.id")
            .Where("node.nodeObjectType = @0", Constants.ObjectTypes.Media);
        var guids = scope.Database.Fetch<Guid>(sql);
        _logger.LogInformation("Found {Count} media items used in relations", guids.Count);
        return guids;
    }

    public bool IsMediaUsedInRelations(Guid mediaGuid) {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        // Check if this media is referenced as a child in ANY relation
        var sql = scope.SqlContext.Sql()
            .Select("COUNT(*)")
            .From("umbracoRelation rel")
            .InnerJoin("umbracoNode node").On("rel.childId = node.id")
            .Where("node.uniqueId = @0 AND node.nodeObjectType = @1", mediaGuid, Constants.ObjectTypes.Media);
        var count = scope.Database.ExecuteScalar<int>(sql);
        
        if (count > 0) {
            _logger.LogDebug("Media {MediaGuid} is used in {Count} relation(s)", mediaGuid, count);
        }
        
        return count > 0;
    }
}