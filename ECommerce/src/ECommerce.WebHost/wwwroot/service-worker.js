/* global self, caches, fetch */
(function () {
    'use strict';

    var cacheVersion = 'invenzo-pwa-v1';
    var staticCacheName = cacheVersion + '-static';
    var offlineUrl = '/offline.html';
    var staticAssets = [
        offlineUrl,
        '/manifest.json',
        '/themes/CozaStore/images/icons/favicon.png',
        '/themes/CozaStore/images/icons/logo-01.png',
        '/lib/bootstrap4/js/bootstrap.min.js',
        '/lib/toastr/build/toastr.min.js'
    ];

    function isGetRequest(request) {
        return request && request.method === 'GET';
    }

    function isApiOrAdminRequest(url) {
        return url.pathname.indexOf('/api/') === 0 ||
            url.pathname.indexOf('/admin') === 0 ||
            url.pathname.indexOf('/hubs/') === 0 ||
            url.pathname.indexOf('/identity') === 0;
    }

    function isStaticRequest(url) {
        return url.pathname.indexOf('/lib/') === 0 ||
            url.pathname.indexOf('/themes/') === 0 ||
            url.pathname.indexOf('/_content/') === 0 ||
            url.pathname.indexOf('/js/') === 0 ||
            /\.(css|js|png|jpg|jpeg|gif|svg|webp|ico|woff|woff2|ttf)$/i.test(url.pathname);
    }

    self.addEventListener('install', function (event) {
        event.waitUntil(
            caches.open(staticCacheName)
                .then(function (cache) {
                    return cache.addAll(staticAssets);
                })
                .then(function () {
                    return self.skipWaiting();
                })
        );
    });

    self.addEventListener('activate', function (event) {
        event.waitUntil(
            caches.keys()
                .then(function (keys) {
                    return Promise.all(keys.map(function (key) {
                        if (key.indexOf(cacheVersion) !== 0) {
                            return caches.delete(key);
                        }

                        return Promise.resolve();
                    }));
                })
                .then(function () {
                    return self.clients.claim();
                })
        );
    });

    self.addEventListener('fetch', function (event) {
        var request = event.request;
        var url = new URL(request.url);

        if (!isGetRequest(request) || url.origin !== self.location.origin || isApiOrAdminRequest(url)) {
            return;
        }

        if (request.mode === 'navigate') {
            event.respondWith(
                fetch(request)
                    .then(function (response) {
                        return response;
                    })
                    .catch(function () {
                        return caches.match(offlineUrl);
                    })
            );
            return;
        }

        if (isStaticRequest(url)) {
            event.respondWith(
                caches.match(request)
                    .then(function (cachedResponse) {
                        var networkResponse = fetch(request)
                            .then(function (response) {
                                if (response && response.ok) {
                                    caches.open(staticCacheName)
                                        .then(function (cache) {
                                            cache.put(request, response.clone());
                                        });
                                }

                                return response;
                            })
                            .catch(function () {
                                return cachedResponse;
                            });

                        return cachedResponse || networkResponse;
                    })
            );
        }
    });
}());
