/*global window, document*/
(function () {
    'use strict';

    function toTitleCase(value) {
        return value.replace(/\s+/g, ' ').trim().replace(/\b\w/g, function (character) {
            return character.toUpperCase();
        });
    }

    function getActionLabel(element) {
        var tooltip = element.getAttribute('data-original-title') || '';
        var title = (element.getAttribute('title') || '').toLowerCase();
        var text = (element.textContent || '').trim().toLowerCase();
        var sourceLabel = (tooltip || element.getAttribute('title') || element.textContent || '').trim();

        if (title.indexOf('configure') > -1 || text === 'configure') {
            return 'Configure';
        }

        if (title.indexOf('enable') > -1 || text === 'enable') {
            return 'Enable';
        }

        if (title.indexOf('disable') > -1 || text === 'disable') {
            return 'Disable';
        }

        if (title.indexOf('publish') > -1 || text === 'publish') {
            return 'Publish';
        }

        if (title.indexOf('un publish') > -1 || title.indexOf('unpublish') > -1 || text === 'un publish' || text === 'unpublish') {
            return 'Unpublish';
        }

        if (title.indexOf('approve') > -1 || text === 'approve') {
            return 'Approve';
        }

        if (title.indexOf('edit') > -1 || text === 'edit') {
            return 'Edit';
        }

        if (title.indexOf('delete') > -1 || element.classList.contains('delete') || text === 'delete') {
            return 'Delete';
        }

        if (title.indexOf('view') > -1 || text === 'view') {
            return 'View';
        }

        if (element.querySelector('.glyphicon-pencil')) {
            return 'Edit';
        }

        if (element.querySelector('.glyphicon-remove')) {
            return 'Delete';
        }

        if (element.querySelector('.glyphicon-eye-open') || element.querySelector('.glyphicon-search') || element.querySelector('.fa-eye')) {
            return 'View';
        }

        if (element.querySelector('.glyphicon-cog') || element.querySelector('.fa-cog')) {
            return 'Configure';
        }

        if (element.querySelector('.glyphicon-pause')) {
            return 'Disable';
        }

        if (element.querySelector('.glyphicon-play')) {
            return 'Enable';
        }

        if (sourceLabel) {
            return toTitleCase(sourceLabel);
        }

        return 'Action';
    }

    function isEligibleAction(element) {
        if (!element || !element.matches('a,button')) {
            return false;
        }

        if (!element.classList.contains('btn-xs') ||
            element.classList.contains('btn-translate') ||
            element.classList.contains('dropdown-toggle') ||
            element.hasAttribute('data-admin-grid-action-item')) {
            return false;
        }

        return true;
    }

    function buildMenuAction(element) {
        var label = getActionLabel(element);
        var text = (element.textContent || '').replace(/\s+/g, ' ').trim();

        element.classList.remove('btn');
        element.classList.remove('btn-primary');
        element.classList.remove('btn-danger');
        element.classList.remove('btn-default');
        element.classList.remove('btn-info');
        element.classList.remove('btn-success');
        element.classList.remove('btn-warning');
        element.classList.remove('btn-xs');
        element.classList.remove('delete');
        element.classList.add('admin-grid-actions__link');
        element.setAttribute('data-admin-grid-action-item', 'true');

        if (element.tagName === 'BUTTON') {
            element.type = 'button';
        }

        if (!text && !element.querySelector('.admin-grid-actions__text')) {
            var textNode = document.createElement('span');
            textNode.className = 'admin-grid-actions__text';
            textNode.textContent = label;
            element.appendChild(textNode);
        }

        return element;
    }

    function processCell(cell) {
        if (!cell) {
            return;
        }

        var candidates = Array.prototype.filter.call(cell.querySelectorAll('a,button'), function (element) {
            return isEligibleAction(element) && !element.closest('[data-admin-grid-actions]');
        });

        if (!candidates.length) {
            return;
        }

        var wrapper = cell.querySelector('[data-admin-grid-actions]');
        var menu;

        if (!wrapper) {
            wrapper = document.createElement('div');
            wrapper.className = 'dropdown admin-grid-actions';
            wrapper.setAttribute('data-admin-grid-actions', 'true');

            var toggle = document.createElement('button');
            toggle.type = 'button';
            toggle.className = 'btn btn-default btn-xs dropdown-toggle admin-grid-actions__toggle';
            toggle.setAttribute('data-toggle', 'dropdown');
            toggle.setAttribute('aria-expanded', 'false');
            toggle.setAttribute('aria-label', 'Row actions');
            toggle.innerHTML = '<span class="fa fa-cog" aria-hidden="true"></span>';

            menu = document.createElement('ul');
            menu.className = 'dropdown-menu dropdown-menu-right admin-grid-actions__menu';

            var title = document.createElement('li');
            title.className = 'admin-grid-actions__title';
            title.textContent = 'Options';

            wrapper.appendChild(toggle);
            wrapper.appendChild(menu);
            menu.appendChild(title);
            cell.insertBefore(wrapper, candidates[0]);
        } else {
            menu = wrapper.querySelector('.admin-grid-actions__menu');
        }

        cell.classList.add('admin-grid-actions-cell');

        candidates.forEach(function (candidate) {
            var item = document.createElement('li');
            item.appendChild(buildMenuAction(candidate));
            menu.appendChild(item);
        });
    }

    function scanTables() {
        var cells = document.querySelectorAll('table td, table th');
        Array.prototype.forEach.call(cells, processCell);
    }

    function debounce(fn, delay) {
        var timeoutId;
        return function () {
            window.clearTimeout(timeoutId);
            timeoutId = window.setTimeout(fn, delay);
        };
    }

    var scheduleScan = debounce(scanTables, 50);

    var observer;

    function initialize() {
        scanTables();

        if (!window.MutationObserver || observer || !document.body) {
            return;
        }

        observer = new MutationObserver(scheduleScan);
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize, { once: true });
    } else {
        initialize();
    }
}());
