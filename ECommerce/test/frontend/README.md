# District Admin UI Checks

The production screen uses the existing authenticated admin session and the real
state/district API. No UI fixtures are bundled with the application.

## Browser Regression Test

Requires Node.js, Chrome (or BROWSER_PATH), and playwright-core. In PowerShell,
from the repository root:

```powershell
npm install --prefix "$env:TEMP\ziraat-ui-check" --no-save --package-lock=false playwright-core
$env:PLAYWRIGHT_MODULE = "$env:TEMP\ziraat-ui-check\node_modules\playwright-core"
node --test ECommerce/test/frontend/district-ui.test.cjs
```

The test serves the real AngularJS templates, controllers, services and CSS
against an in-memory mock API. It checks navigation, creation, duplicates,
search, loading failures, authorization errors, pending requests, state creation
and desktop/mobile overflow. Screenshots go to TEMP/ziraat-ui-check.
This does not test live API authorization or database persistence.

## Isolated Preview

```powershell
node ECommerce/test/frontend/district-ui-preview.cjs
```

Open http://127.0.0.1:4318. PORT overrides the default port. The preview binds to
loopback, serves only allowlisted UI assets, uses temporary in-memory sample
data, and does not start the .NET application or its background jobs.

## Administrator Workflow

1. Open States/Provinces and select the country.
2. Open the state's action menu and select Districts, or choose Edit and then
   the Districts tab.
3. Enter Name (required), then Add District. Type is fixed to District and
   Location is filled with the saved state name. Both fields are greyed out.
4. Search or refresh the list as needed.

Create a state first before adding its districts. District creation calls
POST /api/states-provinces/{stateOrProvinceId}/districts and requires the admin
role. No database migration is required for this UI change.
