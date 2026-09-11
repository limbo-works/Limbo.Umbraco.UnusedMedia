using Umbraco.Cms.Api.Management.OpenApi;

namespace Limbo.Umbraco.UnusedMedia.Api;

internal class UnusedMediaSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase {

    protected override string ApiName => UnusedMediaApiConstants.Name;

}