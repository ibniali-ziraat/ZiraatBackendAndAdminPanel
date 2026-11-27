/*global angular*/
(function () {
    'use strict';

    angular
        .module('simplAdmin.core', [])
        .config(['$stateProvider', function ($stateProvider) {
            $stateProvider.state('users', {
                url: '/users',
                templateUrl: "_content/ECommerce.Module.Core/admin/user/user-list.html",
                controller: 'UserListCtrl as vm'
                })
                .state('account', {
                    url: '/account',
                    templateUrl: '_content/ECommerce.Module.Core/admin/account/account.html',
                    controller: 'AdminAccountCtrl as vm'
                })
                .state('farm-orchards', {
                    url: '/farm-orchards',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-management.html',
                    controller: 'FarmManagementCtrl as vm'
                })
                .state('farm-advisories', {
                    url: '/farm-advisories?orchard',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-management.html',
                    controller: 'FarmManagementCtrl as vm'
                })
                .state('farm-advisory-create', {
                    url: '/farm-advisories/new?orchard',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-management.html',
                    controller: 'FarmManagementCtrl as vm'
                })
                .state('farm-operations', {
                    url: '/farm-operations',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-operations.html',
                    controller: 'FarmOperationsCtrl as vm'
                })
                .state('farm-lab-reports', {
                    url: '/farm-lab-reports',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-lab-reports.html',
                    controller: 'FarmLabReportsCtrl as vm'
                })
                .state('farm-rules', {
                    url: '/farm-rules',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-rules.html',
                    controller: 'FarmRulesCtrl as vm'
                })
                .state('farm-workflow-monitor', {
                    url: '/farm-workflow-monitor',
                    templateUrl: '_content/ECommerce.Module.Core/admin/farm-management/farm-workflow-monitor.html',
                    controller: 'FarmWorkflowMonitorCtrl as vm'
                })
                .state('user-create', {
                    url: '/user/create',
                    templateUrl: '_content/ECommerce.Module.Core/admin/user/user-form.html',
                    controller: 'UserFormCtrl as vm'
                })
                .state('user-edit', {
                    url: '/user/edit/:id',
                    templateUrl: '_content/ECommerce.Module.Core/admin/user/user-form.html',
                    controller: 'UserFormCtrl as vm'
                })
                .state('widget', {
                    url: '/widget',
                    templateUrl: '_content/ECommerce.Module.Core/admin/widget/widget-instance-list.html',
                    controller: 'WidgetInstanceListCtrl as vm'
                })
                .state('configuration', {
                    url: '/configuration',
                    templateUrl: '_content/ECommerce.Module.Core/admin/configuration/configuration.html',
                    controller: 'ConfigurationCtrl as vm'
                })
                .state('customergroups', {
                    url: '/customergroups',
                    templateUrl: "_content/ECommerce.Module.Core/admin/customergroups/customergroup-list.html",
                    controller: 'CustomerGroupListCtrl as vm'
                })
                .state('customergroup-create', {
                    url: '/customergroups/create',
                    templateUrl: "_content/ECommerce.Module.Core/admin/customergroups/customergroup-form.html",
                    controller: 'CustomerGroupFormCtrl as vm'
                })
                .state('customergroup-edit', {
                    url: '/customergroups/edit/:id',
                    templateUrl: '_content/ECommerce.Module.Core/admin/customergroups/customergroup-form.html',
                    controller: 'CustomerGroupFormCtrl as vm'
                })
                .state('themes', {
                    url: '/themes',
                    templateUrl: '_content/ECommerce.Module.Core/admin/themes/theme-list.html',
                    controller: 'ThemeListCtrl as vm'
                })
                .state('online-themes', {
                    url: '/online-themes',
                    templateUrl: '_content/ECommerce.Module.Core/admin/themes/online-theme-list.html',
                    controller: 'OnlineThemeListCtrl as vm'
                })
                .state('theme-details', {
                    url: '/theme-details/:name',
                    templateUrl: '_content/ECommerce.Module.Core/admin/themes/theme-details.html',
                    controller: 'ThemeDetailsCtrl as vm'
                })
                .state('countries', {
                    url: '/countries',
                    templateUrl: '_content/ECommerce.Module.Core/admin/countries/country-list.html',
                    controller: 'CountryListCtrl as vm'
                })
                .state('country-create', {
                    url: '/countries/create',
                    templateUrl: '_content/ECommerce.Module.Core/admin/countries/country-form.html',
                    controller: 'CountryFormCtrl as vm'
                })
                .state('country-edit', {
                    url: '/countries/edit/:id',
                    templateUrl: '_content/ECommerce.Module.Core/admin/countries/country-form.html',
                    controller: 'CountryFormCtrl as vm'
                })
                .state('states-provinces', {
                    url: '/states-provinces',
                    templateUrl: '_content/ECommerce.Module.Core/admin/stateprovince/state-province-list.html',
                    controller: 'StateProvinceListCtrl as vm'
                })
                .state('country-states-provinces', {
                    url: '/countries/:countryId/states-provinces',
                    templateUrl: '_content/ECommerce.Module.Core/admin/stateprovince/state-province-list.html',
                    controller: 'StateProvinceListCtrl as vm'
                })
                .state('state-province-create', {
                    url: '/countries/:countryId/state-province/create',
                    templateUrl: '_content/ECommerce.Module.Core/admin/stateprovince/state-province-form.html',
                    controller: 'StateProvinceFormCtrl as vm'
                })
                .state('state-province-edit', {
                    url: '/countries/:countryId/state-province/edit/:id?tab',
                    templateUrl: '_content/ECommerce.Module.Core/admin/stateprovince/state-province-form.html',
                    controller: 'StateProvinceFormCtrl as vm'
                });
        }]);
})();
