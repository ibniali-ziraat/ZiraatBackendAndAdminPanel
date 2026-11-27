/*global angular, jQuery*/
(function ($) {
    angular
        .module('simplAdmin.catalog')
        .controller('ProductFormCtrl', ['$state', '$timeout', '$stateParams', '$http', '$scope', 'categoryService', 'productService', 'summerNoteService', 'brandService', 'translateService', ProductFormCtrl]);

    function ProductFormCtrl($state, $timeout, $stateParams, $http, $scope, categoryService, productService, summerNoteService, brandService, translateService) {
        var vm = this;
        vm.translate = translateService;
        // declare shoreDescription and description for summernote
        vm.product = { shortDescription: '', description: '', specification: '', isPublished: true, price: 0, isCallForPricing: false, isAllowToOrder: true };
        vm.product.categoryIds = [];
        vm.product.options = [];
        vm.product.variations = [];
        vm.product.attributes = [];
        vm.product.relatedProducts = [];
        vm.product.crossSellProducts = [];
        vm.categories = [];
        vm.thumbnailImage = null;
        vm.productImages = [];
        vm.productDocuments = [];
        vm.enhancingImage = null;
        vm.enhancingNewImage = null;
        vm.options = [];
        vm.productTemplates = [];
        vm.addingOption = null;
        vm.attributes = [];
        vm.addingAttribute = null;
        vm.productId = $stateParams.id;
        vm.isEditMode = vm.productId > 0;
        vm.addingVariation = { price: 0 };
        vm.brands = [];
        vm.taxClasses = [];
        vm.phoneSync = {
            sessionId: '',
            mobileUrl: '',
            qrCodeUrl: '',
            status: 'Not connected',
            receivedFiles: [],
            latestFile: null
        };
        vm.phoneSyncConnection = null;
        vm.phoneSyncConnectionPromise = null;
        vm.voiceNote = {
            isProcessing: false,
            isRecording: false,
            isRecordingSupported: !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.MediaRecorder),
            mediaRecorder: null,
            stream: null,
            chunks: [],
            error: ''
        };
        vm.giTagValidation = {
            status: 'missing',
            message:'',// 'Add a GI Tag attribute before saving.',
            value: ''
        };
        vm.giTagValidationTimer = null;
        vm.giTagValidationRequestId = 0;

        vm.datePickerSpecialPriceStart = {};
        vm.datePickerSpecialPriceEnd = {};

        vm.updateSlug = function () {
            vm.product.slug = slugify(vm.product.name);
        };

        vm.openCalendar = function (e, picker) {
            vm[picker].open = true;
        };

        vm.generateThumbnail = function () {
            if (!vm.productImages || vm.productImages.length === 0) {
                toastr.warning('Please select at least one product image first');
                return;
            }

            var imageFile = vm.productImages[0];
            productService.generateThumbnail(imageFile).then(
                function (response) {
                    if (response.data && response.data.data) {
                        var file = base64ToFile(response.data.data, response.data.fileName, response.data.type);
                        vm.thumbnailImage = file;
                        toastr.success('Thumbnail generated successfully');
                    } else {
                        toastr.error('No response from thumbnail generation');
                    }
                },
                function (error) {
                    toastr.error('Failed to generate thumbnail: ' + (error.data && error.data.error ? error.data.error : 'Unknown error'));
                    console.error(error);
                }
            );
        };

        vm.enhanceImage = function (image) {
            if (!image || !image.mediaUrl) {
                toastr.warning('Invalid image');
                return;
            }

            var confirmEnhance = confirm('Enhance this image? This will replace the current image.');
            if (!confirmEnhance) {
                return;
            }

            var imageIndex = vm.product.productImages.indexOf(image);
            if (imageIndex === -1) {
                toastr.error('Image not found');
                return;
            }

            vm.enhancingImage = image;
            toastr.info('Enhancing image...');

            $http.get(image.mediaUrl, { responseType: 'blob' })
                .then(function (response) {
                    var sourceFile = new File(
                        [response.data],
                        getFileNameFromUrl(image.mediaUrl),
                        { type: response.data.type || 'image/jpeg' }
                    );

                    return productService.enhanceImage(sourceFile);
                })
                .then(function (response) {
                    var file;

                    if (response.data && response.data.data) {
                        file = base64ToFile(response.data.data, response.data.fileName, response.data.type);
                        vm.product.deletedMediaIds = vm.product.deletedMediaIds || [];
                        vm.product.deletedMediaIds.push(image.id);
                        vm.product.productImages.splice(imageIndex, 1);
                        vm.productImages = vm.productImages || [];
                        vm.productImages.push(file);
                        toastr.success('Image enhanced successfully');
                    } else {
                        toastr.error('No response from image enhancement');
                    }
                })
                .catch(function (error) {
                    toastr.error('Failed to enhance image: ' + (error.data && error.data.error ? error.data.error : 'Unknown error'));
                    console.error(error);
                })
                .finally(function () {
                    vm.enhancingImage = null;
                });
        };

        vm.enhanceNewImage = function (image) {
            if (!image) {
                toastr.warning('Invalid image');
                return;
            }

            var index = vm.productImages.indexOf(image);
            if (index === -1) {
                toastr.warning('Image not found in list');
                return;
            }

            var confirmEnhance = confirm('Enhance this image? This will replace the current image.');
            if (!confirmEnhance) {
                return;
            }

            vm.enhancingNewImage = image;

            productService.enhanceImage(image).then(
                function (response) {
                    if (response.data && response.data.data) {
                        var file = base64ToFile(response.data.data, response.data.fileName, response.data.type);
                        vm.productImages[index] = file;
                        toastr.success('Image enhanced successfully');
                    } else {
                        toastr.error('No response from image enhancement');
                    }
                },
                function (error) {
                    toastr.error('Failed to enhance image: ' + (error.data && error.data.error ? error.data.error : 'Unknown error'));
                    console.error(error);
                }
            ).finally(function () {
                vm.enhancingNewImage = null;
            });
        };

        vm.shortDescUpload = function (files) {
            summerNoteService.upload(files[0])
                .then(function (response) {
                    $(vm.shortDescEditor).summernote('insertImage', response.data);
                });
        };

        vm.descUpload = function (files) {
            summerNoteService.upload(files[0])
                .then(function (response) {
                    $(vm.descEditor).summernote('insertImage', response.data);
                });
        };

        vm.specUpload = function (files) {
            summerNoteService.upload(files[0])
                .then(function (response) {
                    $(vm.specEditor).summernote('insertImage', response.data);
                });
        };

        vm.processVoiceNote = function processVoiceNote(audioFile) {
            if (!audioFile) {
                return;
            }

            vm.voiceNote.error = '';
            vm.voiceNote.isProcessing = true;
            toastr.info('Converting voice note to English...');

            productService.processVoiceNote(audioFile)
                .then(function (response) {
                    var text = extractVoiceNoteText(response.data);

                    if (!text) {
                        vm.voiceNote.error = 'The voice note was processed, but no English text was returned.';
                        toastr.warning(vm.voiceNote.error);
                        return;
                    }

                    appendDescriptionText(text);
                    toastr.success('Voice note added to description');
                })
                .catch(function (error) {
                    vm.voiceNote.error = getVoiceNoteError(error);
                    toastr.error(vm.voiceNote.error);
                    console.error(error);
                })
                .finally(function () {
                    vm.voiceNote.isProcessing = false;
                });
        };

        vm.startVoiceRecording = function startVoiceRecording() {
            if (!vm.voiceNote.isRecordingSupported) {
                vm.voiceNote.error = 'Voice recording is not supported in this browser.';
                toastr.error(vm.voiceNote.error);
                return;
            }

            vm.voiceNote.error = '';
            vm.voiceNote.chunks = [];

            navigator.mediaDevices.getUserMedia({ audio: true })
                .then(function (stream) {
                    var options = getAudioRecorderOptions();

                    vm.voiceNote.stream = stream;
                    vm.voiceNote.mediaRecorder = options ? new MediaRecorder(stream, options) : new MediaRecorder(stream);
                    vm.voiceNote.mediaRecorder.ondataavailable = function (event) {
                        if (event.data && event.data.size > 0) {
                            vm.voiceNote.chunks.push(event.data);
                        }
                    };
                    vm.voiceNote.mediaRecorder.onstop = onVoiceRecordingStopped;
                    vm.voiceNote.mediaRecorder.start();

                    $timeout(function () {
                        vm.voiceNote.isRecording = true;
                    });
                })
                .catch(function (error) {
                    $timeout(function () {
                        vm.voiceNote.error = 'Microphone access was not allowed.';
                    });
                    toastr.error('Microphone access was not allowed.');
                    console.error(error);
                });
        };

        vm.stopVoiceRecording = function stopVoiceRecording() {
            if (vm.voiceNote.mediaRecorder && vm.voiceNote.mediaRecorder.state !== 'inactive') {
                vm.voiceNote.mediaRecorder.stop();
            }

            vm.voiceNote.isRecording = false;
        };

        vm.addOption = function addOption() {
            onModifyOption(function() {
                vm.addingOption.values = [];
                vm.addingOption.displayType = "text";
                var index = vm.options.indexOf(vm.addingOption);
                vm.product.options.push(vm.addingOption);
                vm.options.splice(index, 1);
                vm.addingOption = null;
            });
        };

        vm.deleteOption = function deleteOption(option) {
            onModifyOption(function() {
                var index = vm.product.options.indexOf(option);
                vm.product.options.splice(index, 1);
                vm.options.push(option);
            });
        };

        function onModifyOption(callback) {
            if (vm.product.variations.length === 0) {
                callback();
                return;
            }

            bootbox.confirm('Add or remove option will clear all existing variations. Are you sure you want to do this?', function (result) {
                if (result) {
                    $timeout(function () {
                        vm.product.variations = [];
                        callback();
                    }, 1);
                }
            });
        }

        vm.newOptionValue = function (chip) {
            var value = angular.isObject(chip) ? chip.key : chip;

            return {
                key: value,
                display: value
            };
        };

        vm.generateOptionCombination = function generateOptionCombination() {
            if (vm.product.options.some(function (option) { return !option.values || option.values.length === 0; })) {
                toastr.error('Enter option values before generating variations.');
                return;
            }

            if (vm.product.variations.length > 0) {
                bootbox.confirm('Generating combinations will replace existing variations. Are you sure?', function (result) {
                    if (result) {
                        $timeout(generateOptionCombinations, 1);
                    }
                });
                return;
            }

            generateOptionCombinations();
        };

        function generateOptionCombinations() {
            var maxIndexOption = vm.product.options.length - 1;
            vm.product.variations = [];

            function getItemValue(item) {
                return item.value;
            }

            function helper(arr, optionIndex) {
                var j, l, variation, optionCombinations, optionValue;

                for (j = 0, l = vm.product.options[optionIndex].values.length; j < l; j = j + 1) {
                    optionCombinations = arr.slice(0);
                    optionValue = {
                        optionName: vm.product.options[optionIndex].name,
                        optionId: vm.product.options[optionIndex].id,
                        value: vm.product.options[optionIndex].values[j].key,
                        sortIndex : optionIndex
                    };
                    optionCombinations.push(optionValue);

                    if (optionIndex === maxIndexOption) {
                        variation = {
                            name: vm.product.name + ' ' + optionCombinations.map(getItemValue).join(' '),
                            normalizedName : optionCombinations.map(getItemValue).join('-'),
                            optionCombinations: optionCombinations,
                            price: vm.product.price,
                            oldPrice: vm.product.oldPrice
                        };
                        vm.product.variations.push(variation);
                    } else {
                        helper(optionCombinations, optionIndex + 1);
                    }
                }
            }

            helper([], 0);
        }

        vm.deleteVariation = function deleteVariation(variation) {
            var index = vm.product.variations.indexOf(variation);
            vm.product.variations.splice(index, 1);
        };

        vm.removeImage = function removeImage(media) {
            var index = vm.product.productImages.indexOf(media);
            vm.product.productImages.splice(index, 1);
            vm.product.deletedMediaIds.push(media.id);
        };

        vm.removeDocument = function removeDocument(media) {
            var index = vm.product.productDocuments.indexOf(media);
            vm.product.productDocuments.splice(index, 1);
            vm.product.deletedMediaIds.push(media.id);
        };

        vm.getDocumentPreviewUrl = function getDocumentPreviewUrl(file) {
            if (!file) {
                return '';
            }

            if (file.mediaUrl) {
                return file.mediaUrl;
            }

            if (!file.previewUrl) {
                file.previewUrl = URL.createObjectURL(file);
            }

            return file.previewUrl;
        };

        vm.isImageDocument = function isImageDocument(file) {
            var fileName;

            if (!file) {
                return false;
            }

            if ((file.type && file.type.indexOf('image/') === 0) || (file.contentType && file.contentType.indexOf('image/') === 0)) {
                return true;
            }

            fileName = (file.name || file.caption || file.mediaUrl || '').split('?')[0].toLowerCase();

            return /\.(apng|avif|bmp|gif|jpe?g|png|svg|webp)$/i.test(fileName);
        };

        vm.previewDocument = function previewDocument(file) {
            var previewUrl = vm.getDocumentPreviewUrl(file);

            if (previewUrl) {
                window.open(previewUrl, '_blank');
            }
        };

        vm.downloadDocument = function downloadDocument(file) {
            var downloadUrl = vm.getDocumentPreviewUrl(file);
            var link;

            if (!downloadUrl) {
                return;
            }

            link = document.createElement('a');
            link.href = downloadUrl;
            link.download = file.name || file.caption || 'product-document';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        };

        vm.openPhoneSync = function openPhoneSync() {
            if (!window.signalR) {
                toastr.error('SignalR client is not available.');
                return;
            }

            vm.phoneSync.sessionId = createSessionId();
            vm.phoneSync.receivedFiles = [];
            vm.phoneSync.latestFile = null;
            vm.phoneSync.mobileUrl = window.location.origin + '/phone-sync?sessionId=' + encodeURIComponent(vm.phoneSync.sessionId);
            vm.phoneSync.qrCodeUrl = 'https://api.qrserver.com/v1/create-qr-code/?size=220x220&margin=10&data=' + encodeURIComponent(vm.phoneSync.mobileUrl);
            vm.phoneSync.status = 'Connecting...';

            $('#phoneSyncModal')
                .appendTo('body')
                .off('hidden.bs.modal.phoneSync')
                .modal({
                backdrop: true,
                keyboard: true,
                show: true
            });

            ensurePhoneSyncConnection()
                .then(function () {
                    return vm.phoneSyncConnection.invoke('JoinSession', vm.phoneSync.sessionId, 'desktop');
                })
                .then(function () {
                    $timeout(function () {
                        vm.phoneSync.status = 'Waiting for phone';
                    });
                })
                .catch(function () {
                    $timeout(function () {
                        vm.phoneSync.status = 'Could not connect';
                    });
                });
        };

        vm.isAddVariationFormValid = function () {
            var i;
            if (isNaN(vm.addingVariation.price) || vm.addingVariation.price === '') {
                return false;
            }

            for (i = 0; i < vm.product.options.length; i = i + 1) {
                if (!normalizeOptionValue(vm.addingVariation[vm.product.options[i].name])) {
                    return false;
                }
            }

            return true;
        };

        vm.addVariation = function addVariation() {
            var variation,
                optionCombinations = [];

            vm.product.options.forEach(function (option, index) {
                var selectedValue = vm.addingVariation[option.name];

                ensureOptionValue(option, selectedValue);

                var optionValue = {
                    optionName: option.name,
                    optionId: option.id,
                    value: selectedValue,
                    sortIndex: index
                };
                optionCombinations.push(optionValue);
            });

            variation = {
                name: vm.product.name + ' ' + optionCombinations.map(function (item) {
                    return item.value;
                }).join(' '),
                normalizedName : optionCombinations.map(function (item) {
                    return item.value;
                }).join('-'),
                optionCombinations: optionCombinations,
                price: vm.addingVariation.price || vm.product.price,
                oldPrice: vm.addingVariation.oldPrice || vm.product.oldPrice,
                sku: vm.addingVariation.sku,
                gtin: vm.addingVariation.gtin
            };

            if (!vm.product.variations.find(function (item) { return item.name === variation.name; })) {
                vm.product.variations.push(variation);
                vm.addingVariation = { price: vm.product.price, oldPrice: vm.product.oldPrice };
            } else {
                toastr.error('The ' + variation.name + ' has been existing');
            }
        };

        function ensureOptionValue(option, value) {
            var normalizedValue = normalizeOptionValue(value);

            if (!normalizedValue) {
                return;
            }

            option.values = option.values || [];

            if (!option.values.some(function (item) { return normalizeOptionValue(item.key) === normalizedValue; })) {
                option.values.push(vm.newOptionValue(value));
            }
        }

        function normalizeOptionValue(value) {
            return (value || '').toString().trim().toLowerCase();
        }

        // TODO look for a more concise way
        vm.applyTemplate = function applyTemplate() {
            var template, i, index, workingAttr,
                nonTemplateAttrs = [];

            productService.getProductTemplate(vm.product.template.id).then(function (response) {
                template = response.data;

                for (i = 0; i < template.attributes.length; i = i + 1) {
                    workingAttr = vm.product.attributes.find(function (item) { return item && item.id === template.attributes[i].id; });
                    if (workingAttr) {
                        continue;
                    }
                    workingAttr = vm.attributes.find(function (item) { return item && item.id === template.attributes[i].id; });
                    index = vm.attributes.indexOf(workingAttr);
                    vm.attributes.splice(index, 1);
                    vm.product.attributes.push(workingAttr);
                }

                for (i = 0; i < vm.product.attributes.length; i = i + 1) {
                    workingAttr = template.attributes.find(function (item) { return item && item.id === vm.product.attributes[i].id; });
                    if (!workingAttr) {
                        nonTemplateAttrs.push(vm.product.attributes[i]);
                    }
                }

                for (i = 0; i < nonTemplateAttrs.length; i = i + 1) {
                    workingAttr = vm.product.attributes.find(function (item) { return item && item.id === nonTemplateAttrs[i].id; });
                    index = vm.product.attributes.indexOf(workingAttr);
                    vm.product.attributes.splice(index, 1);
                    vm.attributes.push(workingAttr);
                }
            });
        };

        vm.addAttribute = function addAttribute() {
            var index = vm.attributes.indexOf(vm.addingAttribute);
            vm.addingAttribute.value = vm.addingAttribute.value || '';
            vm.product.attributes.push(vm.addingAttribute);
            vm.attributes.splice(index, 1);
            vm.addingAttribute = null;
            scheduleGiTagValidation(0);
        };

        vm.deleteAttribute = function deleteAttribute(attribute) {
            var index = vm.product.attributes.indexOf(attribute);
            vm.product.attributes.splice(index, 1);
            vm.attributes.push(attribute);
            scheduleGiTagValidation(0);
        };

        vm.isGiTagAttribute = function isGiTagAttributeForView(attribute) {
            return isGiTagAttribute(attribute);
        };

        vm.onGiTagValueChanged = function onGiTagValueChanged(attribute) {
            if (isGiTagAttribute(attribute)) {
                scheduleGiTagValidation();
            }
        };

        vm.refreshGiTagValidation = function refreshGiTagValidation() {
            scheduleGiTagValidation(0);
        };

        vm.isGiTagSaveAllowed = function isGiTagSaveAllowed() {
            var giTagAttribute = getGiTagAttribute();
            var value = getGiTagValue();
            var normalizedValue = normalizeGiTagValue(value);
            var response = vm.giTagValidation.response || {};
            var verifiedValues = [
                vm.giTagValidation.value,
                response.tagId,
                response.registrationNumber
            ].map(normalizeGiTagValue);

            if (!giTagAttribute || !normalizedValue) {
                return true;
            }

            return vm.giTagValidation.status === 'verified'
                && verifiedValues.indexOf(normalizedValue) > -1;
        };

        vm.getGiTagSaveBlockMessage = function getGiTagSaveBlockMessage() {
            if (vm.giTagValidation.status === 'missing') {
                return '';
            }

            if (vm.giTagValidation.status === 'empty') {
                return '';
            }

            if (vm.giTagValidation.status === 'validating') {
                return 'Validating GI Tag...';
            }

            if (vm.giTagValidation.status === 'verified') {
                return '';
            }

            return vm.giTagValidation.message || 'GI Tag is not verified.';
        };

        vm.toggleCategories = function toggleCategories(categoryId) {
            var index = vm.product.categoryIds.indexOf(categoryId);
            if (index > -1) {
                vm.product.categoryIds.splice(index, 1);
                var childCategoryIds = getChildCategoryIds(categoryId);
                childCategoryIds.forEach(function spliceChildCategory(childCategoryId) {
                    index = vm.product.categoryIds.indexOf(childCategoryId);
                    if (index > -1) {
                        vm.product.categoryIds.splice(index, 1);
                    }
                });
            } else {
                vm.product.categoryIds.push(categoryId);
                var category = vm.categories.find(function (item) { return item.id === categoryId; });
                if (category) {
                    var parentCategoryIds = getParentCategoryIds(category.parentId);
                    parentCategoryIds.forEach(function pushParentCategory(parentCategoryId) {
                        if (vm.product.categoryIds.indexOf(parentCategoryId) < 0) {
                            vm.product.categoryIds.push(parentCategoryId);
                        }
                    });
                }
            }
        };

        vm.filterAddedOptionValue = function filterAddedOptionValue(item) {
            if (vm.product.options.length > 1) {
                return true;
            }
            var optionValueAdded = false;
            vm.product.variations.forEach(function (variation) {
                var optionValues = variation.optionCombinations.map(function (item) {
                    return item.value;
                });
                if (optionValues.indexOf(item) > -1) {
                    optionValueAdded = true;
                }
            });

            return !optionValueAdded;
        };

        vm.save = function save() {
            var promise;

            if (!vm.isGiTagSaveAllowed()) {
                toastr.error(vm.getGiTagSaveBlockMessage() || 'GI Tag is not verified.');
                return;
            }

            // ng-upload will post null as text
            vm.product.taxClassId = vm.product.taxClassId === null ? '' : vm.product.taxClassId;
            vm.product.brandId = vm.product.brandId === null ? '' : vm.product.brandId;
            vm.product.oldPrice = vm.product.oldPrice === null ? '' : vm.product.oldPrice;
            vm.product.specialPrice = vm.product.specialPrice === null ? '' : vm.product.specialPrice;
            vm.product.specialPriceStart = vm.product.specialPriceStart === null ? '' : vm.product.specialPriceStart;
            vm.product.specialPriceEnd = vm.product.specialPriceEnd === null ? '' : vm.product.specialPriceEnd;
            vm.product.sku = vm.product.sku === null ? '' : vm.product.sku;
            vm.product.gtin = vm.product.gtin === null ? '' : vm.product.gtin;
            vm.product.metaTitle = vm.product.metaTitle === null ? '' : vm.product.metaTitle;
            vm.product.metaKeywords = vm.product.metaKeywords === null ? '' : vm.product.metaKeywords;
            vm.product.metaDescription = vm.product.metaDescription === null ? '' : vm.product.metaDescription;
            vm.product.variations.forEach(function (item) {
                item.oldPrice = item.oldPrice === null ? '' : item.oldPrice;
                item.sku = item.sku === null ? '' : item.sku;
                item.gtin = item.gtin === null ? '' : item.gtin;
            });

            vm.product.options.forEach(function (item) {
                item.values.forEach(function (val) {
                    val.display = val.display === null ? '' : val.display;
                });
            });

            if (vm.isEditMode) {
                promise = productService.editProduct(vm.product, vm.thumbnailImage, vm.productImages, vm.productDocuments);
            } else {
                promise = productService.createProduct(vm.product, vm.thumbnailImage, vm.productImages, vm.productDocuments);
            }

            promise.then(function (result) {
                    $state.go('product');
                })
                .catch(function (response) {
                    var error = response.data;
                    vm.validationErrors = [];
                    if (error && angular.isObject(error)) {
                        for (var key in error) {
                            vm.validationErrors.push(error[key][0]);
                        }
                    } else {
                        vm.validationErrors.push('Could not add product.');
                    }
                });
        };

        function getProduct() {
            productService.getProduct($stateParams.id).then(function (result) {
                var i, index, optionIds, attributeIds;
                vm.product = result.data;
                optionIds = vm.options.map(function (item) { return item.id; });
                for (i = 0; i < vm.product.options.length; i = i + 1) {
                    index = optionIds.indexOf(vm.product.options[i].id);
                    optionIds.splice(index, 1);
                    vm.options.splice(index, 1);
                }

                attributeIds = vm.attributes.map(function (item) { return item.id; });
                for (i = 0; i < vm.product.attributes.length; i = i + 1) {
                    index = attributeIds.indexOf(vm.product.attributes[i].id);
                    attributeIds.splice(index, 1);
                    vm.attributes.splice(index, 1);
                }
                scheduleGiTagValidation(0);

                if (vm.product.specialPriceStart) {
                    vm.product.specialPriceStart = new Date(vm.product.specialPriceStart);
                }
                if (vm.product.specialPriceEnd) {
                    vm.product.specialPriceEnd = new Date(vm.product.specialPriceEnd);
                }
            });
        }

        function getCategories() {
            categoryService.getCategories().then(function (result) {
                vm.categories = result.data;
            });
        }

        function getProductOptions() {
            productService.getProductOptions().then(function (result) {
                vm.options = result.data;
            });
        }

        function getProductTemplates() {
            productService.getProductTemplates().then(function (result) {
                vm.productTemplates = result.data;
            });
        }

        function getAttributes() {
            productService.getProductAttrs().then(function (result) {
                vm.attributes = result.data;
                scheduleGiTagValidation(0);
            });
        }

        function getBrands() {
            brandService.getBrands().then(function (result) {
                vm.brands = result.data;
            });
        }

        function getTaxClasses() {
            productService.getTaxClasses().then(function (result) {
                vm.taxClasses = result.data;
            });
        }

        function getDefaultTaxClass() {
            productService.getDefaultTaxClass().then(function (result) {
                if (result.data) {
                    vm.product.taxClassId = result.data.id;
                }
            });
        }

        function init() {
            if (vm.isEditMode) {
                getProduct();
            }
            else {
                getDefaultTaxClass();
            }
            getProductOptions();
            getProductTemplates();
            getAttributes();
            getCategories();
            getBrands();
            getTaxClasses();
            warmPhoneSyncConnection();
            scheduleGiTagValidation(0);
        }

        function isGiTagAttribute(attribute) {
            var name;

            if (!attribute || !attribute.name) {
                return false;
            }

            name = attribute.name.toLowerCase().replace(/[\s_\-]/g, '');
            return name === 'gitag'
                || name === 'gitagid'
                || name === 'giregistrationnumber'
                || name === 'geographicalindicationtag'
                || name.indexOf('gitag') > -1;
        }

        function getGiTagAttribute() {
            return (vm.product.attributes || []).find(isGiTagAttribute);
        }

        function getGiTagValue() {
            var attribute = getGiTagAttribute();
            return attribute && attribute.value ? attribute.value.trim() : '';
        }

        function normalizeGiTagValue(value) {
            return (value || '').toString().trim().toUpperCase();
        }

        function setGiTagValidation(status, message, value, response) {
            vm.giTagValidation.status = status;
            vm.giTagValidation.message = message;
            vm.giTagValidation.value = value || '';
            vm.giTagValidation.response = response || null;
        }

        function scheduleGiTagValidation(delay) {
            if (vm.giTagValidationTimer) {
                $timeout.cancel(vm.giTagValidationTimer);
            }

            vm.giTagValidationTimer = $timeout(validateGiTagAttribute, angular.isNumber(delay) ? delay : 450);
        }

        function validateGiTagAttribute() {
            var giTagAttribute = getGiTagAttribute();
            var value = getGiTagValue();
            var requestId;

            if (!giTagAttribute) {
                setGiTagValidation('missing', '', '');
                return;
            }

            if (!value) {
                setGiTagValidation('empty', '', '');
                return;
            }

            requestId = vm.giTagValidationRequestId + 1;
            vm.giTagValidationRequestId = requestId;
            setGiTagValidation('validating', 'Validating GI Tag...', value);

            productService.validateGiTag(value)
                .then(function (result) {
                    var data;

                    if (requestId !== vm.giTagValidationRequestId) {
                        return;
                    }

                    data = result.data || {};
                    if (data.isValid) {
                        setGiTagValidation('verified', data.message || 'Verified', value, data);
                    } else {
                        setGiTagValidation('notVerified', data.message || 'Not verified', value, data);
                    }
                })
                .catch(function () {
                    if (requestId !== vm.giTagValidationRequestId) {
                        return;
                    }

                    setGiTagValidation('notVerified', 'Not verified', value);
                });
        }

        function createSessionId() {
            if (window.crypto && window.crypto.randomUUID) {
                return window.crypto.randomUUID();
            }

            return Date.now() + '-' + Math.random().toString(16).slice(2);
        }

        function dataUrlToFile(dataUrl, fileName, contentType) {
            var parts = dataUrl.split(',');
            var byteString = atob(parts[1]);
            var arrayBuffer = new ArrayBuffer(byteString.length);
            var intArray = new Uint8Array(arrayBuffer);
            var i;

            for (i = 0; i < byteString.length; i = i + 1) {
                intArray[i] = byteString.charCodeAt(i);
            }

            return new File([arrayBuffer], fileName, { type: contentType });
        }

        function getAudioRecorderOptions() {
            var types = [
                'audio/webm;codecs=opus',
                'audio/webm',
                'audio/mp4',
                'audio/wav'
            ];
            var i;

            if (!window.MediaRecorder || !MediaRecorder.isTypeSupported) {
                return null;
            }

            for (i = 0; i < types.length; i = i + 1) {
                if (MediaRecorder.isTypeSupported(types[i])) {
                    return { mimeType: types[i] };
                }
            }

            return null;
        }

        function onVoiceRecordingStopped() {
            var contentType = (vm.voiceNote.mediaRecorder && vm.voiceNote.mediaRecorder.mimeType) || 'audio/webm';
            var extension = contentType.indexOf('mp4') > -1 ? 'm4a' : (contentType.indexOf('wav') > -1 ? 'wav' : 'webm');
            var blob = new Blob(vm.voiceNote.chunks, { type: contentType });
            var file = new File([blob], 'voice-note-' + Date.now() + '.' + extension, { type: contentType });

            stopVoiceStream();

            $timeout(function () {
                vm.voiceNote.isRecording = false;
                vm.processVoiceNote(file);
            });
        }

        function stopVoiceStream() {
            if (vm.voiceNote.stream) {
                vm.voiceNote.stream.getTracks().forEach(function (track) {
                    track.stop();
                });
            }

            vm.voiceNote.stream = null;
            vm.voiceNote.mediaRecorder = null;
        }

        function extractVoiceNoteText(data) {
            var parsedData;

            if (!data) {
                return '';
            }

            if (angular.isString(data)) {
                parsedData = tryParseJson(data);
                return parsedData ? extractVoiceNoteText(parsedData) : data.trim();
            }

            return findVoiceNoteText(data);
        }

        function tryParseJson(value) {
            try {
                return JSON.parse(value);
            } catch (e) {
                return null;
            }
        }

        function findVoiceNoteText(value) {
            var candidateKeys = [
                'englishText',
                'english_text',
                'translatedText',
                'translated_text',
                'translation',
                'translationText',
                'translation_text',
                'description',
                'generatedDescription',
                'generated_description',
                'outputText',
                'output_text',
                'transcript',
                'transcription',
                'transcribedText',
                'transcribed_text',
                'text',
                'content'
            ];
            var containerKeys = [
                'data',
                'result',
                'response',
                'output',
                'payload',
                'body'
            ];
            var i;
            var key;
            var found;

            if (!value) {
                return '';
            }

            if (angular.isString(value)) {
                return value.trim();
            }

            if (angular.isArray(value)) {
                for (i = 0; i < value.length; i = i + 1) {
                    found = findVoiceNoteText(value[i]);
                    if (found) {
                        return found;
                    }
                }

                return '';
            }

            if (!angular.isObject(value)) {
                return '';
            }

            for (i = 0; i < candidateKeys.length; i = i + 1) {
                key = candidateKeys[i];
                if (value[key]) {
                    found = findVoiceNoteText(value[key]);
                    if (found) {
                        return found;
                    }
                }
            }

            for (i = 0; i < containerKeys.length; i = i + 1) {
                key = containerKeys[i];
                if (value[key]) {
                    found = findVoiceNoteText(value[key]);
                    if (found) {
                        return found;
                    }
                }
            }

            return '';
        }

        function appendDescriptionText(text) {
            var html = '<p>' + escapeHtml(text).replace(/\r?\n/g, '<br>') + '</p>';

            vm.product.description = (vm.product.description || '') + html;

            if (vm.descEditor) {
                $(vm.descEditor).summernote('pasteHTML', html);
            }
        }

        function escapeHtml(value) {
            return (value || '').toString()
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function getVoiceNoteError(error) {
            if (error && error.data) {
                if (angular.isString(error.data)) {
                    return error.data;
                }

                return error.data.error || error.data.message || 'Failed to convert the voice note.';
            }

            return 'Failed to convert the voice note.';
        }

        function getFileNameFromUrl(url) {
            var cleanUrl = (url || '').split('?')[0];
            var fileName = cleanUrl.substring(cleanUrl.lastIndexOf('/') + 1);

            return fileName || ('product-image-' + Date.now() + '.jpg');
        }

        function warmPhoneSyncConnection() {
            if (window.signalR) {
                ensurePhoneSyncConnection().catch(function () {
                    vm.phoneSyncConnectionPromise = null;
                });
            }
        }

        function ensurePhoneSyncConnection() {
            if (vm.phoneSyncConnectionPromise) {
                return vm.phoneSyncConnectionPromise;
            }

            vm.phoneSyncConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/phone-sync')
                .build();

            vm.phoneSyncConnection.on('DeviceJoined', function (device) {
                $timeout(function () {
                    if (device && device.sessionId === vm.phoneSync.sessionId && device.deviceType === 'phone') {
                        vm.phoneSync.status = 'Phone connected';
                    }
                });
            });

            vm.phoneSyncConnection.on('FileReceived', function (file) {
                $timeout(function () {
                    if (!file || file.sessionId !== vm.phoneSync.sessionId) {
                        return;
                    }

                    if (!(file.dataUrl || file.DataUrl)) {
                        vm.phoneSync.status = 'Received an empty file payload';
                        return;
                    }

                    var uploadedFile = dataUrlToFile(
                        file.dataUrl || file.DataUrl,
                        file.fileName || file.FileName || ('phone-file-' + Date.now()),
                        file.contentType || file.ContentType || 'application/octet-stream'
                    );

                    uploadedFile.previewUrl = URL.createObjectURL(uploadedFile);

                    vm.productDocuments = vm.productDocuments || [];
                    vm.productDocuments.push(uploadedFile);
                    vm.phoneSync.receivedFiles.push(uploadedFile);
                    vm.phoneSync.latestFile = uploadedFile;
                    vm.phoneSync.status = 'File received and added to Product Documents';
                    toastr.success('File received from phone: ' + uploadedFile.name);

                    $timeout(function () {
                        $('#phoneSyncModal').modal('hide');
                    }, 650);
                });
            });

            vm.phoneSyncConnection.onclose(function () {
                $timeout(function () {
                    vm.phoneSyncConnectionPromise = null;
                    if (vm.phoneSync.sessionId) {
                        vm.phoneSync.status = 'Disconnected';
                    }
                });
            });

            vm.phoneSyncConnectionPromise = vm.phoneSyncConnection.start()
                .catch(function (error) {
                    vm.phoneSyncConnectionPromise = null;
                    throw error;
                });

            return vm.phoneSyncConnectionPromise;
        }

        function stopPhoneSyncConnection() {
            if (vm.giTagValidationTimer) {
                $timeout.cancel(vm.giTagValidationTimer);
                vm.giTagValidationTimer = null;
            }

            stopVoiceStream();

            if (vm.phoneSyncConnection) {
                vm.phoneSyncConnection.stop();
                vm.phoneSyncConnection = null;
                vm.phoneSyncConnectionPromise = null;
            }
        }

        $scope.$on('$destroy', stopPhoneSyncConnection);

        function getParentCategoryIds(categoryId) {
            if (!categoryId) {
                return [];
            }
            var category = vm.categories.find(function (item) { return item.id === categoryId; });

            return category ? [category.id].concat(getParentCategoryIds(category.parentId)) : [];
        }

        function getChildCategoryIds(categoryId) {
            if (!categoryId) {
                return [];
            }

            var category = vm.categories.find(function (item) { return item.id === categoryId; });
            var result = category ? [category.id] : [];

            var childCategories = vm.categories.filter(function (item) { return item.parentId === categoryId; });
            childCategories.forEach(function (item) {
                result = result.concat(getChildCategoryIds(item.id));
            });

            return result;
        }

        // Helper function to convert base64 data URI to File object
        function base64ToFile(dataURI, fileName, mimeType) {
            var arr = dataURI.split(',');
            var bstr = atob(arr[1]);
            var n = bstr.length;
            var u8arr = new Uint8Array(n);
            
            while (n--) {
                u8arr[n] = bstr.charCodeAt(n);
            }
            
            var file = new File([u8arr], fileName, { type: mimeType });
            
            // Add properties that ngf expects
            file.$ngfName = fileName;
            file.$ngfSize = u8arr.length;
            file.$ngfType = mimeType;
            
            return file;
        }

        init();
    }
})(jQuery);
