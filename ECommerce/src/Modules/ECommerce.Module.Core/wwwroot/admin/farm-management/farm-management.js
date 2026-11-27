/*global angular, crypto, window*/
(function () {
    'use strict';
    angular.module('simplAdmin.core').controller('FarmManagementCtrl', ['$http', '$state', '$stateParams', function ($http, $state, $stateParams) {
        var vm = this, base = 'api/admin/farm-management/', pending = {}, listSequence = 0, targetSequence = 0;
        vm.directory = $state.current.name === 'farm-orchards';
        vm.createPage = $state.current.name === 'farm-advisory-create';
        vm.page = 1; vm.pageSize = 25; vm.items = []; vm.total = 0; vm.targetPage = 1; vm.selected = {};
        vm.kinds = ['general', 'spray', 'fertigation', 'pruning', 'harvest'];
        vm.kindLabel = function (kind) {
            var normalized = kind;
            if (kind && typeof kind === 'object') normalized = kind.value || kind.label || '';
            normalized = normalized || '';
            return normalized === 'general' ? 'General' : normalized;
        };
        vm.normalizeKinds = function () {
            vm.kinds = (vm.kinds || []).map(function (kind) {
                if (!kind) return kind;
                if (typeof kind === 'object') return kind.value || kind.label || '';
                return kind;
            });
        };
        vm.normalizeKinds();
        vm.tab = 'recipients'; vm.detailPage = 1;
        function fail(error) {
            vm.error = error.status === 401 || error.status === 403 ? 'Administrator sign-in is required.' :
                (error.data && error.data.title) || 'The request failed. Please retry.';
        }
        function write(method, path, body) {
            var signature = method + path + angular.toJson(body);
            var key = pending[signature] || (pending[signature] = crypto.randomUUID());
            return $http({ method: method, url: base + path, data: body, headers: { 'Idempotency-Key': key } }).then(function (r) {
                delete pending[signature]; return r;
            });
        }
        vm.load = function (reset) {
            if (reset) vm.page = 1;
            vm.loading = true; vm.error = ''; var sequence = ++listSequence;
            return $http.get(base + (vm.directory ? 'orchards' : 'advisories'), { params: {
                page: vm.page, pageSize: vm.pageSize, search: vm.search, status: vm.status
            } }).then(function (r) { if (sequence === listSequence) { vm.items = r.data.items; vm.total = r.data.total; } })
                .catch(fail).finally(function () { if (sequence === listSequence) vm.loading = false; });
        };
        vm.move = function (step) { vm.page += step; vm.load(); };
        vm.newDraft = function (orchard) {
            if (vm.directory || !vm.createPage) {
                $state.go('farm-advisory-create', orchard ? { orchard: orchard.id } : {});
                return;
            }
            vm.edit = { kind: 'spray', title: '', recommendation: '', orchardProfileIds: [], status: 'draft' };
            vm.selected = {}; if (orchard) vm.selected[orchard.id] = orchard.orchardName || ('Orchard #' + orchard.id);
            vm.preview = null; vm.detail = null; vm.error = ''; vm.success = ''; vm.targetPage = 1; vm.loadTargets();
        };
        vm.open = function (item) {
            vm.busy = true; vm.error = ''; vm.success = ''; vm.preview = null;
            return $http.get(base + 'advisories/' + item.id).then(function (r) {
                vm.edit = r.data; vm.edit.windowStart = new Date(r.data.windowStart); vm.edit.windowEnd = new Date(r.data.windowEnd);
                vm.selected = {}; angular.forEach(r.data.orchardProfileIds, function (id) { vm.selected[id] = 'Orchard #' + id; });
                vm.detailPage = 1; vm.tab = 'recipients';
                if (vm.edit.status === 'draft') vm.loadTargets(); else vm.loadDetail();
            }).catch(fail).finally(function () { vm.busy = false; });
        };
        vm.close = function () {
            if (vm.createPage) { $state.go('farm-advisories'); return; }
            vm.edit = null; vm.error = ''; vm.success = '';
        };
        vm.loadTargets = function (reset) {
            if (reset) vm.targetPage = 1;
            var sequence = ++targetSequence; vm.targetsLoading = true;
            return $http.get(base + 'orchards', { params: { search: vm.targetSearch, district: vm.targetDistrict, page: vm.targetPage, pageSize: 25 } }).then(function (r) {
                if (sequence !== targetSequence) return;
                vm.targets = r.data.items; vm.targetTotal = r.data.total; vm.districts = r.data.districts || [];
                angular.forEach(vm.targets, function (o) { if (vm.selected[o.id]) vm.selected[o.id] = o.orchardName || ('Orchard #' + o.id); });
            }).catch(fail).finally(function () { if (sequence === targetSequence) vm.targetsLoading = false; });
        };
        vm.targetMove = function (step) { vm.targetPage += step; vm.loadTargets(); };
        vm.toggle = function (o) { if (vm.selected[o.id]) delete vm.selected[o.id]; else vm.selected[o.id] = o.orchardName || ('Orchard #' + o.id); vm.preview = null; };
        vm.selectionCount = function () { return Object.keys(vm.selected).length; };
        vm.save = function (form) {
            if (vm.busy || form.$invalid || !vm.selectionCount()) return;
            vm.busy = true; vm.error = ''; vm.success = ''; vm.preview = null;
            var body = angular.copy(vm.edit); delete body.id; delete body.status; delete body.createdOn; delete body.publishedOn; delete body.cancelledOn; delete body.cancellationReason;
            body.orchardProfileIds = Object.keys(vm.selected).map(Number).sort(function (a, b) { return a - b; });
            return write(vm.edit.id ? 'PUT' : 'POST', 'advisories' + (vm.edit.id ? '/' + vm.edit.id : ''), body).then(function (r) {
                vm.edit.id = r.data.id; vm.edit.version = r.data.version; vm.edit.orchardProfileIds = body.orchardProfileIds;
                vm.success = 'Draft saved.'; form.$setPristine(); vm.load();
            }).catch(fail).finally(function () { vm.busy = false; });
        };
        vm.previewRecipients = function () {
            vm.busy = true; vm.error = '';
            return $http.get(base + 'advisories/' + vm.edit.id + '/recipient-preview').then(function (r) { vm.preview = r.data; })
                .catch(fail).finally(function () { vm.busy = false; });
        };
        vm.publish = function () {
            if (vm.busy || !vm.preview || !window.confirm('Publish this advisory to ' + vm.preview.count + ' orchard recipients?')) return;
            vm.busy = true; vm.error = '';
            return write('POST', 'advisories/' + vm.edit.id + '/publish', { version: vm.preview.version }).then(function (r) {
                vm.edit.status = r.data.status; vm.edit.version = r.data.version; vm.preview = null;
                vm.success = 'Advisory published. Notifications are queued.'; vm.load(); vm.loadDetail();
            }).catch(fail).finally(function () { vm.busy = false; });
        };
        vm.cancel = function () {
            if (vm.busy || !vm.cancelReason || !vm.cancelReason.trim() || !window.confirm('Cancel this advisory?')) return;
            vm.busy = true; vm.error = '';
            return write('POST', 'advisories/' + vm.edit.id + '/cancel', { version: vm.edit.version, reason: vm.cancelReason }).then(function (r) {
                vm.edit.status = r.data.status; vm.edit.version = r.data.version; vm.edit.cancellationReason = vm.cancelReason;
                vm.cancelReason = ''; vm.success = 'Advisory cancelled.'; vm.load(); vm.loadDetail();
            }).catch(fail).finally(function () { vm.busy = false; });
        };
        vm.loadDetail = function (tab) {
            if (tab) { vm.tab = tab; vm.detailPage = 1; }
            vm.detailLoading = true;
            var path = vm.tab, page = vm.detailPage, id = vm.edit.id;
            return $http.get(base + 'advisories/' + id + '/' + path, { params: { page: page, pageSize: 25 } }).then(function (r) {
                if (vm.edit && vm.edit.id === id && vm.tab === path && vm.detailPage === page) vm.detail = r.data;
            }).catch(fail).finally(function () { vm.detailLoading = false; });
        };
        vm.detailMove = function (step) { vm.detailPage += step; vm.loadDetail(); };
        vm.load();
        if (vm.createPage) vm.newDraft($stateParams.orchard ? { id: Number($stateParams.orchard) } : null);
        else if (!vm.directory && $stateParams.orchard) vm.newDraft({ id: Number($stateParams.orchard) });
    }]);
}());
