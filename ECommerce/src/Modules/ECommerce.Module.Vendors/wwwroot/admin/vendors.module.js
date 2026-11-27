/*global angular*/
(function () {
    'use strict';

    angular
        .module('simplAdmin.vendors', [])
        .config(['$stateProvider', function ($stateProvider) {
            $stateProvider.state('vendors', {
                url: '/vendors',
                templateUrl: "_content/ECommerce.Module.Vendors/admin/vendors/vendor-list.html",
                controller: 'VendorListCtrl as vm'
            })
            .state('vendor-create', {
                url: '/vendors/create',
                templateUrl: "_content/ECommerce.Module.Vendors/admin/vendors/vendor-form.html",
                controller: 'VendorFormCtrl as vm'
            })
            .state('vendor-credit-dashboard', {
                url: '/vendors/:id/credit-dashboard',
                templateUrl: '_content/ECommerce.Module.Vendors/admin/vendors/vendor-credit-dashboard.html',
                controller: 'VendorCreditDashboardCtrl as vm'
            })
            .state('vendor-edit', {
                url: '/vendors/edit/:id',
                templateUrl: '_content/ECommerce.Module.Vendors/admin/vendors/vendor-form.html',
                controller: 'VendorFormCtrl as vm'
            });
        }]);
})();
