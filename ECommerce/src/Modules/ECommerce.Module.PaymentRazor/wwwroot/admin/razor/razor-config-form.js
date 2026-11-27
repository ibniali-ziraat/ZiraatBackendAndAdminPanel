/*global angular, jQuery*/
(function ($) {
    angular
        .module('simplAdmin.paymentRazor')
        .controller('RazorConfigFormCtrl', ['paymentRazorService', 'translateService', RazorConfigFormCtrl]);

    function RazorConfigFormCtrl(paymentRazorService, translateService) {
        var vm = this;
        vm.translate = translateService;
        vm.razorConfig = {};

        vm.save = function save() {
            vm.validationErrors = [];
            paymentRazorService.updateSetting(vm.razorConfig)
                .then(function () {
                    toastr.success('Application settings have been saved');
                })
                .catch(function (response) {
                    var error = response.data;
                    vm.validationErrors = [];
                    if (error && angular.isObject(error)) {
                        for (var key in error) {
                            vm.validationErrors.push(error[key][0]);
                        }
                    } else {
                        vm.validationErrors.push('Could not save settings.');
                    }
                });
        };

        function init() {
            paymentRazorService.getSettings().then(function (result) {
                vm.razorConfig = result.data;
            });
        }

        init();
    }
})(jQuery);
