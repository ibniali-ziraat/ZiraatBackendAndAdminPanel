/*global angular*/
(function () {
    'use strict';
    angular.module('simplAdmin.core').controller('AdminAccountCtrl',
        ['$http', '$httpParamSerializer', 'translateService', AdminAccountCtrl]);

    function AdminAccountCtrl($http, $httpParamSerializer, translateService) {
        var vm = this;
        vm.translate = translateService;
        vm.account = {};
        vm.profile = {};
        vm.password = {};
        vm.loading = true;

        function errorText(response) {
            if (response.status === 401 || response.status === 403) {
                return vm.translate.get('Your session has expired. Please sign in again.');
            }
            var messages = [];
            angular.forEach(response.data, function (value) {
                if (angular.isArray(value)) {
                    angular.forEach(value, function (item) { if (typeof item === 'string') messages.push(item); });
                } else if (typeof value === 'string') {
                    messages.push(value);
                } else if (value && value.description) {
                    messages.push(value.description);
                }
            });
            return messages.join(' ') || vm.translate.get('Unable to save changes. Please try again.');
        }

        function postForm(url, values) {
            return $http.post(url, $httpParamSerializer(values), {
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
            });
        }

        vm.load = function () {
            vm.loading = true;
            vm.loadError = '';
            return $http.get('user', { headers: { Accept: 'application/json' } }).then(function (response) {
                vm.account = response.data.model;
                vm.profile = { fullName: vm.account.fullName, email: vm.account.email };
            }).catch(function (response) {
                vm.loadError = errorText(response);
            }).finally(function () { vm.loading = false; });
        };

        vm.saveProfile = function (form) {
            if (form.$invalid || vm.savingProfile) return;
            vm.savingProfile = true;
            vm.profileError = '';
            vm.profileSuccess = '';
            return postForm('user/info', vm.profile).then(function () {
                vm.account.fullName = vm.profile.fullName;
                vm.profileSuccess = vm.translate.get('Profile updated.');
                form.$setPristine();
            }).catch(function (response) {
                vm.profileError = errorText(response);
            }).finally(function () { vm.savingProfile = false; });
        };

        vm.savePassword = function (form) {
            if (form.$invalid || vm.savingPassword) return;
            vm.passwordError = '';
            vm.passwordSuccess = '';
            if (vm.password.newPassword !== vm.password.confirmPassword) {
                vm.passwordError = vm.translate.get('Passwords do not match.');
                return;
            }
            vm.savingPassword = true;
            return postForm(vm.account.hasPassword ? 'user/change-password' : 'user/set-password', vm.password)
                .then(function () {
                    vm.account.hasPassword = true;
                    vm.password = {};
                    vm.showPasswords = false;
                    vm.passwordSuccess = vm.translate.get('Password updated.');
                    form.$setPristine();
                    form.$setUntouched();
                }).catch(function (response) {
                    vm.passwordError = errorText(response);
                }).finally(function () { vm.savingPassword = false; });
        };

        vm.load();
    }
})();
