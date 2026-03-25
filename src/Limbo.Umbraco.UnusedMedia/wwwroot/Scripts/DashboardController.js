angular.module("umbraco").controller("Limbo.UnusedMedia.DashboardController", function ($element) {

    const vm = this;

    const variables = Umbraco.Sys.ServerVariables.limbo.unusedMedia;
    if (!variables) return;

    if (variables.dashboardElementName) {
        const map = document.createElement(variables.dashboardElementName);
        vm.dashboardElementName = variables.dashboardElementName;
        $element[0].appendChild(map);
    }

});