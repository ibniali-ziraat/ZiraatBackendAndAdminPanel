const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '../../src');
const moduleRoot = path.join(root, 'Modules/ECommerce.Module.Core/wwwroot');
const libRoot = path.join(root, 'ECommerce.WebHost/wwwroot/lib');
const prefix = '/_content/ECommerce.Module.Core/admin/';
const assets = new Map();
[
    'core.module.js', 'admin-core.css', 'countries/country-service.js',
    'stateprovince/state-province-form.js', 'stateprovince/state-province-form.html',
    'stateprovince/state-province-service.js', 'stateprovince/state-province-list.js',
    'stateprovince/state-province-list.html'
].forEach(file => assets.set(prefix + file, path.join(moduleRoot, 'admin', file)));
[
    'jquery/jquery.js', 'angular/angular.js', 'angular-ui-router/angular-ui-router.js',
    'angular-smart-table/dist/smart-table.js', 'bootstrap/dist/js/bootstrap.js',
    'bootstrap/dist/css/bootstrap.css', 'font-awesome/css/font-awesome.css',
    'bootstrap/dist/fonts/glyphicons-halflings-regular.woff2',
    'font-awesome/fonts/fontawesome-webfont.woff2'
].forEach(file => assets.set('/lib/' + file, path.join(libRoot, file)));

const scripts = [
    '/lib/jquery/jquery.js', '/lib/bootstrap/dist/js/bootstrap.js', '/lib/angular/angular.js',
    '/lib/angular-ui-router/angular-ui-router.js', '/lib/angular-smart-table/dist/smart-table.js',
    prefix + 'core.module.js', prefix + 'countries/country-service.js',
    prefix + 'stateprovince/state-province-service.js', prefix + 'stateprovince/state-province-form.js',
    prefix + 'stateprovince/state-province-list.js'
];
const html = `<!doctype html><html lang="en" ng-app="districtPreview"><head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>District Admin - Local Test Preview</title>
<link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.css">
<link rel="stylesheet" href="/lib/font-awesome/css/font-awesome.css">
<link rel="stylesheet" href="${prefix}admin-core.css">
<style>body{background:#f7f9f8;margin:0}main{max-width:1120px;margin:32px auto;padding:24px;min-width:0}
@media(max-width:767px){main{margin:0;padding:20px}}</style></head><body>
<main><div ui-view></div></main>
${scripts.map(src => '<script src="' + src + '"></script>').join('\n')}
<script>
angular.module('districtPreview', ['ui.router', 'smart-table', 'simplAdmin.core'])
.factory('translateService', function(){return {get: function(text){return text;}};})
.config(['$urlRouterProvider', function(router){
router.otherwise('/countries/IN/state-province/edit/10?tab=districts');
}]);
</script></body></html>`;

function createPreviewServer() {
    // In-memory fixtures only. This server never connects to the application database.
    const state = { id: 10, name: 'Jammu and Kashmir', code: 'JK', type: 'Union Territory', countryId: 'IN' };
    const districts = [
        { id: 1, name: 'Srinagar', type: 'District', location: 'Kashmir', stateOrProvinceId: 10 },
        { id: 2, name: 'Jammu', type: 'District', location: 'Jammu', stateOrProvinceId: 10 }
    ];
    const requests = [];
    const json = (res, status, data) => {
        res.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
        res.end(JSON.stringify(data));
    };
    const server = http.createServer(async (req, res) => {
        const url = new URL(req.url, 'http://localhost');
        const route = url.pathname;
        if (route === '/' && req.method === 'GET') {
            res.writeHead(200, { 'Content-Type': 'text/html' });
            return res.end(html);
        }
        if (assets.has(route) && req.method === 'GET') {
            const file = assets.get(route);
            const types = { '.js': 'text/javascript', '.html': 'text/html', '.css': 'text/css', '.woff2': 'font/woff2' };
            res.writeHead(200, { 'Content-Type': types[path.extname(file)], 'Cache-Control': 'no-store' });
            return fs.createReadStream(file).pipe(res);
        }
        let body = '';
        for await (const chunk of req) body += chunk;
        let data;
        try { data = body ? JSON.parse(body) : null; }
        catch { return json(res, 400, { error: 'Invalid JSON' }); }
        requests.push({ method: req.method, route, data });
        if (route === '/api/countries' && req.method === 'GET') return json(res, 200, [{ id: 'IN', name: 'India' }]);
        if (route === '/api/states-provinces/grid' && req.method === 'POST') {
            return json(res, 200, { items: [state], numberOfPages: 1, totalRecord: 1 });
        }
        if (route === '/api/states-provinces/10' && req.method === 'GET') return json(res, 200, state);
        if (route === '/api/states-provinces/10' && req.method === 'PUT') {
            Object.assign(state, data);
            return json(res, 202, null);
        }
        if (route === '/api/states-provinces/' && req.method === 'POST') return json(res, 201, null);
        if (route === '/api/states-provinces/10/districts') {
            if (req.method === 'GET') return json(res, 200, districts);
            if (req.method === 'POST') {
                if (!data?.name?.trim()) return json(res, 400, { Name: ['The Name field is required.'] });
                if (districts.some(d => d.name.toLowerCase() === data.name.trim().toLowerCase())) {
                    return json(res, 409, { error: 'A district with this name already exists in the state or province.' });
                }
                const district = { ...data, name: data.name.trim(), id: districts.length + 1, stateOrProvinceId: 10 };
                districts.push(district);
                return json(res, 201, district);
            }
        }
        return json(res, 404, { error: 'Not found in local test preview.' });
    });
    return { server, requests };
}

module.exports = { createPreviewServer };
if (require.main === module) {
    const { server } = createPreviewServer();
    server.listen(Number(process.env.PORT || 4318), '127.0.0.1', () => {
        console.log('District UI preview (in-memory test data): http://127.0.0.1:' + server.address().port);
    });
}
