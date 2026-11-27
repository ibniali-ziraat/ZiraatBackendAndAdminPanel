/*global angular confirm*/
(function () {
    angular
        .module('simplAdmin.catalog')
        .directive('productOptionDisplayDirective', ['$document', productOptionDisplayDirective]);

    function productOptionDisplayDirective($document) {
        var directive = {
            restrict: 'E',
            templateUrl: '_content/ECommerce.Module.Catalog/admin/product/product-option-display-directive.html',
            scope: {
                option: '=option',
                modelId: '@modelId',
                title: '@title'
            },
            controller: ProductOptionDisplayCtrl,
            controllerAs: 'vm',
            bindToController: true,
            link: function link(scope, element) {
                var modal = element.children().eq(0);
                $document.find('body').append(modal);

                scope.$on('$destroy', function () {
                    modal.remove();
                });
            }
        };

        return directive;
    }

    /* @ngInject */
    function ProductOptionDisplayCtrl() {
        var vm = this;
    }
})();
