const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const farmRoot = path.join(root, 'src/Modules/ECommerce.Module.Core/wwwroot/admin/farm-management');
const publicRoot = path.join(root, 'src/ECommerce.WebHost/wwwroot');

function start(port = 5188) {
    const server = http.createServer((req, res) => {
        const url = new URL(req.url, 'http://localhost');
        const json = value => { res.writeHead(200, { 'Content-Type': 'application/json' }); res.end(JSON.stringify(value)); };
        if (url.pathname === '/api/admin/farm-management/orchards') return json({ total: 2, page: 1, pageSize: 25, items: [
            { id: 10, orchardId: 'ORC-10', orchardName: 'North Apple Orchard', userId: 101, district: 'Shopian', tehsil: 'Zainapora', kanals: 14, marlas: 5, varieties: ['Gala', 'Red Delicious'] },
            { id: 11, orchardId: 'ORC-11', orchardName: 'River Orchard', userId: 102, district: 'Pulwama', tehsil: 'Tral', kanals: 9, marlas: 0, varieties: ['Ambri'] }
        ] });
        if (url.pathname === '/api/admin/farm-management/advisories') return json({ total: 2, page: 1, pageSize: 25, items: [
            { id: '11111111-1111-1111-1111-111111111111', version: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', title: 'Scab protection window', kind: 'spray', status: 'published', windowStart: new Date().toISOString(), windowEnd: new Date(Date.now() + 86400000).toISOString(), recipientCount: 18 },
            { id: '22222222-2222-2222-2222-222222222222', version: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', title: 'Nutrition schedule', kind: 'fertigation', status: 'draft', windowStart: new Date().toISOString(), windowEnd: new Date(Date.now() + 172800000).toISOString(), recipientCount: 0 }
        ] });
        if (url.pathname === '/api/admin/farm-management/operations') return json({ total: 2, page: 1, pageSize: 25, items: [
            { id: '33333333-3333-3333-3333-333333333333', version: 'cccccccc-cccc-cccc-cccc-cccccccccccc', orchardProfileId: 10, orchardName: 'North Apple Orchard', operationDate: new Date().toISOString(), kind: 'harvest', status: 'posted', coverageValue: 100, coverageUnit: 'trees', cost: 8500, currency: 'INR', totalIncome: 176400 },
            { id: '44444444-4444-4444-4444-444444444444', version: 'dddddddd-dddd-dddd-dddd-dddddddddddd', orchardProfileId: 11, orchardName: 'River Orchard', operationDate: new Date().toISOString(), kind: 'pruning', status: 'posted', coverageValue: 80, coverageUnit: 'trees', cost: null, currency: null, totalIncome: null }
        ] });
        if (url.pathname === '/api/admin/farm-management/lab/orchards/10/reports' || url.pathname === '/api/admin/farm-management/lab/orchards/11/reports') return json({ items: [{ id: '55555555-5555-5555-5555-555555555555', sampleId: '66666666-6666-6666-6666-666666666666', sampleType: 'soil', sampledAt: new Date().toISOString(), source: 'District laboratory', status: 'approved' }], samples: [{ id: '66666666-6666-6666-6666-666666666666', sampleType: 'soil', sampledAt: new Date().toISOString() }] });
        if (url.pathname === '/api/admin/farm-management/lab/rules') return json([{ id: '77777777-7777-7777-7777-777777777777', analyteCode: 'nitrogen', unit: 'mg/kg', crop: 'Apple', variety: 'Gala', growthStage: 'Fruit development', minimum: 20, maximum: 40, evidence: 'Approved district agronomy guidance.', status: 'approved' }]);
        if (url.pathname === '/api/admin/farm-management/expansion/vendors') return json([{ id: 31, name: 'Valley Farm Supplies' }, { id: 32, name: 'Green Harvest Services' }]);
        if (url.pathname === '/api/admin/farm-management/expansion/delegations') return json([{ id: '88888888-8888-8888-8888-888888888888', version: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', vendorId: 31, vendorName: 'Valley Farm Supplies', orchardProfileId: 10, orchardName: 'North Apple Orchard', permissionsJson: '["operations.read","labs.read"]', status: 'active', createdOn: new Date().toISOString() }]);
        if (url.pathname === '/api/admin/farm-management/expansion/cases') return json([{ id: '99999999-9999-9999-9999-999999999999', version: 'ffffffff-ffff-ffff-ffff-ffffffffffff', kind: 'diagnosis', farmerName: 'Aamir Dar', orchardName: 'North Apple Orchard', title: 'Leaf discoloration', detail: 'Review requested for the upper block.', status: 'open', createdOn: new Date().toISOString() }]);
        if (url.pathname === '/api/admin/farm-management/expansion/automation-rules') return json([{ id: 'aaaaaaaa-1111-1111-1111-111111111111', version: 'abababab-abab-abab-abab-abababababab', kind: 'disease-risk', evidence: 'Approved extension guidance.', status: 'approved' }]);
        if (url.pathname === '/admin' || url.pathname === '/') {
            res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
            return res.end(`<!doctype html><html lang="en" ng-app="simplAdmin.core"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Farm management preview</title>
<link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.css"><link rel="stylesheet" href="/lib/font-awesome/css/font-awesome.css"><link rel="stylesheet" href="/farm-management.css"><style>body{background:#f1f4f3;font-family:Segoe UI,sans-serif;margin:0}header{background:#183e32;color:white;padding:16px 24px}*{box-sizing:border-box}</style>
<script src="/lib/angular/angular.js"></script><script src="/lib/angular-ui-router/angular-ui-router.js"></script><script>angular.module('simplAdmin.core',['ui.router']).config(function($stateProvider){$stateProvider.state('farm-orchards',{url:'/farm-orchards',templateUrl:'/farm-management.html',controller:'FarmManagementCtrl as vm'}).state('farm-advisories',{url:'/farm-advisories?orchard',templateUrl:'/farm-management.html',controller:'FarmManagementCtrl as vm'}).state('farm-operations',{url:'/farm-operations',templateUrl:'/farm-operations.html',controller:'FarmOperationsCtrl as vm'}).state('farm-lab-reports',{url:'/farm-lab-reports',templateUrl:'/farm-lab-reports.html',controller:'FarmLabReportsCtrl as vm'}).state('farm-rules',{url:'/farm-rules',templateUrl:'/farm-rules.html',controller:'FarmRulesCtrl as vm'}).state('farm-workflow-monitor',{url:'/farm-workflow-monitor',templateUrl:'/farm-workflow-monitor.html',controller:'FarmWorkflowMonitorCtrl as vm'});});</script><script src="/farm-management.js"></script><script src="/farm-operations.js"></script><script src="/farm-lab-reports.js"></script><script src="/farm-workflow-monitor.js"></script></head><body><header><strong>Admin / Farm Management</strong></header><main ui-view></main></body></html>`);
        }
        let base, relative;
        if (url.pathname.startsWith('/lib/')) { base = publicRoot; relative = url.pathname.slice(1); }
        else if (url.pathname.startsWith('/farm-')) { base = farmRoot; relative = url.pathname.slice(1); }
        else { res.writeHead(404); return res.end(); }
        const file = path.resolve(base, relative);
        if (!file.startsWith(base + path.sep) || !fs.existsSync(file)) { res.writeHead(404); return res.end(); }
        const mime = { '.js': 'text/javascript', '.css': 'text/css', '.html': 'text/html' };
        res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream' }); fs.createReadStream(file).pipe(res);
    });
    return new Promise((resolve, reject) => { server.once('error', reject); server.listen(port, '127.0.0.1', () => resolve(server)); });
}
module.exports = { start };
if (require.main === module) start(Number(process.env.PORT || 5188)).then(server => console.log('Mock-data farm preview: http://127.0.0.1:' + server.address().port + '/admin#!/farm-advisories'));
