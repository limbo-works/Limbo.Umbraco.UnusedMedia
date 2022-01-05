angular.module("umbraco.services").factory("unusedMediaService", function ($http) {

    // Get the base URL for the API controller
    const baseUrl = Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + "/backoffice/Limbo/UnusedMedia/";

    return {

        /**
         * @ngdoc method
         * @name umbraco.services.unusedMediaService#getBaseUrl
         * @methodOf umbraco.services.unusedMediaService
         *
         * @description
         * Returns the base URL for the API controller of this package.
         */
        getBaseUrl: function() {
            return baseUrl;
        },

        /**
         * @ngdoc method
         * @name umbraco.services.unusedMediaService#getCacheBuster
         * @methodOf umbraco.services.unusedMediaService
         *
         * @description
         * Returns a cache busting value.
         */
        getCacheBuster: function() {
            return Umbraco.Sys.ServerVariables.application.cacheBuster;
        },

        /**
         * @ngdoc method
         * @name umbraco.services.unusedMediaService#getUnusedMedia
         * @methodOf umbraco.services.unusedMediaService
         *
         * @description
         * Returns a list of unused media items.
         *
         * @param {object} options the options for the request.
         * @param {number} options.page The page to be returned.
         * @param {string} options.$filterName The value of a filter, if active. `$filterName` should be the alias of the filter.
         */
        getUnusedMedia: function(options) {
            return $http.get(baseUrl + "GetItems", options);
        },

        /**
         * @ngdoc method
         * @name umbraco.services.unusedMediaService#getReferencesById
         * @methodOf umbraco.services.unusedMediaService
         *
         * @description
         * Returns a grouped list of references.
         *
         * @param {object} options the options for the request.
         * @param {number} options.id The numeric ID of the item.
         * @param {string} options.type The type of the item - eg. `media`.
         */
        getReferencesById: function(options) {
            return $http.get(baseUrl + "GetReferencesById", options);
        },

        /**
         * @ngdoc method
         * @name umbraco.services.unusedMediaService#getFilters
         * @methodOf umbraco.services.unusedMediaService
         *
         * @description
         * Returns a list of available filters for the unused media dashboard.
         */
        getFilters: function() {
            return $http.get(baseUrl + "GetFilters");
        },

        /**
         * @ngdoc method
         * @name umbraco.services.unusedMediaService#trashMedia
         * @methodOf umbraco.services.unusedMediaService
         *
         * @description
         * Trashes the media with the specified `mediaId`.
         *
         * @param {number} mediaId The ID of the media to be trashed.
         */
        trashMedia: function(mediaId) {
            return $http.get(baseUrl + "TrashMedia?mediaId=" + mediaId, { umbIgnoreErrors: true });
        }

    };

});