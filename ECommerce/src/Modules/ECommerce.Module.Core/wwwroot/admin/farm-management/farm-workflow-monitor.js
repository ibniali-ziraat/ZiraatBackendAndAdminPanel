/*global angular*/
(function () {
    'use strict';
    angular.module('simplAdmin.core').controller('FarmWorkflowMonitorCtrl', ['$http', function ($http) {
        var vm = this;
        vm.tab = 'delegations'; vm.delegations = []; vm.cases = []; vm.rules = []; vm.vendors = []; vm.orchards = [];
        vm.grant = {}; vm.rule = { configurationJson: '{}' };
        vm.permissionOptions = [
            { code: 'operations.read', label: 'View operations', description: 'Read non-financial operation details.' },
            { code: 'operations.write', label: 'Record operations', description: 'Reserved assisted-entry permission.' },
            { code: 'labs.read', label: 'View lab reports', description: 'Read approved report metadata.' },
            { code: 'financial.read', label: 'View financial data', description: 'Reserved separately reviewed access.' },
            { code: 'support.write', label: 'Create support cases', description: 'Submit support requests for the orchard.' }
        ];
        vm.load = function () { vm.error = null; vm.loadDelegations(); vm.loadCases(); vm.loadRules(); };
        vm.loadDelegations = function () { return $http.get('/api/admin/farm-management/expansion/delegations').then(function (r) { vm.delegations = r.data; }, fail); };
        vm.loadCases = function () { return $http.get('/api/admin/farm-management/expansion/cases', { params: { kind: vm.caseKind || null, status: vm.caseStatus || null } }).then(function (r) { vm.cases = r.data; }, fail); };
        vm.loadRules = function () { return $http.get('/api/admin/farm-management/expansion/automation-rules').then(function (r) { vm.rules = r.data; }, fail); };
        vm.saveGrant = function () { vm.grant.permissions = vm.permissionOptions.filter(function (x) { return x.selected; }).map(function (x) { return x.code; }); if (!vm.grant.permissions.length) { vm.error = 'Select at least one permission.'; return; } $http.post('/api/admin/farm-management/expansion/delegations', vm.grant).then(function () { vm.grant = {}; vm.permissionOptions.forEach(function (x) { x.selected = false; }); vm.loadDelegations(); }, fail); };
        vm.revoke = function (x) { var reason = window.prompt('Reason for revoking access'); if (!reason) return; $http.post('/api/admin/farm-management/expansion/delegations/' + x.id + '/revoke', { version: x.version, reason: reason }).then(vm.loadDelegations, fail); };
        vm.transition = function (x, action) { var response = action === 'respond' ? window.prompt('Expert response') : null; if (action === 'respond' && !response) return; $http.post('/api/admin/farm-management/expansion/cases/' + x.id + '/transition', { version: x.version, action: action, response: response }).then(vm.loadCases, fail); };
        vm.saveRule = function () { $http.post('/api/admin/farm-management/expansion/automation-rules', vm.rule).then(function () { vm.rule = { configurationJson: '{}' }; vm.loadRules(); }, fail); };
        vm.approveRule = function (x) { $http.post('/api/admin/farm-management/expansion/automation-rules/' + x.id + '/approve', { version: x.version }).then(vm.loadRules, fail); };
        vm.permissionNames = function (json) { try { var codes = angular.fromJson(json); return codes.map(function (code) { var p = vm.permissionOptions.filter(function (x) { return x.code === code; })[0]; return p ? p.label : code; }).join(', '); } catch (e) { return 'Not available'; } };
        vm.caseTypeName = function (kind) { return ({ diagnosis: 'Diagnosis', consultation: 'Consultation', complaint: 'Complaint', 'service-booking': 'Service booking' })[kind] || kind; };
        vm.ruleTypeName = function (kind) { return ({ gdd: 'Growing degree days', 'disease-risk': 'Disease risk', resistance: 'Resistance review', recommendation: 'Recommendation', reminder: 'Reminder', 'harvest-escalation': 'Harvest escalation' })[kind] || kind; };
        function fail(r) { vm.error = (r.data && (r.data.title || r.data.message)) || 'Request failed.'; }
        $http.get('/api/admin/farm-management/expansion/vendors').then(function (r) { vm.vendors = r.data; }, fail);
        $http.get('/api/admin/farm-management/orchards', { params: { pageSize: 100 } }).then(function (r) { vm.orchards = r.data.items; }, fail);
        vm.load();
    }]);
}());
