angular.module("umbraco").controller("Limbo.Umbraco.UnusedMedia.MediaDeleteController", function ($scope, $controller) {
    $scope.isMedia = true;
    $scope.allowDelete = false;
    angular.extend(this, $controller("Limbo.Umbraco.UnusedMedia.BaseDeleteController", { $scope: $scope }));
});