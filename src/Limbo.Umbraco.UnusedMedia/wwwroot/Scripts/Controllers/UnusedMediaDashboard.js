angular.module("umbraco").controller("Limbo.Umbraco.UnusedMedia.Dashboard.Controller", function ($scope, $http, localizationService, notificationsService, overlayService, $interval) {

    const vm = this;

    vm.page = {
        title: "",
        description: ""
    };

    vm.scanDate = null;
    vm.media = [];
    vm.mediaFolderCount = 0;
    vm.scanning = false;
    vm.showProgress = false;
    vm.currentStatus = null;
    vm.progressPercentage = 0;

    let pollInterval = null;
    let currentTaskId = null;

    vm.overlay = {
        view: "/umbraco/views/overlays/default/default.html",
        show: false,
        title: "Delete media",
        subtitle: "Are you sure you want to delete this media? This action cannot be undone.",
        closeButtonLabel: "Cancel",
        submitButtonLabel: "Delete",
        submitButtonStyle: "danger",
        close: function () {
            vm.overlay.show = false;
        },
        submit: function () {
            vm.deleteMedia(vm.overlay.media);
            vm.overlay.show = false;
        }
    }

    function init() {
        localizationService.localize("unusedMediaDashboard_title").then(function (value) { vm.page.title = value; });
        localizationService.localize("unusedMediaDashboard_description").then(function (value) { vm.page.description = value; });

        vm.getUnusedMedia();
    }

    vm.getUnusedMedia = function () {
        $http.get("/umbraco/backoffice/api/UnusedMediaBackOffice/GetUnusedMedia").then(function (response) {
            vm.media = response.data.media;
            vm.scanDate = response.data.scanDate;
            vm.mediaFolderCount = response.data.mediaFolderCount;
        });
    };

    vm.clearScan = function () {
        $http.post("/umbraco/backoffice/api/UnusedMediaBackOffice/ClearScan").then(function (response) {
            vm.media = [];
            vm.scanDate = null;
            vm.mediaFolderCount = 0;
            notificationsService.success("Success", "Scan data cleared.");
        });
    };

    vm.startScan = function () {
        vm.scanning = true;
        vm.showProgress = true;
        vm.currentStatus = {
            status: "Queued",
            progress: 0,
            total: 0,
            updatedPages: [],
            errors: [],
            message: ""
        };
        vm.progressPercentage = 0;

        $http.post("/umbraco/backoffice/api/UnusedMediaBackOffice/StartScan").then(function (response) {
            currentTaskId = response.data.taskId;
            notificationsService.success("Success", "Scan started.");
            pollInterval = $interval(function () {
                vm.pollStatus(currentTaskId);
            }, 2000); // Poll every 2 seconds
        }, function (error) {
            notificationsService.error("Error", "Failed to start scan.");
            vm.scanning = false;
            vm.showProgress = false;
        });
    };

    vm.pollStatus = function (taskId) {
        $http.get("/umbraco/backoffice/api/UnusedMediaBackOffice/GetScanStatus", { params: { taskId: taskId } }).then(function (response) {
            vm.currentStatus = response.data;
            if (vm.currentStatus.total > 0) {
                vm.progressPercentage = Math.round((vm.currentStatus.progress / vm.currentStatus.total) * 100);
            } else {
                vm.progressPercentage = 0;
            }

            if (vm.currentStatus.status === "Completed" || vm.currentStatus.status === "Failed") {
                $interval.cancel(pollInterval);
                vm.scanning = false;
                if (vm.currentStatus.status === "Completed") {
                    notificationsService.success("Completed", "Scan completed.");
                    vm.getUnusedMedia(); // Refresh media list
                } else {
                    notificationsService.error("Failed", "Scan failed. Check logs for details.");
                }
            }
        }, function (error) {
            $interval.cancel(pollInterval);
            notificationsService.error("Error", "Failed to get scan status.");
            vm.scanning = false;
            vm.showProgress = false;
        });
    };

    vm.openDeleteOverlay = function (media) {
        vm.overlay.media = media;
        localizationService.localize("unusedMediaDashboard_confirmDeleteTitle").then(function (value) { vm.overlay.title = value; });
        localizationService.localize("unusedMediaDashboard_confirmDeleteMessage", [media.name]).then(function (value) { vm.overlay.subtitle = value; });
        vm.overlay.show = true;
    };

    vm.deleteMedia = function (media) {
        $http.post("/umbraco/backoffice/api/UnusedMediaBackOffice/DeleteMedia", { mediaId: media.id }).then(function (response) {
            notificationsService.success("Success", "Media deleted.");
            vm.getUnusedMedia(); // Refresh media list
        }, function (error) {
            notificationsService.error("Error", "Failed to delete media.");
        });
    };

    // Initialize the controller
    init();

    // Clean up interval when controller is destroyed
    $scope.$on('$destroy', function () {
        if (pollInterval) {
            $interval.cancel(pollInterval);
        }
    });

});