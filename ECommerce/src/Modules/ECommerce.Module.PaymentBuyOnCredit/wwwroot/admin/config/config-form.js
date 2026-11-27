/*global angular, jQuery*/
(function ($) {
    angular
        .module('simplAdmin.paymentBuyOnCredit')
        .controller('BuyOnCreditConfigFormCtrl', ['paymentBuyOnCreditService', 'translateService', BuyOnCreditConfigFormCtrl]);

    function BuyOnCreditConfigFormCtrl(paymentBuyOnCreditService, translateService) {
        var vm = this;
        vm.translate = translateService;
        vm.buyoncreditConfig = {};

        vm.save = function save() {
            vm.validationErrors = [];
            paymentBuyOnCreditService.updateSetting(vm.buyoncreditConfig)
                .then(function (result) {
                    toastr.success('Settings have been saved');
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
            paymentBuyOnCreditService.getSettings().then(function (result) {
                vm.buyoncreditConfig = result.data;
            });
        }

        init();
    }
})(jQuery);
