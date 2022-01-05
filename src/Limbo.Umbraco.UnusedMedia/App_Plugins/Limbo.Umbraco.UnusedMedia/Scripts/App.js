angular.module("umbraco.services").config(function ($httpProvider) {

    $httpProvider.interceptors.push(function () {
        return {
            request: function (request) {

                // Redirect any requests to built in media delete to our custom delete
                if (request.url.indexOf("views/media/delete.html") === 0) {
                    request.url = "/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/MediaDelete.html?v=" + Umbraco.Sys.ServerVariables.application.cacheBuster;
                }

                return request;

            },
            response: function(response) {
                return response;
            }
        };
    });

});