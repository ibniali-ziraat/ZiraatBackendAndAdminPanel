/*global angular*/
(function () {
    angular
        .module('simplAdmin.vendors')
        .factory('vendorService', ['$http', vendorService]);

    function vendorService($http) {
        var service = {
            getVendors: getVendors,
            getVendor: getVendor,
            getVendorCreditDashboard: getVendorCreditDashboard,
            getVendorCustomers: getVendorCustomers,
            getCustomerCreditOrders: getCustomerCreditOrders,
            getCustomerSettlementSummary: getCustomerSettlementSummary,
            addCustomerSettlement: addCustomerSettlement,
            updateCustomerBalance: updateCustomerBalance,
            createVendor: createVendor,
            editVendor: editVendor,
            deleteVendor: deleteVendor
        };
        return service;

        function getVendors(params) {
            return $http.post('api/vendors/grid', params);
        }

        function getVendor(id) {
            return $http.get('api/vendors/' + id);
        }

        function getVendorCreditDashboard(id) {
            return $http.get('api/vendor-dashboard/' + id + '/credit');
        }

        function getVendorCustomers(id) {
            return $http.get('api/vendors/' + id + '/customers/all');
        }

        function getCustomerCreditOrders(vendorId, customerId) {
            return $http.get('api/vendors/' + vendorId + '/customers/' + customerId + '/BuyOnCredit/orders');
        }

        function getCustomerSettlementSummary(vendorId, customerId) {
            return $http.get('api/vendors/' + vendorId + '/customers/' + customerId + '/settlement/summary');
        }

        function addCustomerSettlement(vendorId, customerId, settlement) {
            return $http.post('api/vendors/' + vendorId + '/customers/' + customerId + '/settlement/add', settlement);
        }

        function updateCustomerBalance(vendorId, payload) {
            return $http.post('api/vendors/' + vendorId + '/customers/updateCustomerBalance', payload);
        }

        function createVendor(vendor) {
            return $http.post('api/vendors', vendor);
        }

        function editVendor(vendor) {
            return $http.put('api/vendors/' + vendor.id, vendor);
        }

        function deleteVendor(vendor) {
            return $http.delete('api/vendors/' + vendor.id, null);
        }
    }
})();
