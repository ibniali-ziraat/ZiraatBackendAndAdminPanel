/*global angular, FormData*/
(function () {
    angular
        .module('simplAdmin.common')
        .factory('summerNoteService', summerNoteService);

    /* @ngInject */
    function summerNoteService($http) {
        var service = {
            upload : upload
        };
        return service;

        function upload(file) {
            if (!file) {
                return Promise.reject({ data: { message: 'Please select an image to upload.' } });
            }

            var data = new FormData();
            data.append("file", file);
            return $http.post('/api/common/upload', data, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function (response) {
                var url = angular.isString(response.data) ? response.data : response.data && response.data.url;

                if (!url) {
                    return Promise.reject({ data: { message: 'The server did not return an image URL.' } });
                }

                response.data = url;
                return response;
            });
        }
    }
})();
