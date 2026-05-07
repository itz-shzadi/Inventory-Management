// Staff Dashboard JavaScript
let currentTransactions = [];
let lowStockItems = [];

function getAntiForgeryToken() {
    let token = $('input[name="__RequestVerificationToken"]').val();
    if (!token) token = $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();
    return token || '';
}

// API endpoints
const API = {
    getTodayStats: '/Staff/GetTodayStats',
    getMyTransactions: '/Staff/GetMyTransactions',
    getLowStockItems: '/Stock/GetLowStockItems',
    logout: '/Account/Logout'
};

function fetchDashboardData() {
    // Fetch today's stats
    $.ajax({
        url: API.getTodayStats,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                $('#todayStockIn').text(response.todayStockIn || 0);
                $('#todayStockOut').text(response.todayStockOut || 0);
            }
        },
        error: function () {
            $('#todayStockIn').text('0');
            $('#todayStockOut').text('0');
        }
    });

    // Fetch transactions
    fetchTransactions();

    // Fetch low stock items
    fetchLowStockItems();
}

function fetchTransactions() {
    $.ajax({
        url: API.getMyTransactions,
        type: 'GET',
        success: function (response) {
            if (response.success && response.transactions) {
                currentTransactions = response.transactions;
            } else if (Array.isArray(response)) {
                currentTransactions = response;
            } else {
                currentTransactions = [];
            }
            renderTransactions();
        },
        error: function () {
            currentTransactions = [];
            renderTransactions();
            showToast('Failed to load transactions', 'error');
        }
    });
}

function fetchLowStockItems() {
    $.ajax({
        url: API.getLowStockItems,
        type: 'GET',
        success: function (response) {
            if (response.success && response.items) {
                lowStockItems = response.items;
            } else if (Array.isArray(response)) {
                lowStockItems = response;
            } else {
                lowStockItems = [];
            }
            renderLowStockItems();
        },
        error: function () {
            lowStockItems = [];
            renderLowStockItems();
        }
    });
}

function renderTransactions() {
    const tbody = $('#transactionsBody');
    tbody.empty();

    if (!currentTransactions.length) {
        tbody.html('<tr><td colspan="5" style="text-align:center;">No transactions found.</td></tr>');
        return;
    }

    currentTransactions.forEach(transaction => {
        const row = `
            <tr>
                <td>${formatDate(transaction.dateTime || transaction.createdAt)}</td>
                <td>${escapeHtml(transaction.productName || 'Unknown')}</td>
                <td>
                    <span class="transaction-type ${transaction.type === 'Stock In' ? 'type-in' : 'type-out'}">
                        ${transaction.type === 'Stock In' ? '<i class="fas fa-arrow-down"></i> Stock In' : '<i class="fas fa-arrow-up"></i> Stock Out'}
                    </span>
                </td>
                <td><strong>${transaction.quantity || 0}</strong></td>
                <td>${escapeHtml(transaction.reference || '-')}</td>
            </tr>
        `;
        tbody.append(row);
    });
}

function renderLowStockItems() {
    const tbody = $('#lowStockBody');
    tbody.empty();

    if (!lowStockItems.length) {
        tbody.html('<tr><td colspan="4" style="text-align:center;">No low stock items found.</td></tr>');
        return;
    }

    lowStockItems.forEach(item => {
        const status = getStockStatus(item.currentStock, item.minStockLevel);
        const row = `
            <tr>
                <td><i class="fas fa-box" style="color:#f59e0b;"></i> ${escapeHtml(item.productName)}</td>
                <td><strong class="${item.currentStock <= 0 ? 'text-danger' : 'text-warning'}">${item.currentStock}</strong> units</td>
                <td>${item.minStockLevel || 10} units</td>
                <td><span class="status-badge ${status.class}">${status.text}</span></td>
            </tr>
        `;
        tbody.append(row);
    });
}

function getStockStatus(current, min) {
    if (current <= 0) {
        return { text: 'Out of Stock', class: 'out-stock' };
    } else if (current < (min || 10)) {
        return { text: 'Critical Stock', class: 'critical-stock' };
    } else if (current < (min || 10) * 2) {
        return { text: 'Low Stock', class: 'low-stock' };
    }
    return { text: 'Adequate', class: 'adequate' };
}

function formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleString();
}

function escapeHtml(str) {
    if (!str) return '';
    return $('<div>').text(str).html();
}

function showToast(message, type = 'success') {
    let toast = $('#toast');
    toast.removeClass('success error warning').addClass(type);
    toast.text(message).fadeIn(300);
    setTimeout(() => toast.fadeOut(300), 3000);
}

window.logout = function () {
    let token = getAntiForgeryToken();
    const formData = new URLSearchParams();
    if (token) formData.append('__RequestVerificationToken', token);

    $.ajax({
        url: API.logout,
        type: 'POST',
        data: formData.toString(),
        contentType: 'application/x-www-form-urlencoded',
        success: function () {
            window.location.href = '/Account/Login';
        },
        error: function () {
            window.location.href = '/Account/Login';
        }
    });
};

// Auto-refresh every 30 seconds for staff dashboard
let refreshInterval;

function startAutoRefresh() {
    if (refreshInterval) clearInterval(refreshInterval);
    refreshInterval = setInterval(() => {
        fetchTransactions();
        fetchLowStockItems();
    }, 30000);
}

function stopAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}

$(document).ready(function () {
    fetchDashboardData();
    startAutoRefresh();

    $('#menuToggle').on('click', function () {
        $('#sidebar').toggleClass('open');
    });

    // Stop auto-refresh on page unload
    $(window).on('beforeunload', function () {
        stopAutoRefresh();
    });
});