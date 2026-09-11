using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Limbo.Umbraco.UnusedMedia.Api;

public class UnusedMediaSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions> {

    public void Configure(SwaggerGenOptions options) {

        options.SwaggerDoc(UnusedMediaApiConstants.Alias, new OpenApiInfo {
            Title = UnusedMediaApiConstants.Name,
            Version = UnusedMediaApiConstants.Version
        });

        options.OperationFilter<UnusedMediaSecurityFilter>();

    }

}