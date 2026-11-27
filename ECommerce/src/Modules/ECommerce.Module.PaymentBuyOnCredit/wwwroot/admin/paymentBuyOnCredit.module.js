/*global angular*/
(function () {
    'use strict';

    angular
        .module('simplAdmin.paymentBuyOnCredit', [])
        .config(['$stateProvider',
            function ($stateProvider) {
                $stateProvider
                    .state('payments-buyoncredit-config', {
                        url: '/payments/buyoncredit/config',
                        templateUrl: '_content/ECommerce.Module.PaymentBuyOnCredit/admin/config/config-form.html',
                        controller: 'BuyOnCreditConfigFormCtrl as vm'
                    })
                    ;
            }
        ]);
})();
