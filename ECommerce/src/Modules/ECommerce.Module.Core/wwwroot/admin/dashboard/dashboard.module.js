/*global angular*/
(function () {
    'use strict';

    angular
        .module('simplAdmin.dashboard', [])
        .controller('DashboardCtrl', ['$http', '$q', '$state', DashboardCtrl])
        .config(['$stateProvider', function ($stateProvider) {
            $stateProvider.state('dashboard', {
                url: '/dashboard',
                templateUrl: "/admin/dashboard-tpl",
                controller: 'DashboardCtrl as vm'
            });
        }]);

    function DashboardCtrl($http, $q, $state) {
        var vm = this;
        var metricDefinitions = [
            {
                key: 'orders',
                label: 'Orders',
                description: 'Open the order queue',
                icon: 'fa-shopping-bag',
                endpoint: 'api/orders/grid',
                state: 'order'
            },
            {
                key: 'products',
                label: 'Products',
                description: 'Review the active catalog',
                icon: 'fa-cube',
                endpoint: 'api/products/grid',
                state: 'product'
            },
            {
                key: 'shipments',
                label: 'Shipments',
                description: 'Check dispatch progress',
                icon: 'fa-truck',
                endpoint: 'api/shipments/grid',
                state: 'shipments'
            },
            {
                key: 'reviews',
                label: 'Reviews',
                description: 'Moderate fresh feedback',
                icon: 'fa-star-half-o',
                endpoint: 'api/reviews/grid',
                state: 'reviews'
            }
        ];

        vm.dashboard = getEmptyDashboard();
        vm.loading = true;
        vm.error = false;
        vm.chartMetrics = [];
        vm.metricsLoading = true;
        vm.metricsTotal = 0;
        vm.refresh = refresh;
        vm.openMetric = openMetric;

        refresh();

        function refresh() {
            vm.loading = true;
            vm.error = false;
            vm.metricsLoading = true;

            return $q.all([loadZiraatDashboard(), loadModuleMetrics()]).finally(function () {
                vm.loading = false;
                vm.metricsLoading = false;
            });
        }

        function loadZiraatDashboard() {
            return $http.get('api/admin/dashboard').then(function (result) {
                vm.dashboard = normalizeDashboard(result.data);
            }, function () {
                vm.dashboard = getEmptyDashboard();
                vm.error = true;
            });
        }

        function loadModuleMetrics() {
            return $q.all(metricDefinitions.map(loadMetric)).then(function (metrics) {
                vm.chartMetrics = buildMetricDisplay(metrics.filter(function (metric) {
                    return !metric.failed;
                }));

                vm.metricsTotal = vm.chartMetrics.reduce(function (total, metric) {
                    return total + metric.total;
                }, 0);
            });
        }

        function loadMetric(metricDefinition) {
            return fetchTotal(metricDefinition.endpoint).then(function (total) {
                return angular.extend({}, metricDefinition, {
                    total: total
                });
            }, function () {
                return angular.extend({}, metricDefinition, {
                    total: 0,
                    failed: true
                });
            });
        }

        function fetchTotal(endpoint) {
            return $http.post(endpoint, {
                pagination: {
                    start: 0,
                    number: 1,
                    totalItemCount: 0,
                    numberOfPages: 0
                },
                search: {
                    predicateObject: null
                },
                sort: {
                    predicate: '',
                    reverse: false
                }
            }).then(function (result) {
                return Number(result.data.totalRecord) || 0;
            });
        }

        function buildMetricDisplay(metrics) {
            var maxTotal = metrics.reduce(function (max, metric) {
                return Math.max(max, metric.total);
            }, 0);

            if (maxTotal < 1) {
                maxTotal = 1;
            }

            return metrics.map(function (metric, index) {
                var widthPercent = Math.round((metric.total / maxTotal) * 100);

                return angular.extend(metric, {
                    toneClass: 'dashboard-tone-' + ((index % 4) + 1),
                    widthPercent: metric.total > 0 ? Math.max(widthPercent, 14) : 10
                });
            });
        }

        function openMetric(metric) {
            if (!metric || !metric.state) {
                return;
            }

            $state.go(metric.state);
        }

        function normalizeDashboard(data) {
            data = data || {};
            return {
                metrics: data.metrics || [],
                customerRegistrations: data.customerRegistrations || [],
                customerVerificationRates: data.customerVerificationRates || [],
                ordersByRegion: data.ordersByRegion || [],
                customersByRegion: data.customersByRegion || [],
                customerActivity: data.customerActivity || []
            };
        }

        function getEmptyDashboard() {
            return {
                metrics: [],
                customerRegistrations: [],
                customerVerificationRates: [],
                ordersByRegion: [],
                customersByRegion: [],
                customerActivity: []
            };
        }
    }
})();
