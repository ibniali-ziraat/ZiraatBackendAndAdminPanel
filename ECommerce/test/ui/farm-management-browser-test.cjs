const assert = require('node:assert/strict');
const fs = require('node:fs'); const os = require('node:os'); const path = require('node:path'); const { spawn } = require('node:child_process');
const { start } = require('./farm-management-preview.cjs');
(async () => {
    const server = await start(0), base = 'http://127.0.0.1:' + server.address().port, temp = fs.mkdtempSync(path.join(os.tmpdir(), 'ziraat-farm-ui-'));
    const chrome = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', ['--headless=new','--disable-gpu','--no-first-run','--remote-debugging-port=0','--user-data-dir='+temp,'about:blank'], { windowsHide:true, stdio:'ignore' });
    let socket;
    try {
        const portFile = path.join(temp,'DevToolsActivePort'); for(let i=0;i<100&&!fs.existsSync(portFile);i++) await new Promise(r=>setTimeout(r,100)); assert.ok(fs.existsSync(portFile));
        const port=fs.readFileSync(portFile,'utf8').split('\n')[0], targets=await (await fetch('http://127.0.0.1:'+port+'/json')).json(); socket=new WebSocket(targets.find(x=>x.type==='page').webSocketDebuggerUrl); await new Promise((r,j)=>{socket.onopen=r;socket.onerror=j;});
        let id=0; const pending=new Map(); socket.onmessage=e=>{const m=JSON.parse(e.data);if(pending.has(m.id)){const p=pending.get(m.id);pending.delete(m.id);m.error?p.reject(new Error(m.error.message)):p.resolve(m.result);}};
        const send=(method,params={})=>new Promise((resolve,reject)=>{const key=++id;pending.set(key,{resolve,reject});socket.send(JSON.stringify({id:key,method,params}));});
        const evalJs=async expression=>(await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true})).result.value;
        const wait=async expression=>{for(let i=0;i<100;i++){if(await evalJs(expression))return;await new Promise(r=>setTimeout(r,100));}throw new Error('UI condition failed: '+expression);};
        await send('Page.enable');
        for (const [route,label] of [['farm-advisories','Advisory Center'],['farm-orchards','Orchard Directory'],['farm-operations','Farm Operations']]) {
            await send('Page.navigate',{url:base+'/admin#!/'+route}); await wait(`document.body.innerText.includes('${label}') && document.querySelectorAll('tbody tr').length === 2`);
            assert.equal(await evalJs("document.body.innerText.includes('{{')"),false);
            for (const [name,width,height] of [['desktop',1440,1000],['mobile',390,844]]) { await send('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false}); const layout=await evalJs(`({page:document.documentElement.scrollWidth,viewport:window.innerWidth,offenders:Array.from(document.querySelectorAll('*')).filter(x=>x.getBoundingClientRect().right>window.innerWidth+1).slice(0,5).map(x=>({tag:x.tagName,cls:x.className,right:x.getBoundingClientRect().right,width:x.getBoundingClientRect().width}))})`); assert.equal(layout.page <= layout.viewport,true,JSON.stringify(layout)); const shot=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true}); fs.writeFileSync(path.join(temp,route+'-'+name+'.png'),Buffer.from(shot.data,'base64')); }
        }
        for (const [route,label] of [['farm-lab-reports','Soil & Water Reports'],['farm-rules','Nutrient Rules'],['farm-workflow-monitor','Workflow Monitor']]) {
            await send('Page.navigate',{url:base+'/admin#!/'+route}); await wait(`document.body.innerText.includes('${label}') && !document.body.innerText.includes('Loading...')`);
            assert.equal(await evalJs("document.body.innerText.includes('{{')"),false);
            if (route === 'farm-workflow-monitor') { await wait("document.body.innerText.includes('Valley Farm Supplies') && document.body.innerText.includes('North Apple Orchard')"); for (const label of ['Support queue','Automation rules']) { await evalJs(`Array.from(document.querySelectorAll('.farm-tab')).find(x=>x.innerText.includes('${label}')).click()`); await wait(`document.body.innerText.includes('${label === 'Support queue' ? 'Aamir Dar' : 'Disease risk'}')`); } }
            for (const [name,width,height] of [['desktop',1440,1000],['mobile',390,844]]) { await send('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false}); const layout=await evalJs(`({page:document.documentElement.scrollWidth,viewport:window.innerWidth,offenders:Array.from(document.querySelectorAll('*')).filter(x=>x.getBoundingClientRect().right>window.innerWidth+1).slice(0,5).map(x=>({tag:x.tagName,cls:x.className,right:x.getBoundingClientRect().right,width:x.getBoundingClientRect().width}))})`); assert.equal(layout.page <= layout.viewport,true,JSON.stringify(layout)); const shot=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true}); fs.writeFileSync(path.join(temp,route+'-'+name+'.png'),Buffer.from(shot.data,'base64')); }
        }
        console.log('Passed: all Farm Management screens and Workflow Monitor tabs render at desktop and mobile widths with no page overflow.'); console.log('Screenshots: '+temp);
    } finally { if(socket)socket.close();chrome.kill();server.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;});
