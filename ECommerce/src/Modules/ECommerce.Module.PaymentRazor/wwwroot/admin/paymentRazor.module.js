/*global angular*/
(function () {
    'use strict';

    angular
        .module('simplAdmin.paymentRazor', [])
        .config(['$stateProvider',
            function ($stateProvider) {
                $stateProvider
                    .state('payments-razor-config', {
                        url: '/payments/razor/config',
                        templateUrl: '_content/ECommerce.Module.PaymentRazor/admin/razor/razor-config-form.html',
                        controller: 'RazorConfigFormCtrl as vm'
                    })
                    .state('payments-razorpay-config', {
                        url: '/payments/razorpay/config',
                        templateUrl: '_content/ECommerce.Module.PaymentRazor/admin/razor/razor-config-form.html',
                        controller: 'RazorConfigFormCtrl as vm'
                    })
                ;
            }
        ]);
})();
