const assert = require('node:assert/strict');
const test = require('node:test');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { createPreviewServer } = require('./district-ui-preview.cjs');
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright-core');

test('state and district admin workflows in a real browser', async t => {
    const { server, requests } = createPreviewServer();
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const browser = await chromium.launch({
        executablePath: process.env.BROWSER_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe',
        headless: true
    });
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    page.setDefaultTimeout(10000);
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error' && /\[\$|Error:/.test(message.text())) errors.push(message.text()); });
    const base = 'http://127.0.0.1:' + server.address().port;
    const edit = base + '/#!/countries/IN/state-province/edit/10?tab=districts';
    const submit = page.locator('form[name="districtForm"] button[type="submit"]');
    const visible = selector => page.locator(selector).waitFor({ state: 'visible' });
    try {
        await t.test('state list shortcut opens the selected state districts', async () => {
            await page.goto(base + '/#!/countries/IN/states-provinces');
            await page.locator('.dropdown-toggle').click();
            await page.locator('.dropdown-menu a').filter({ hasText: 'Districts' }).click();
            await visible('#districts-panel');
            await visible('td:text-is("Srinagar")');
            assert.equal(await page.locator('#districts-tab').getAttribute('aria-selected'), 'true');
            assert.equal(await submit.isDisabled(), true);
            assert.equal(await page.locator('#district-type').inputValue(), 'District');
            assert.equal(await page.locator('#district-location').inputValue(), 'Jammu and Kashmir');
            assert.equal(await page.locator('#district-type').isDisabled(), true);
            assert.equal(await page.locator('#district-location').isDisabled(), true);
        });
        await t.test('add uses the parent route, refresh persists it, duplicate retains input', async () => {
            await page.locator('#district-name').fill('  Budgam  ');
            await submit.click();
            await visible('td:text-is("Budgam")');
            const post = requests.find(r => r.method === 'POST' && r.route.endsWith('/districts'));
            assert.deepEqual(post.data, { name: 'Budgam', type: 'District', location: 'Jammu and Kashmir' });
            assert.equal(post.route, '/api/states-provinces/10/districts');
            assert.equal(await page.locator('#district-name').inputValue(), '');
            assert.equal(await page.locator('#district-type').inputValue(), 'District');
            assert.equal(await page.locator('#district-location').inputValue(), 'Jammu and Kashmir');
            await page.getByRole('button', { name: 'Refresh districts' }).click();
            await visible('td:text-is("Budgam")');
            await page.locator('#district-name').fill('budgam');
            await submit.click();
            await visible('.alert-danger li:text-is("A district with this name already exists in the state or province.")');
            assert.equal(await page.locator('#district-name').inputValue(), 'budgam');
            assert.equal(await page.locator('#district-location').inputValue(), 'Jammu and Kashmir');
            assert.equal(requests.filter(r => r.method === 'PUT').length, 0);
        });
        await t.test('search and unsaved state edits survive tab switches', async () => {
            await page.locator('#district-search').fill('missing district');
            await visible('td:text-is("No matching districts.")');
            await page.locator('#district-search').fill('srin');
            assert.equal(await page.locator('tbody tr:visible').count(), 1);
            await page.locator('#state-tab').click();
            await page.locator('#state-name').fill('Unsaved state name');
            await page.locator('#state-tab').press('ArrowRight');
            await visible('#districts-panel');
            assert.equal(await page.locator('#district-location').inputValue(), 'Jammu and Kashmir');
            await page.locator('#districts-tab').press('Home');
            assert.equal(await page.locator('#state-name').inputValue(), 'Unsaved state name');
            assert.equal(await page.locator('.state-province-editor__header p').textContent(), 'Jammu and Kashmir');
        });
        await t.test('load failure blocks creation and retry recovers', async () => {
            await page.route('**/api/states-provinces/10/districts', route => route.fulfill({ status: 503, json: {} }), { times: 1 });
            await page.goto(edit);
            await page.reload();
            await visible('.alert-danger');
            assert.equal(await page.locator('#district-name').isDisabled(), true);
            await page.getByRole('button', { name: 'Retry', exact: true }).click();
            await visible('td:text-is("Srinagar")');
            assert.equal(await page.locator('#district-name').isEnabled(), true);
        });
        await t.test('authorization errors preserve input and in-flight add prevents duplicates', async () => {
            let release;
            let started;
            const entered = new Promise(resolve => { started = resolve; });
            const gate = new Promise(resolve => { release = resolve; });
            await page.route('**/api/states-provinces/10/districts', async route => {
                started();
                await gate;
                await route.fulfill({ status: 403, json: {} });
            }, { times: 1 });
            await page.locator('#district-name').fill('Restricted district');
            await submit.click();
            await entered;
            assert.equal(await submit.isDisabled(), true);
            assert.equal(await page.locator('#state-tab').isDisabled(), true);
            release();
            await visible('.alert-danger li:text-is("Administrator access is required.")');
            assert.equal(await page.locator('#district-name').inputValue(), 'Restricted district');
        });
        await t.test('new state still saves and has no district tab until it exists', async () => {
            await page.goto(base + '/#!/countries/IN/state-province/create');
            await visible('#state-name');
            assert.equal(await page.locator('#districts-tab').count(), 0);
            await page.locator('#state-name').fill('New state');
            await page.getByRole('button', { name: 'Save', exact: true }).click();
            await page.waitForURL('**/states-provinces');
            assert.equal(requests.find(r => r.method === 'POST' && r.route === '/api/states-provinces/').data.countryId, 'IN');
        });
        await t.test('state save remains independent of districts', async () => {
            await page.goto(base + '/#!/countries/IN/state-province/edit/10');
            await page.waitForFunction(() => document.querySelector('#state-name')?.value === 'Jammu and Kashmir');
            await page.locator('#state-code').fill('JK-UPDATED');
            await page.getByRole('button', { name: 'Save', exact: true }).click();
            await page.waitForURL('**/states-provinces');
            const saved = requests.find(r => r.method === 'PUT');
            assert.equal(saved.route, '/api/states-provinces/10');
            assert.equal(saved.data.code, 'JK-UPDATED');
            assert.equal(saved.data.districts, undefined);
        });
        await t.test('API field errors show complete messages and preserve district values', async () => {
            await page.goto(edit);
            await visible('td:text-is("Srinagar")');
            await page.locator('#district-name').fill('   ');
            await page.locator('#district-search').focus();
            assert.equal(await submit.isDisabled(), true);
            await visible('#district-name-error');
            await page.route('**/api/states-provinces/10/districts', route => route.fulfill({
                status: 400, json: { errors: { Name: ['The district name is invalid.'], Type: ['The type is invalid.'] } }
            }), { times: 1 });
            await page.locator('#district-name').fill('Retained name');
            await submit.click();
            await visible('.alert-danger li:text-is("The district name is invalid.")');
            await visible('.alert-danger li:text-is("The type is invalid.")');
            assert.equal(await page.locator('#district-name').inputValue(), 'Retained name');
        });
        await t.test('missing state blocks district creation and retry reloads the parent', async () => {
            await page.route('**/api/states-provinces/10', route => route.fulfill({ status: 404, json: {} }), { times: 1 });
            await page.reload();
            await visible('.alert-danger');
            assert.equal(await page.locator('#district-name').isDisabled(), true);
            await page.getByRole('button', { name: 'Retry', exact: true }).click();
            await visible('td:text-is("Srinagar")');
        });
        await t.test('desktop and mobile layout fits long district values', async () => {
            await page.goto(edit);
            await visible('td:text-is("Srinagar")');
            await page.locator('#district-name').fill('A'.repeat(450));
            await submit.click();
            await visible('td:text-is("' + 'A'.repeat(450) + '")');
            const folder = path.join(os.tmpdir(), 'ziraat-ui-check');
            fs.mkdirSync(folder, { recursive: true });
            for (const width of [1440, 390, 320]) {
                await page.setViewportSize({ width, height: 1000 });
                const dimensions = await page.evaluate(() => ({
                    page: document.documentElement.scrollWidth,
                    viewport: innerWidth,
                    overlapping: Array.from(document.querySelectorAll('.state-province-editor button, .state-province-editor th, .state-province-editor td'))
                        .filter(el => el.offsetWidth && el.scrollWidth > el.clientWidth + 2).map(el => el.textContent.trim())
                }));
                assert.ok(dimensions.page <= dimensions.viewport, JSON.stringify(dimensions));
                assert.deepEqual(dimensions.overlapping, []);
                await page.screenshot({ path: path.join(folder, 'districts-' + width + '.png'), fullPage: true });
            }
        });
        assert.deepEqual(errors, []);
    } finally {
        await browser.close();
        await new Promise(resolve => server.close(resolve));
    }
});
