// Manager Dashboard JavaScript
let categoryChart = null, statusChart = null;
let currentProducts = [];
let currentSellProduct = null;

function getAntiForgeryToken() {
    let token = $('input[name="__RequestVerificationToken"]').val();
    if (!token) token = $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();
    return token || '';
}

// API endpoints
const API = {
    getCurrentStock: '/Stock/GetCurrentStock',
    removeStock: '/Stock/RemoveStock',
    getDashboardStats: '/Manager/GetDashboardStats',
    logout: '/Account/Logout'
};

function fetchDashboardData() {
    $.ajax({
        url: API.getDashboardStats,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                $('#totalProducts').text(response.totalProducts || 0);
                $('#lowStockCount').text(response.lowStockCount || 0);
                $('#totalValue').text('$' + (response.totalValue || 0).toLocaleString());
                $('#categoryCount').text(response.categoryCount || 0);
            }
        },
        error: function () {
            fetchProducts(); // Fallback to products data
        }
    });

    fetchProducts();
}

function fetchProducts() {
    $.ajax({
        url: API.getCurrentStock,
        type: 'GET',
        success: function (response) {
            if (response && response.products && response.products.length) {
                currentProducts = response.products.map(p => ({
                    id: p.productId,
                    name: p.productName || 'Unknown',
                    category: p.categoryName || 'General',
                    stock: p.quantity || 0,
                    price: p.salePrice || p.purchasePrice || 0
                }));
            } else if (Array.isArray(response)) {
                currentProducts = response.map(p => ({
                    id: p.productId || p.id,
                    name: p.productName || p.name,
                    category: p.categoryName || p.category || 'General',
                    stock: p.quantity || p.stock || 0,
                    price: p.salePrice || p.price || 0
                }));
            }
            updateDashboard();
        },
        error: function () {
            currentProducts = [];
            updateDashboard();
            showToast('Failed to load products', 'error');
        }
    });
}

function getStatus(stock) {
    if (stock <= 0) return "Out of Stock";
    if (stock < 10) return "Low Stock";
    return "In Stock";
}

function getStatusClass(status) {
    if (status === "In Stock") return "in-stock";
    if (status === "Low Stock") return "low-stock";
    return "out-stock";
}

function updateStats() {
    const total = currentProducts.length;
    const lowStock = currentProducts.filter(p => p.stock > 0 && p.stock < 10).length;
    const totalVal = currentProducts.reduce((sum, p) => sum + (p.stock * (p.price || 0)), 0);
    const uniqueCategories = [...new Set(currentProducts.map(p => p.category || 'General'))];

    $('#totalProducts').text(total);
    $('#lowStockCount').text(lowStock);
    $('#totalValue').text('$' + totalVal.toLocaleString());
    $('#categoryCount').text(uniqueCategories.length);
}

function renderTable() {
    const tbody = $('#tableBody');
    tbody.empty();

    if (!currentProducts.length) {
        tbody.html('<tr><td colspan="6" style="text-align:center;">No products found.</td></tr>');
        return;
    }

    currentProducts.forEach(product => {
        const status = getStatus(product.stock);
        const statusClass = getStatusClass(status);
        const row = `
            <tr>
                <td><i class="fas fa-box" style="color:#8b5cf6; margin-right:8px;"></i>${escapeHtml(product.name)}</td>
                <td><span class="category-badge">${escapeHtml(product.category)}</span></td>
                <td><strong>${product.stock}</strong> units</td>
                <td><strong>$${(product.price || 0).toFixed(2)}</strong></td>
                <td><span class="status ${statusClass}">${status}</span></td>
                <td>
                    <button class="action-btn" onclick="openSellModal(${product.id})"><i class="fas fa-minus-circle"></i> Sell</button>
                    <button class="action-btn" onclick="window.location.href='/Stock/StockIn'"><i class="fas fa-plus"></i> Restock</button>
                    <button class="action-btn" onclick="editProduct(${product.id})"><i class="fas fa-edit"></i> Edit</button>
                </td>
            </tr>
        `;
        tbody.append(row);
    });
}

function editProduct(productId) {
    window.location.href = `/Product/Edit/${productId}`;
}

window.openSellModal = function (productId) {
    const product = currentProducts.find(p => p.id === productId);
    if (!product) {
        showToast('Product not found', 'error');
        return;
    }
    if (product.stock <= 0) {
        showToast('No stock available to sell', 'error');
        return;
    }

    currentSellProduct = product;
    $('#modalProductName').val(product.name);
    $('#modalProductId').val(product.id);
    $('#modalAvailableStock').val(product.stock + ' units');
    $('#modalQuantity').attr('max', product.stock).val(1);
    $('#modalReason').val('Sale');
    $('#modalRemarks').val('');
    $('#sellModal').fadeIn(300);
};

