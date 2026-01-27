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

    // Helper function to format file size
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }

    vm.getUnusedMedia = function () {
        $http.get("/umbraco/backoffice/api/UnusedMediaBackOffice/GetUnusedMedia").then(function (response) {
            console.log("GetUnusedMedia response:", response.data);

            // Format dates and file sizes in each media item to prevent digest loop
            vm.media = (response.data.media || []).map(function (item) {
                // Format update date
                if (item.updateDate) {
                    var date = new Date(item.updateDate);
                    item.updateDateFormatted = date.toISOString().replace('T', ' ').substring(0, 16);
                } else {
                    item.updateDateFormatted = 'N/A';
                }

                // Format file size
                item.totalBytesFormatted = formatFileSize(item.totalBytes || 0);

                return item;
            });

            // Format scanDate to prevent digest loop
            if (response.data.scanDate) {
                var date = new Date(response.data.scanDate);
                vm.scanDate = date.toISOString().replace('T', ' ').substring(0, 16);
            } else {
                vm.scanDate = "N/A";
            }

            vm.mediaFolderCount = response.data.mediaFolderCount || 0;
            console.log("vm.media:", vm.media);
            console.log("vm.media.length:", vm.media.length);
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