angular.module("umbraco").directive("limboReferenceList", function (unusedMediaService) {
	return {
		restrict: "E",
        templateUrl: "/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/Directives/ReferenceList.html?v=" + unusedMediaService.getCacheBuster(),
        link: function link(scope) {

            const options = {
                params: {
                    id: scope.currentNode.id,
                    type: scope.currentNode.nodeType
                }
            };

            scope.isLoading = true;

            unusedMediaService.getReferencesById(options).then(function (r) {

                scope.references = r.data;
                scope.hasReferences = r.data.total > 0;
                scope.allowDelete = r.data.allowDelete;
                scope.showToggle = r.data.allowDelete && r.data.showToggle;
                scope.notAllowedMessage = r.data.notAllowedMessage ? r.data.notAllowedMessage : null;

                // If the toggle is enabled, we disable the OK button until the user has activated the toggle
                if (scope.showToggle) scope.allowDelete = false;

                scope.isLoading = false;

            });

            scope.holdMyBeer = function () {
                scope.allowDelete = true;
            };

        }
	};

});