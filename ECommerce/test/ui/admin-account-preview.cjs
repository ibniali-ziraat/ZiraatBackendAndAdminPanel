const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const accountRoot = path.join(root, 'src/Modules/ECommerce.Module.Core/wwwroot/admin/account');
const publicRoot = path.join(root, 'src/ECommerce.WebHost/wwwroot');

function start(port = 5187) {
    let fullName = 'Admin';
    let mode = '';
    const server = http.createServer(async (req, res) => {
        const url = new URL(req.url, 'http://localhost');
        const json = (status, value) => {
            res.writeHead(status, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(value));
        };
        if (url.pathname === '/test-mode') {
            mode = url.searchParams.get('value') || '';
            return json(200, {});
        }
        if (url.pathname === '/user' && req.method === 'GET') {
            if (mode === 'unauthorized') return json(401, {});
            return json(200, { model: { fullName, email: 'admin@example.test', phoneNumber: '0000000000', hasPassword: true, twoFactor: false } });
        }
        if (req.method === 'POST' && ['/user/info', '/user/change-password', '/user/set-password'].includes(url.pathname)) {
            let body = '';
            for await (const chunk of req) { body += chunk; if (body.length > 8192) { res.writeHead(413); return res.end(); } }
            const form = new URLSearchParams(body);
            if (url.pathname === '/user/info') {
                fullName = form.get('fullName');
                if (!fullName || !fullName.trim()) return json(400, { FullName: ['Full name is required.'] });
            } else if (url.pathname === '/user/change-password' && form.get('oldPassword') !== 'current-password') {
                return json(400, [{ code: 'PasswordMismatch', description: 'Incorrect current password.' }]);
            }
            return json(200, { success: true });
        }
        if (url.pathname === '/admin' || url.pathname === '/') {
            res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
            return res.end(`<!doctype html><html lang="en" ng-app="simplAdmin.core"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Admin account preview</title>
<link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.css"><link rel="stylesheet" href="/lib/font-awesome/css/font-awesome.css"><link rel="stylesheet" href="/admin-core.css"><link rel="stylesheet" href="/account/account.css">
<style>body{background:#edf1f3;font-family:"Segoe UI",sans-serif;margin:0}header.preview{padding:18px 24px;background:#193e31;color:white;display:flex;justify-content:space-between;gap:20px}main{padding:24px 12px}*{box-sizing:border-box}</style>
<script src="/lib/angular/angular.js"></script><script>angular.module('simplAdmin.core',[]).factory('translateService',function(){return {get:function(value){return value;}};});</script><script src="/account/account.js"></script></head>
<body class="admin-body"><header class="preview"><strong>Admin</strong><span>Account settings</span></header><main ng-controller="AdminAccountCtrl as vm">${fs.readFileSync(path.join(accountRoot, 'account.html'), 'utf8')}</main></body></html>`);
        }
        let base, relative;
        if (url.pathname.startsWith('/account/')) { base = accountRoot; relative = url.pathname.slice(9); }
        else if (url.pathname === '/admin-core.css') { base = path.dirname(accountRoot); relative = 'admin-core.css'; }
        else if (url.pathname.startsWith('/lib/')) { base = publicRoot; relative = url.pathname.slice(1); }
        else { res.writeHead(404); return res.end(); }
        const file = path.resolve(base, relative);
        if (!file.startsWith(base + path.sep) || !fs.existsSync(file) || !fs.statSync(file).isFile()) {
            res.writeHead(404); return res.end();
        }
        const mime = { '.js': 'text/javascript', '.css': 'text/css', '.woff2': 'font/woff2', '.woff': 'font/woff', '.ttf': 'font/ttf' };
        res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream' });
        fs.createReadStream(file).pipe(res);
    });
    return new Promise((resolve, reject) => {
        server.once('error', reject);
        server.listen(port, '127.0.0.1', () => resolve(server));
    });
}
module.exports = { start };
if (require.main === module) start(Number(process.env.PORT || 5187))
    .then(server => console.log('Mock-data account preview: http://127.0.0.1:' + server.address().port + '/admin#!/account'))
    .catch(error => { console.error(error.message); process.exitCode = 1; });
