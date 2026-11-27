const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');
const { start } = require('./admin-account-preview.cjs');

(async () => {
    const server = await start(0);
    const base = 'http://127.0.0.1:' + server.address().port;
    const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'ziraat-account-ui-'));
    const chrome = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', [
        '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
        '--remote-debugging-port=0', '--user-data-dir=' + temp, 'about:blank'
    ], { windowsHide: true, stdio: 'ignore' });
    let socket;
    try {
        const activePortFile = path.join(temp, 'DevToolsActivePort');
        for (let i = 0; i < 100 && !fs.existsSync(activePortFile); i++) await new Promise(r => setTimeout(r, 100));
        assert.ok(fs.existsSync(activePortFile), 'Chrome started');
        const port = fs.readFileSync(activePortFile, 'utf8').split('\n')[0];
        const targets = await (await fetch('http://127.0.0.1:' + port + '/json')).json();
        const target = targets.find(x => x.type === 'page');
        socket = new WebSocket(target.webSocketDebuggerUrl);
        await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
        let id = 0;
        const pending = new Map();
        socket.onmessage = event => {
            const message = JSON.parse(event.data);
            if (pending.has(message.id)) {
                const request = pending.get(message.id);
                pending.delete(message.id);
                clearTimeout(request.timer);
                if (message.error) request.reject(new Error(message.error.message));
                else request.resolve(message.result);
            }
        };
        const send = (method, params = {}) => new Promise((resolve, reject) => {
            const key = ++id;
            const timer = setTimeout(() => { pending.delete(key); reject(new Error('CDP timeout: ' + method)); }, 15000);
            pending.set(key, { resolve, reject, timer });
            socket.send(JSON.stringify({ id: key, method, params }));
        });
        const evaluate = async expression => {
            const result = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
            if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
            return result.result.value;
        };
        const waitFor = async expression => {
            for (let i = 0; i < 100; i++) {
                if (await evaluate(expression)) return;
                await new Promise(r => setTimeout(r, 100));
            }
            throw new Error('UI condition failed: ' + expression);
        };
        await send('Page.enable');
        await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
        await send('Page.navigate', { url: base + '/admin#!/account' });
        await waitFor("document.querySelector('#account-name') && document.querySelector('#account-name').value === 'Admin'");
        assert.equal(await evaluate("document.querySelector('#account-email').readOnly"), true);
        assert.equal(await evaluate("document.body.innerText.includes('{{')"), false);

        const setValue = (selector, value) => evaluate(`(() => {const input=document.querySelector(${JSON.stringify(selector)}); input.value=${JSON.stringify(value)}; input.dispatchEvent(new Event('input',{bubbles:true})); input.dispatchEvent(new Event('change',{bubbles:true}));})()`);
        await setValue('#account-name', 'Updated Admin');
        await evaluate("document.querySelector('form[name=profileForm] button[type=submit]').click()");
        await waitFor("document.body.innerText.includes('Profile updated.')");
        assert.equal((await (await fetch(base + '/user')).json()).model.fullName, 'Updated Admin');

        await setValue('#account-current-password', 'wrong-password');
        await setValue('#account-new-password', 'new-password');
        await setValue('#account-confirm-password', 'not-matching');
        await evaluate("document.querySelector('form[name=passwordForm] button[type=submit]').click()");
        await waitFor("document.body.innerText.includes('Passwords do not match.')");
        await setValue('#account-confirm-password', 'new-password');
        await evaluate("document.querySelector('form[name=passwordForm] button[type=submit]').click()");
        await waitFor("document.body.innerText.includes('Incorrect current password.')");
        await setValue('#account-current-password', 'current-password');
        await evaluate("document.querySelector('form[name=passwordForm] button[type=submit]').click()");
        await waitFor("document.body.innerText.includes('Password updated.')");
        assert.equal(await evaluate("document.querySelector('#account-new-password').value"), '');

        for (const [name, width, height] of [['desktop', 1440, 1000], ['mobile', 390, 844]]) {
            await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
            assert.equal(await evaluate('document.documentElement.scrollWidth <= window.innerWidth'), true, name + ' has no horizontal overflow');
            const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true });
            fs.writeFileSync(path.join(temp, name + '.png'), Buffer.from(shot.data, 'base64'));
        }
        await fetch(base + '/test-mode?value=unauthorized');
        await send('Page.reload');
        await waitFor("document.body && document.body.innerText.includes('Your session has expired.')");
        await fetch(base + '/test-mode');
        await evaluate("Array.from(document.querySelectorAll('button')).find(x => x.textContent.includes('Retry')).click()");
        await waitFor("document.querySelector('#account-name')");
        console.log('Passed: desktop/mobile rendering, profile save, password validation/error/success, session error and retry.');
        console.log('Screenshots: ' + temp);
    } finally {
        if (socket) socket.close();
        chrome.kill();
        server.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
