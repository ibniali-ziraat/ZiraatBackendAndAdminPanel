/*global angular*/
(function () {
    angular
        .module('simplAdmin.paymentBuyOnCredit')
        .factory('paymentBuyOnCreditService', ['$http', paymentBuyOnCreditService]);

    function paymentBuyOnCreditService($http) {
        var service = {
            getSettings: getSettings,
            updateSetting: updateSetting
        };
        return service;

        function getSettings() {
            return $http.get('api/buy-on-credit/config');
        }

        function updateSetting(settings) {
            return $http.put('api/buy-on-credit/config', settings);
        }
    }
})();
