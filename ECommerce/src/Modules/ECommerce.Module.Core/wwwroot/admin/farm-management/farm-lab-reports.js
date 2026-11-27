/*global angular*/
(function () {
    'use strict';
    angular.module('simplAdmin.core')
        .controller('FarmLabReportsCtrl', ['$http', function ($http) {
            var vm = this;
            vm.orchards = []; vm.items = []; vm.error = null; vm.busy = false;
            vm.sample = { sampleType: 'soil' }; vm.report = { source: '', readings: [], attachments: [] };
            vm.analytes = [{ code: 'ph', label: 'Soil pH' }, { code: 'ec', label: 'Electrical conductivity' }, { code: 'nitrogen', label: 'Nitrogen' }, { code: 'phosphorus', label: 'Phosphorus' }, { code: 'potassium', label: 'Potassium' }, { code: 'organic-matter', label: 'Organic matter' }, { code: 'organic-carbon', label: 'Organic carbon' }];
            vm.addReading = function () { vm.report.readings.push({ analyteCode: 'ph', unit: 'pH' }); };
            vm.removeReading = function (index) { vm.report.readings.splice(index, 1); };
            vm.sampleLabel = function (sample) { return (sample.sampleType === 'water' ? 'Water' : 'Soil') + ' sample · ' + new Date(sample.sampledAt).toLocaleDateString(); };
            vm.useSample = function (sample) { vm.sampleId = sample.id; };
            vm.load = function () {
                if (!vm.orchardId) { vm.items = []; return; } vm.busy = true; vm.error = null;
                $http.get('/api/admin/farm-management/lab/orchards/' + vm.orchardId + '/reports').then(function (r) { vm.items = r.data.items; }, fail).finally(done);
            };
            vm.createSample = function () {
                vm.error = null; $http.post('/api/admin/farm-management/lab/orchards/' + vm.orchardId + '/samples', vm.sample).then(function (r) { vm.sampleId = r.data.id; vm.sample = { sampleType: 'soil' }; vm.load(); }, fail);
            };
            vm.createReport = function () {
                vm.error = null; $http.post('/api/admin/farm-management/lab/samples/' + vm.sampleId + '/reports', vm.report).then(function () { vm.report = { source: '', readings: [], attachments: [] }; vm.load(); }, fail);
            };
            vm.review = function (id, decision) { $http.post('/api/admin/farm-management/lab/reports/' + id + '/review', { decision: decision, notes: vm.reviewNotes }).then(vm.load, fail); };
            function fail(r) { vm.error = (r.data && (r.data.title || r.data.message)) || 'Request failed.'; }
            function done() { vm.busy = false; }
            $http.get('/api/admin/farm-management/orchards?pageSize=100').then(function (r) { vm.orchards = r.data.items; });
        }])
        .controller('FarmRulesCtrl', ['$http', function ($http) {
            var vm = this; vm.rules = []; vm.rule = {}; vm.error = null;
            vm.analytes = [{ code: 'ph', label: 'Soil pH', unit: 'pH' }, { code: 'ec', label: 'Electrical conductivity', unit: 'dS/m' }, { code: 'nitrogen', label: 'Nitrogen', unit: 'mg/kg' }, { code: 'phosphorus', label: 'Phosphorus', unit: 'mg/kg' }, { code: 'potassium', label: 'Potassium', unit: 'mg/kg' }, { code: 'organic-matter', label: 'Organic matter', unit: '%' }, { code: 'organic-carbon', label: 'Organic carbon', unit: '%' }];
            vm.analyteName = function (code) { var match = vm.analytes.filter(function (x) { return x.code === code; })[0]; return match ? match.label : code; };
            vm.applyDefaultUnit = function () { var match = vm.analytes.filter(function (x) { return x.code === vm.rule.analyteCode; })[0]; if (match) vm.rule.unit = match.unit; };
            vm.load = function () { $http.get('/api/admin/farm-management/lab/rules').then(function (r) { vm.rules = r.data; }, fail); };
            vm.save = function () { $http.post('/api/admin/farm-management/lab/rules', vm.rule).then(function () { vm.rule = {}; vm.load(); }, fail); };
            vm.approve = function (rule) { $http.post('/api/admin/farm-management/lab/rules/' + rule.id + '/approve', { version: rule.version }).then(vm.load, fail); };
            function fail(r) { vm.error = (r.data && (r.data.title || r.data.message)) || 'Request failed.'; } vm.load();
        }]);
}());
