angular.module("umbraco").controller("Limbo.Umbraco.UnusedMedia.BaseDeleteController", function ($scope, $controller) {

    // inherit core delete controller
    if ($scope.isMedia) {
        angular.extend(this, $controller("Umbraco.Editors.Media.DeleteController", { $scope: $scope }));
    } else {
        angular.extend(this, $controller("Umbraco.Editors.Content.DeleteController", { $scope: $scope }));
    }

    $scope.$watch("success", function (newValue, oldValue) {
        if (newValue === true) {
            $scope.close();
        }
    });

});