/*global angular*/
(function () {
    'use strict';
    angular.module('simplAdmin.core').controller('FarmOperationsCtrl', ['$http', function ($http) {
        var vm = this, base = 'api/admin/farm-management/operations', sequence = 0;
        vm.page = 1; vm.pageSize = 25; vm.orchards = []; vm.kinds = ['', 'spray', 'fertigation', 'pruning', 'harvest']; vm.statuses = ['', 'posted', 'void'];
        function fail(r) { vm.error = r.status === 401 || r.status === 403 ? 'Administrator sign-in is required.' : (r.data && r.data.title) || 'Unable to load operations.'; }
        vm.load = function (reset) {
            if (reset) vm.page = 1; vm.loading = true; vm.error = ''; var current = ++sequence;
            return $http.get(base, { params: { orchardProfileId: vm.orchardProfileId || null, kind: vm.kind, status: vm.status, page: vm.page, pageSize: vm.pageSize } })
                .then(function (r) { if (current === sequence) { vm.items = r.data.items; vm.total = r.data.total; } }).catch(fail)
                .finally(function () { if (current === sequence) vm.loading = false; });
        };
        vm.move = function (step) { vm.page += step; vm.load(); };
        vm.open = function (row) { vm.detailLoading = true; vm.error = ''; return $http.get(base + '/' + row.id, { params: { orchardProfileId: row.orchardProfileId } })
            .then(function (r) { vm.detail = r.data; }).catch(fail).finally(function () { vm.detailLoading = false; }); };
        vm.close = function () { vm.detail = null; };
        $http.get('api/admin/farm-management/orchards', { params: { pageSize: 100 } }).then(function (r) { vm.orchards = r.data.items; }).catch(fail);
        vm.load();
    }]);
}());
