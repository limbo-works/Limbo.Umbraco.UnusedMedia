// [CHANGE: Umbraco 17 upgrade - dropped ILocalizedTextService/IUmbracoContextAccessor, added IUmbracoContextFactory + IServiceScopeFactory] Related: see documentation/UMBRACO-17-UPGRADE.md for the full list of changed files.

using Examine;
using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Limbo.Umbraco.UnusedMedia.Helpers;

public class UnusedMediaBackOfficeHelperDependencies {

    private readonly IOptions<UnusedMediaSettings> _settings;

    public IRuntimeState RuntimeState { get; }

    public UnusedMediaSettings Settings => _settings.Value;

    public IUserService UserService { get; }

    public UnusedMediaService UnusedMediaService { get; }

    public IExamineManager ExamineManager { get; }

    /// <summary>
    /// Gets the factory used for ensuring an <see cref="IUmbracoContext"/>. Unlike the Umbraco 13 backoffice,
    /// Management API requests don't come with an ambient Umbraco context, so one has to be ensured explicitly
    /// before the published caches can be queried.
    /// </summary>
    public IUmbracoContextFactory UmbracoContextFactory { get; }

    /// <summary>
    /// Gets the factory used for resolving scoped services such as <c>IPublishedContentQuery</c>.
    /// </summary>
    public IServiceScopeFactory ServiceScopeFactory { get; }

    public UnusedMediaBackOfficeHelperDependencies(IRuntimeState runtimeState, IOptions<UnusedMediaSettings> settings, IUserService userService, UnusedMediaService unusedMediaService, IExamineManager examineManager, IUmbracoContextFactory umbracoContextFactory, IServiceScopeFactory serviceScopeFactory) {
        _settings = settings;
        RuntimeState = runtimeState;
        UserService = userService;
        UnusedMediaService = unusedMediaService;
        ExamineManager = examineManager;
        UmbracoContextFactory = umbracoContextFactory;
        ServiceScopeFactory = serviceScopeFactory;
    }

}
