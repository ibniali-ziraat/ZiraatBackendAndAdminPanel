/*global angular*/
(function () {
    angular
        .module('simplAdmin.paymentRazor')
        .factory('paymentRazorService', ['$http', paymentRazorService]);

    function paymentRazorService($http) {
        var service = {
            getSettings: getSettings,
            updateSetting: updateSetting
        };
        return service;

        function getSettings() {
            return $http.get('api/razor/config');
        }

        function updateSetting(settings) {
            return $http.put('api/razor/config', settings);
        }
    }
})();
