/*global angular*/
(function () {
    angular
        .module('simplAdmin.vendors')
        .controller('VendorCreditDashboardCtrl', ['$q', '$stateParams', 'vendorService', 'translateService', VendorCreditDashboardCtrl]);

    function VendorCreditDashboardCtrl($q, $stateParams, vendorService, translateService) {
        var vm = this;

        vm.vendorId = $stateParams.id;
        vm.translate = translateService;
        vm.vendor = {};
        vm.orders = [];
        vm.customers = [];
        vm.settlements = [];
        vm.summaryCards = [];
        vm.settlement = {};
        vm.settlementForm = {};
        vm.selectedCustomerId = null;
        vm.currentPage = 1;
        vm.pageSize = 10;

        vm.refresh = refresh;
        vm.selectCustomer = selectCustomer;
        vm.openSettlementModal = openSettlementModal;
        vm.closeSettlementModal = closeSettlementModal;
        vm.saveSettlement = saveSettlement;
        vm.openSettlementHistory = openSettlementHistory;
        vm.closeSettlementHistory = closeSettlementHistory;
        vm.remindSettlement = remindSettlement;
        vm.getPagedOrders = getPagedOrders;
        vm.totalPages = totalPages;
        vm.pageStart = pageStart;
        vm.pageEnd = pageEnd;
        vm.nextPage = nextPage;
        vm.previousPage = previousPage;

        refresh();

        function refresh() {
            vm.isLoading = true;
            vm.error = null;

            $q.all({
                vendor: vendorService.getVendor(vm.vendorId),
                customers: vendorService.getVendorCustomers(vm.vendorId)
            }).then(function (result) {
                vm.vendor = result.vendor.data || {};
                vm.customers = (result.customers.data && result.customers.data.customers) || [];
                vm.selectedCustomerId = vm.selectedCustomerId || getDefaultCustomerId(vm.customers);

                if (!vm.selectedCustomerId) {
                    resetDashboard();
                    vm.isLoading = false;
                    return null;
                }

                return loadSelectedCustomerDashboard();
            }).then(function () {
                vm.isLoading = false;
            }).catch(function (response) {
                vm.isLoading = false;
                vm.error = (response && response.data && (response.data.error || response.data.message)) || 'Could not load vendor credit dashboard.';
            });
        }

        function selectCustomer() {
            vm.isLoading = true;
            vm.error = null;
            loadSelectedCustomerDashboard().then(function () {
                vm.isLoading = false;
            }).catch(function (response) {
                vm.isLoading = false;
                vm.error = (response && response.data && (response.data.error || response.data.message)) || 'Could not load customer credit details.';
            });
        }

        function loadSelectedCustomerDashboard() {
            if (!vm.selectedCustomerId) {
                resetDashboard();
                return $q.when();
            }

            return $q.all({
                orders: vendorService.getCustomerCreditOrders(vm.vendorId, vm.selectedCustomerId),
                settlement: vendorService.getCustomerSettlementSummary(vm.vendorId, vm.selectedCustomerId)
            }).then(function (result) {
                var orderData = result.orders.data || {};
                var settlementData = result.settlement.data || {};
                var settlementSummary = settlementData.getSettlementSummary || {};

                vm.selectedCustomer = findCustomer(vm.selectedCustomerId);
                vm.orders = (orderData.orders || []).map(mapOrderRow);
                vm.settlements = settlementSummary.settlementHistory || [];
                buildDashboard(orderData, settlementSummary);
            });
        }

        function mapOrderRow(order) {
            return {
                orderId: order.orderId,
                customerId: vm.selectedCustomerId,
                customerName: vm.selectedCustomer ? vm.selectedCustomer.customerName : 'N/A',
                itemName: order.itemName || 'N/A',
                itemQuantity: order.itemQuantity || 0,
                orderedDate: order.orderedDate,
                amount: toNumber(order.orderAmount),
                status: order.orderStatus || 'New',
                paymentMethod: order.paymentMethod
            };
        }

        function resetDashboard() {
            vm.orders = [];
            vm.settlements = [];
            vm.selectedCustomer = null;
            buildDashboard({}, {});
        }

        function buildDashboard(orderData, settlementSummary) {
            vm.orders.sort(function (left, right) {
                return new Date(right.orderedDate || 0) - new Date(left.orderedDate || 0);
            });

            var statusCounts = getStatusCounts(vm.orders);
            var revenue = sum(vm.orders, function (order) { return order.amount; });
            var orderTotal = toNumber(settlementSummary.totalAmount) || toNumber(orderData.totalCreditOrderAmount) || revenue;
            var remaining = toNumber(settlementSummary.remainingAmount) || toNumber(orderData.remainingBalance);
            var collected = Math.max(0, orderTotal - remaining);
            var settlementEntries = vm.settlements.length;

            vm.summaryCards = [
                { label: 'Total Orders', value: vm.orders.length, icon: 'fa fa-cube', tone: 'neutral' },
                { label: 'Revenue', value: compactMoney(revenue), icon: 'fa fa-line-chart', tone: 'success' },
                { label: 'Delivered', value: statusCounts.delivered, icon: 'fa fa-check-circle-o', tone: 'success' },
                { label: 'Shipped', value: statusCounts.shipped, icon: 'fa fa-truck', tone: 'warning' },
                { label: 'Pending', value: statusCounts.pending, icon: 'fa fa-clock-o', tone: 'warning' },
                { label: 'Cancelled', value: statusCounts.cancelled, icon: 'fa fa-times-circle-o', tone: 'danger' }
            ];

            vm.settlement = {
                status: getSettlementStatus(collected, remaining),
                entries: settlementEntries,
                orderTotal: orderTotal,
                orderTotalString: money(orderTotal),
                collected: collected,
                collectedString: money(collected),
                remaining: remaining,
                remainingString: money(remaining)
            };

            vm.currentPage = 1;
        }

        function openSettlementModal() {
            vm.settlementError = null;
            vm.settlementForm = {
                method: 'Cash',
                amount: vm.settlement.remaining || 0,
                date: new Date(),
                reminderDate: new Date(),
                transactionId: '',
                note: ''
            };
            vm.showSettlementModal = true;
        }

        function closeSettlementModal() {
            vm.showSettlementModal = false;
        }

        function saveSettlement() {
            if (!vm.selectedCustomerId || !vm.settlementForm.amount || vm.settlementForm.amount <= 0) {
                vm.settlementError = 'Enter a valid settlement amount.';
                return;
            }

            vm.isSavingSettlement = true;
            vm.settlementError = null;

            var amount = toNumber(vm.settlementForm.amount);
            var payload = {
                vendorId: toNumber(vm.vendorId),
                customerId: toNumber(vm.selectedCustomerId),
                amountPaid: amount,
                paymentMethod: [{
                    cash: vm.settlementForm.method === 'Cash' ? amount : 0,
                    online: vm.settlementForm.method === 'UPI' || vm.settlementForm.method === 'Online' ? amount : 0,
                    cheque: vm.settlementForm.method === 'Cheque' ? amount : 0,
                    transactionId: vm.settlementForm.transactionId || ''
                }],
                date: normalizeDate(vm.settlementForm.date),
                settlementReminderDate: normalizeDate(vm.settlementForm.reminderDate),
                note: vm.settlementForm.note || ''
            };

            vendorService.addCustomerSettlement(vm.vendorId, vm.selectedCustomerId, payload)
                .then(function () {
                    return vendorService.updateCustomerBalance(vm.vendorId, {
                        customerId: toNumber(vm.selectedCustomerId),
                        currentBalance: Math.max(0, vm.settlement.remaining - amount)
                    }).catch(function () { return null; });
                })
                .then(function () {
                    vm.showSettlementModal = false;
                    toastr.success('Settlement recorded successfully');
                    return loadSelectedCustomerDashboard();
                })
                .catch(function (response) {
                    vm.settlementError = (response && response.data && (response.data.error || response.data.message)) || 'Failed to record settlement.';
                })
                .finally(function () {
                    vm.isSavingSettlement = false;
                });
        }

        function openSettlementHistory() {
            vm.showSettlementHistory = true;
        }

        function closeSettlementHistory() {
            vm.showSettlementHistory = false;
        }

        function remindSettlement() {
            toastr.info('Use the vendor notification app to send settlement reminders.');
        }

        function getDefaultCustomerId(customers) {
            var customer = (customers || []).find(function (item) {
                return item.customerId || item.userId;
            });

            return customer ? (customer.customerId || customer.userId) : null;
        }

        function findCustomer(customerId) {
            return (vm.customers || []).find(function (customer) {
                return toNumber(customer.customerId || customer.userId) === toNumber(customerId);
            });
        }

        function getStatusCounts(orders) {
            var counts = {
                delivered: 0,
                shipped: 0,
                pending: 0,
                cancelled: 0
            };

            orders.forEach(function (order) {
                counts[getStatusBucket(order.status)] += 1;
            });

            return counts;
        }

        function getStatusBucket(status) {
            var normalized = (status || '').toString().toLowerCase().replace(/\s+/g, '');

            if (normalized === 'complete' || normalized === 'closed' || normalized === 'delivered') {
                return 'delivered';
            }

            if (normalized === 'shipping' || normalized === 'shipped') {
                return 'shipped';
            }

            if (normalized === 'canceled' || normalized === 'cancelled') {
                return 'cancelled';
            }

            return 'pending';
        }

        function getSettlementStatus(collected, remaining) {
            if (remaining <= 0 && collected > 0) {
                return 'Settled';
            }

            if (collected > 0) {
                return 'Partially Settled';
            }

            return 'Pending';
        }

        function getPagedOrders() {
            var start = (vm.currentPage - 1) * vm.pageSize;
            return vm.orders.slice(start, start + vm.pageSize);
        }

        function totalPages() {
            return Math.max(1, Math.ceil(vm.orders.length / vm.pageSize));
        }

        function pageStart() {
            return vm.orders.length === 0 ? 0 : ((vm.currentPage - 1) * vm.pageSize) + 1;
        }

        function pageEnd() {
            return Math.min(vm.currentPage * vm.pageSize, vm.orders.length);
        }

        function nextPage() {
            if (vm.currentPage < totalPages()) {
                vm.currentPage += 1;
            }
        }

        function previousPage() {
            if (vm.currentPage > 1) {
                vm.currentPage -= 1;
            }
        }

        function sum(items, selector) {
            return items.reduce(function (total, item) {
                return total + toNumber(selector(item));
            }, 0);
        }

        function compactMoney(amount) {
            if (Math.abs(amount) >= 1000) {
                return '₹' + trimNumber(amount / 1000) + 'K';
            }

            return money(amount);
        }

        function money(amount) {
            return '₹' + trimNumber(amount);
        }

        function trimNumber(amount) {
            var rounded = Math.round(toNumber(amount) * 10) / 10;
            return rounded.toLocaleString('en-IN', { maximumFractionDigits: 1 });
        }

        function normalizeDate(date) {
            if (!date) {
                return new Date().toISOString();
            }

            return new Date(date).toISOString();
        }

        function toNumber(value) {
            var parsed = parseFloat(value);
            return isNaN(parsed) ? 0 : parsed;
        }
    }
})();
