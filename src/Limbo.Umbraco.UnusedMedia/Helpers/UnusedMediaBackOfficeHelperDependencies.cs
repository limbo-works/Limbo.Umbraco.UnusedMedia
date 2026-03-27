using Examine;
using Limbo.Umbraco.UnusedMedia.Models.Settings;
using Limbo.Umbraco.UnusedMedia.Services;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Limbo.Umbraco.UnusedMedia.Helpers;

public class UnusedMediaBackOfficeHelperDependencies {

    private readonly IOptions<UnusedMediaSettings> _settings;

    public IRuntimeState RuntimeState { get; }

    public UnusedMediaSettings Settings => _settings.Value;

    public ILocalizedTextService LocalizedTextService { get; }

    public IUserService UserService { get; }

    public UnusedMediaService UnusedMediaService { get; }

    public IExamineManager ExamineManager { get; }

    public IUmbracoContextAccessor UmbracoContextAccessor { get; }

    public UnusedMediaBackOfficeHelperDependencies(IRuntimeState runtimeState, IOptions<UnusedMediaSettings> settings, ILocalizedTextService localizedTextService, IUserService userService, UnusedMediaService unusedMediaService, IExamineManager examineManager, IUmbracoContextAccessor umbracoContextAccessor) {
        _settings = settings;
        RuntimeState = runtimeState;
        LocalizedTextService = localizedTextService;
        UserService = userService;
        UnusedMediaService = unusedMediaService;
        ExamineManager = examineManager;
        UmbracoContextAccessor = umbracoContextAccessor;
    }

}