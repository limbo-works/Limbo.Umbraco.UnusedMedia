angular.module("umbraco").controller("Limbo.Umbraco.UnusedMedia.Dashboard", function ($timeout, localizationService, overlayService, notificationsService, unusedMediaService) {

    const vm = this;

    let wait = null;

    vm.params = {};

    vm.prev = function () {
        if (!vm.pagination) return;
        if (vm.pagination.page && vm.pagination.page > 1) vm.updateList(vm.pagination.page - 1);
    };

    vm.next = function () {
        if (!vm.pagination) return;
        if (vm.pagination.page && vm.pagination.page < vm.pagination.pages) vm.updateList(vm.pagination.page + 1);
    };

    vm.updateList = function (page) {

        if (page) vm.params.page = page;

        vm.loading = true;

        const config = {
            params: vm.params
        };

        unusedMediaService.getUnusedMedia(config).then(function(r) {

            vm.loading = false;
            vm.loaded = true;

            vm.items = r.data.items;

            vm.stats = r.data;
            delete vm.stats.items;

            vm.report = vm.stats.report;

            vm.pagination = {
                from: vm.stats.offset + 1,
                to: Math.min(vm.stats.offset + vm.stats.limit, vm.stats.unused),
                page: vm.stats.page,
                pages: vm.stats.pages,
                total: vm.stats.unused,
                pagination: []
            };

            for (let i = Math.max(1, vm.stats.page - 5); i <= Math.min(vm.stats.page + 5, vm.stats.pages); i++) {
                vm.pagination.pagination.push({
                    page: i,
                    active: i === vm.stats.page
                });
            }

            const tokens = [
                vm.pagination.from,
                vm.pagination.to,
                vm.pagination.total,
                vm.pagination.page,
                vm.pagination.pages
            ];

            localizationService.localize('redirects_pagination', tokens).then(function (value) {
                vm.pagination.text = value;
            });

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

        vm.activeFilters = vm.filters.filter(x => x.value).length;

    };

    vm.moveToTrash = function (media) {

        const options = {
            confirmType: "delete",
            submitButtonLabelKey: "unusedMedia_trashConfirm",
            title: "Flyt til papirkurven",
            content: `Er du sikker på at du vil flytte mediet <strong>${media.name}</strong> til papirkurven?`,
            view: "/App_Plugins/Limbo.Umbraco.UnusedMedia/Views/Overlays/Confirm.html",
            submit: function() {
                options.submitButtonState = "busy";
                unusedMediaService.trashMedia(media.id).then(function() {
                    options.submitButtonState = "success";
                    overlayService.close();
                    notificationsService.success("Papirkurv", `Mediet ${media.name} er nu blevet flyttet til papirkurven.`);
                    vm.updateList();
                }, function () {
                    options.submitButtonState = "error";
                });
            },
            close: function() {
                overlayService.close();
            }
        };

        overlayService.confirmDelete(options);

    }; 

    function init() {

        vm.loading = true;

        unusedMediaService.getFilters().then(function(r) {

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