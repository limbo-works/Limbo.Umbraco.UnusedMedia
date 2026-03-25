using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limbo.Umbraco.UnusedMedia.Helpers;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Media.EmbedProviders;
using Umbraco.Cms.Core.Notifications;

namespace Limbo.Umbraco.UnusedMedia.Notifications.Handlers;

public class ServerVariablesParsingHandler : INotificationHandler<ServerVariablesParsingNotification> {

    private readonly UnusedMediaBackOfficeHelper _backoffice;

    public ServerVariablesParsingHandler(UnusedMediaBackOfficeHelper backoffice) {
        _backoffice = backoffice;
    }

    public void Handle(ServerVariablesParsingNotification notification) {

        // Get or create the "skybrud" dictionary
        if (!(notification.ServerVariables.TryGetValue("limbo", out object? value) && value is Dictionary<string, object> limbo)) {
            notification.ServerVariables["limbo"] = limbo = new Dictionary<string, object>();
        }

        // Append the "unusedMedia" dictionary to "limbo"
        limbo.Add("unusedMedia", _backoffice.GetServerVariables());

    }

}