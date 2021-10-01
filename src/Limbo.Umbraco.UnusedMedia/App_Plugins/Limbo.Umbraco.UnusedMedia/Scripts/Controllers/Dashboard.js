angular.module("umbraco").controller("Limbo.Umbraco.UnusedMedia.Dashboard", function($http, $timeout) {

    const vm = this;

    let wait = null;

    vm.params = {};

    vm.updateList = function (page) {

        if (page) vm.params.page = page;

        vm.loading = true;

        const config = {
            params: vm.params
        };

        $http.get("/umbraco/backoffice/Limbo/UnusedMedia/GetItems", config).then(function(r) {

            vm.loading = false;
            vm.loaded = true;

            vm.items = r.data.items;

        });

    };

    vm.filterChanged = function (filter) {

        if (filter.type === "dropdown") filter.value = filter.selected ? filter.selected.value : null;

        if (filter.value) {
            vm.params[filter.name] = filter.value;
        } else {
            delete vm.params[filter.name];
        }

        if (wait) $timeout.cancel(wait);

        if (filter.type === "text") {

            // Add a small delay so we dont call the API on each keystroke
            wait = $timeout(function () {
                vm.updateList();
            }, 300);

        } else {
            vm.updateList();
        }

    };

    function init() {

        vm.loading = true;

        $http.get("/umbraco/backoffice/Limbo/UnusedMedia/GetFilters").then(function(r) {

            vm.filters = r.data;

            vm.filters.forEach(function(f) {
                switch (f.type) {
                    case "dropdown":
                        f.selected = f.items.length > 0 ? f.items[0] : null;
                        break;
                }
            });

            vm.updateList();

        });

    }

    init();

});