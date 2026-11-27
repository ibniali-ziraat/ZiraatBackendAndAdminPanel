/*global angular*/
(function () {
    angular
        .module('simplAdmin.catalog')
        .factory('productService', ['$http', 'Upload', productService]);

    function productService($http, Upload) {
        var service = {
            quickSearchProducts: quickSearchProducts,
            getProducts: getProducts,
            createProduct: createProduct,
            editProduct: editProduct,
            getProductAttrs: getProductAttrs,
            getProductTemplates: getProductTemplates,
            getProductTemplate: getProductTemplate,
            getProductOptions: getProductOptions,
            getProduct: getProduct,
            changeStatus: changeStatus,
            deleteProduct: deleteProduct,
            getTaxClasses: getTaxClasses,
            getDefaultTaxClass: getDefaultTaxClass,
            getProductTranslation: getProductTranslation,
            editProductTranslation: editProductTranslation,
            generateThumbnail: generateThumbnail,
            enhanceImage: enhanceImage,
            processVoiceNote: processVoiceNote,
            validateGiTag: validateGiTag
        };
        return service;

        function quickSearchProducts(name) {
            return $http.get('api/products/quick-search?name=' + name);
        }

        function getProduct(id) {
            return $http.get('api/products/' + id);
        }

        function getProductAttrs() {
            return $http.get('api/product-attributes');
        }

        function getProductTemplates() {
            return $http.get('api/product-templates');
        }

        function getProductTemplate(id) {
            return $http.get('api/product-templates/' + id);
        }

        function getProductOptions() {
            return $http.get('api/product-options');
        }

        function getProducts(params) {
            return $http.post('api/products/grid', params);
        }

        function createProduct(product, thumbnailImage, productImages, productDocuments) {
            return Upload.upload({
                url: 'api/products',
                data: {
                    product: product,
                    thumbnailImage: thumbnailImage,
                    productImages: productImages,
                    productDocuments: productDocuments
                }
            });
        }

        function editProduct(product, thumbnailImage, productImages, productDocuments) {
            return Upload.upload({
                url: 'api/products/' + product.id,
                method: 'PUT',
                data: {
                    product: product,
                    thumbnailImage: thumbnailImage,
                    productImages: productImages,
                    productDocuments: productDocuments
                }
            });
        }

        function changeStatus(product) {
            return $http.post('api/products/change-status/' + product.id, null);
        }

        function deleteProduct(product) {
            return $http.delete('api/products/' + product.id, null);
        }

        function getTaxClasses() {
            return $http.get('api/tax-classes');
        }

        function getDefaultTaxClass() {
            return $http.get('api/tax-classes/default');
        }

        function getProductTranslation(id, culture) {
            return $http.get('api/product-translations/' + id + '?culture=' + culture);
        }

        function editProductTranslation(id, culture, model) {
            return $http.put('api/product-translations/' + id + '?culture=' + culture, model);
        }

        function generateThumbnail(image) {
            return Upload.upload({
                url: 'api/products/generate-thumbnail',
                data: {
                    image: image
                }
            });
        }

        function enhanceImage(image) {
            return Upload.upload({
                url: 'api/products/enhance-image',
                data: {
                    image: image
                }
            });
        }

        function processVoiceNote(audioFile) {
            return Upload.upload({
                url: 'api/products/process-voice-note',
                data: {
                    file: audioFile
                }
            });
        }

        function validateGiTag(tag) {
            return $http.get('api/gi-tags/validate/' + encodeURIComponent(tag));
        }
    }
})();
