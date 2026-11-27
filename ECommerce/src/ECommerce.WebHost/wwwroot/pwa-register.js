(function () {
    'use strict';

    if (!('serviceWorker' in navigator)) {
        return;
    }

    window.addEventListener('load', function () {
        navigator.serviceWorker.register('/service-worker.js')
            .catch(function (error) {
                if (window.console && window.console.warn) {
                    window.console.warn('Service worker registration failed.', error);
                }
            });
    });
}());
