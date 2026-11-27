/*global angular*/
(function () {
    'use strict';

    angular
        .module('simplAdmin.paymentCashfree', [])
        .config(['$stateProvider',
            function ($stateProvider) {
                $stateProvider
                    .state('payments-cashfree-config', {
                        url: '/payments/cashfree/config',
                        templateUrl: '_content/ECommerce.Module.PaymentCashfree/admin/cashfree/cashfree-config-form.html',
                        controller: 'CashfreeConfigFormCtrl as vm'
                    })
                ;
            }
        ]);
})();