function closeModal() {
    $('#sellModal').fadeOut(300);
    currentSellProduct = null;
}

function confirmSell() {
    if (!currentSellProduct) return;

    const quantity = parseInt($('#modalQuantity').val(), 10);
    const reason = $('#modalReason').val();
    const remarks = $('#modalRemarks').val() || `Sold from dashboard on ${new Date().toLocaleString()}`;

    if (isNaN(quantity) || quantity <= 0) {
        showToast('Please enter a valid quantity', 'error');
        return;
    }
    if (quantity > currentSellProduct.stock) {
        showToast(`Only ${currentSellProduct.stock} units available`, 'error');
        return;
    }

    const confirmBtn = $('#confirmSellBtn');
    confirmBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');

    const token = getAntiForgeryToken();
    const formData = new URLSearchParams();
    formData.append('productId', currentSellProduct.id);
    formData.append('quantityRemoved', quantity);
    formData.append('reason', reason);
    formData.append('remarks', remarks);
    if (token) formData.append('__RequestVerificationToken', token);

    $.ajax({
        url: API.removeStock,
        type: 'POST',
        data: formData.toString(),
        contentType: 'application/x-www-form-urlencoded',
        success: function (response) {
            if (response.success) {
                const productIndex = currentProducts.findIndex(p => p.id === currentSellProduct.id);
                if (productIndex !== -1) {
                    currentProducts[productIndex].stock -= quantity;
                    if (currentProducts[productIndex].stock < 0) currentProducts[productIndex].stock = 0;
                }
                showToast(`Sold ${quantity} ${currentSellProduct.name} successfully!`, 'success');
                closeModal();
                updateDashboard();
            } else {
                showToast(response.message || 'Failed to sell stock', 'error');
            }
            confirmBtn.prop('disabled', false).html('<i class="fas fa-check"></i> Confirm Sale');
        },
        error: function (xhr) {
            let errorMsg = 'Failed to sell stock.';
            try {
                const response = JSON.parse(xhr.responseText);
                if (response.message) errorMsg = response.message;
            } catch (e) {
                if (xhr.status === 400) errorMsg = 'Bad request. Please refresh and try again.';
            }
            showToast(errorMsg, 'error');
            confirmBtn.prop('disabled', false).html('<i class="fas fa-check"></i> Confirm Sale');
        }
    });
}

function updateCharts() {
    const categoryMap = new Map();
    currentProducts.forEach(p => {
        const cat = p.category || 'General';
        categoryMap.set(cat, (categoryMap.get(cat) || 0) + (p.stock || 0));
    });

    const categories = [...categoryMap.keys()];
    const stocks = [...categoryMap.values()];
    const inStock = currentProducts.filter(p => getStatus(p.stock) === "In Stock").length;
    const lowStock = currentProducts.filter(p => getStatus(p.stock) === "Low Stock").length;
    const outStock = currentProducts.filter(p => getStatus(p.stock) === "Out of Stock").length;

    if (document.getElementById('categoryChart')) {
        if (categoryChart) categoryChart.destroy();
        categoryChart = new Chart(document.getElementById('categoryChart'), {
            type: 'bar',
            data: {
                labels: categories.length ? categories : ['No Data'],
                datasets: [{
                    label: 'Stock Units',
                    data: categories.length ? stocks : [0],
                    backgroundColor: '#8b5cf6',
                    borderRadius: 8
                }]
            },
            options: { responsive: true, maintainAspectRatio: true }
        });
    }

    if (document.getElementById('statusChart')) {
        if (statusChart) statusChart.destroy();
        statusChart = new Chart(document.getElementById('statusChart'), {
            type: 'pie',
            data: {
                labels: ['In Stock', 'Low Stock', 'Out of Stock'],
                datasets: [{
                    data: [inStock, lowStock, outStock],
                    backgroundColor: ['#2ecc71', '#f39c12', '#e74c3c']
                }]
            },
            options: { responsive: true, maintainAspectRatio: true }
        });
    }
}

function updateDashboard() {
    updateStats();
    renderTable();
    updateCharts();
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

$(document).ready(function () {
    fetchDashboardData();

    $('#menuToggle').on('click', function () {
        $('#sidebar').toggleClass('open');
    });

    $('#closeSellModalBtn, #cancelSellBtn').on('click', closeModal);
    $('#confirmSellBtn').on('click', confirmSell);

    $(window).on('click', function (e) {
        if ($(e.target).is('#sellModal')) closeModal();
    });

    $('#modalQuantity').on('keypress', function (e) {
        if (e.which === 13) confirmSell();
    });
});