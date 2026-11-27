/*global angular*/
(function () {
    angular
        .module('simplAdmin.core')
        .controller('StateProvinceFormCtrl', ['$state', '$stateParams', 'stateProvinceService', 'translateService', '$document', '$timeout', StateProvinceFormCtrl]);

    function StateProvinceFormCtrl($state, $stateParams, stateProvinceService, translateService, $document, $timeout) {
        var vm = this;
        vm.translate = translateService;
        vm.stateProvince = {};
        vm.stateProvinceId = $stateParams.id;
        vm.countryId = $stateParams.countryId;
        vm.isEditMode = vm.stateProvinceId > 0;
        vm.activeTab = vm.isEditMode && $stateParams.tab === 'districts' ? 'districts' : 'state';
        vm.districts = [];
        vm.district = {};
        vm.districtSearch = '';
        vm.validationErrors = [];
        vm.districtErrors = [];

        vm.selectTab = function (tab) {
            if (!vm.isSavingState && !vm.isAddingDistrict) {
                vm.activeTab = tab;
            }
        };

        vm.onTabKeydown = function (event) {
            if (event.key === 'ArrowLeft' || event.key === 'ArrowRight' || event.key === 'Home' || event.key === 'End') {
                event.preventDefault();
                var tab = event.key === 'Home' ? 'state' : event.key === 'End' ? 'districts' : vm.activeTab === 'state' ? 'districts' : 'state';
                vm.selectTab(tab);
                $document[0].getElementById(vm.activeTab + '-tab').focus();
            }
        };

        vm.save = function (form) {
            if (vm.isSavingState || vm.isLoadingState || vm.stateLoadErrors) {
                return;
            }
            if (form && form.$invalid) {
                form.$setSubmitted();
                return;
            }

            vm.isSavingState = true;
            vm.validationErrors = [];
            var promise;
            if (vm.isEditMode) {
                promise = stateProvinceService.editStateProvince(vm.stateProvince);
            } else {
                vm.stateProvince.countryId = vm.countryId;
                promise = stateProvinceService.createStateProvince(vm.stateProvince);
            }
            return promise.then(function () {
                $state.go('country-states-provinces', { countryId: vm.countryId });
            }).catch(function (response) {
                vm.validationErrors = errorsFrom(response, 'Could not save State or Province.');
            }).finally(function () {
                vm.isSavingState = false;
            });
        };

        vm.loadDistricts = function () {
            if (!vm.isEditMode || vm.isLoadingDistricts || vm.isAddingDistrict) {
                return;
            }
            vm.isLoadingDistricts = true;
            vm.districtLoadErrors = null;
            return stateProvinceService.getDistricts(vm.stateProvinceId).then(function (result) {
                if (angular.isArray(result.data)) {
                    vm.districts = result.data;
                } else {
                    vm.districtLoadErrors = [translateService.get('Could not load districts.')];
                }
            }).catch(function (response) {
                vm.districtLoadErrors = errorsFrom(response, 'Could not load districts.');
            }).finally(function () {
                vm.isLoadingDistricts = false;
            });
        };

        vm.addDistrict = function (form) {
            if (!vm.isEditMode || vm.isAddingDistrict || vm.isLoadingState || vm.stateLoadErrors || vm.isLoadingDistricts || vm.districtLoadErrors) {
                return;
            }
            if (!form || form.$invalid || !vm.district.name || !vm.district.name.trim()) {
                if (form) {
                    form.$setSubmitted();
                }
                return;
            }

            vm.isAddingDistrict = true;
            vm.districtErrors = [];
            vm.districtSuccess = null;
            var payload = {
                name: vm.district.name.trim(),
                type: 'District',
                location: vm.savedStateName
            };
            return stateProvinceService.addDistrict(vm.stateProvinceId, payload).then(function (result) {
                vm.districts.push(result.data);
                vm.district = {};
                vm.districtSearch = '';
                form.$setPristine();
                form.$setUntouched();
                vm.districtSuccess = translateService.get('District added.');
                $timeout(function () {
                    var input = $document[0].getElementById('district-name');
                    if (input) {
                        input.focus();
                    }
                });
            }).catch(function (response) {
                vm.districtErrors = errorsFrom(response, 'Could not add district.');
            }).finally(function () {
                vm.isAddingDistrict = false;
            });
        };

        vm.loadState = function () {
            if (vm.isLoadingState) {
                return;
            }
            vm.isLoadingState = true;
            vm.stateLoadErrors = null;
            return stateProvinceService.getStateProvince(vm.stateProvinceId).then(function (result) {
                vm.stateProvince = result.data;
                vm.countryId = result.data.countryId;
                vm.savedStateName = result.data.name;
                vm.loadDistricts();
            }).catch(function (response) {
                vm.stateLoadErrors = errorsFrom(response, 'Could not load State or Province.');
            }).finally(function () {
                vm.isLoadingState = false;
            });
        };

        function errorsFrom(response, fallback) {
            if (response && response.status === 401) {
                return [translateService.get('Your session has expired. Please sign in again.')];
            }
            if (response && response.status === 403) {
                return [translateService.get('Administrator access is required.')];
            }
            var data = response && response.data;
            if (!data || !angular.isObject(data)) {
                return [translateService.get(fallback)];
            }
            var message = data.error || data.Error || data.message || data.Message;
            if (angular.isString(message)) {
                return [message];
            }
            var errors = [];
            angular.forEach(data.errors || data, function (value) {
                if (angular.isArray(value)) {
                    angular.forEach(value, function (item) {
                        if (angular.isString(item)) {
                            errors.push(item);
                        }
                    });
                }
            });
            return errors.length ? errors : [translateService.get(fallback)];
        }

        if (vm.isEditMode) {
            vm.loadState();
        }
    }
})();
