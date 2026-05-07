// Admin Dashboard JavaScript
let categoryChart = null, statusChart = null;
let currentProducts = [];

function getAntiForgeryToken() {
    let token = $('input[name="__RequestVerificationToken"]').val();
    if (!token) token = $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();
    return token || '';
}

// API endpoints
const API = {
    getDashboardStats: '/Admin/GetDashboardStats',
    getCurrentStock: '/Stock/GetCurrentStock',
    getAllUsers: '/User/GetAllUsers',
    logout: '/Account/Logout'
};

// Fetch all data for admin dashboard
function fetchDashboardData() {
    $.ajax({
        url: API.getDashboardStats,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                $('#totalUsers').text(response.totalUsers || 0);
                $('#totalProducts').text(response.totalProducts || 0);
                $('#totalSuppliers').text(response.totalSuppliers || 0);
                $('#categoryCount').text(response.categoryCount || 0);
            }
        },
        error: function () {
            $('#totalUsers').text('0');
            $('#totalProducts').text('0');
            $('#totalSuppliers').text('0');
            $('#categoryCount').text('0');
        }
    });

    // Fetch products for table and charts
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
            showToast('error', 'Failed to load products', 'error');
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
                    <button class="action-btn" onclick="editProduct(${product.id})"><i class="fas fa-edit"></i> Edit</button>
                    <button class="action-btn btn-danger" onclick="deleteProduct(${product.id})"><i class="fas fa-trash"></i> Delete</button>
                </td>
            </tr>
        `;
        tbody.append(row);
    });
}

function editProduct(productId) {
    window.location.href = `/Product/Edit/${productId}`;
}

function deleteProduct(productId) {
    if (confirm('Are you sure you want to delete this product?')) {
        $.ajax({
            url: `/Product/Delete/${productId}`,
            type: 'POST',
            data: { __RequestVerificationToken: getAntiForgeryToken() },
            success: function (response) {
                if (response.success) {
                    showToast('Product deleted successfully', 'success');
                    fetchProducts();
                } else {
                    showToast(response.message || 'Failed to delete product', 'error');
                }
            },
            error: function () {
                showToast('Error deleting product', 'error');
            }
        });
    }
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

    // Category Chart
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
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { position: 'top' }
                }
            }
        });
    }

    // Status Chart
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
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { position: 'bottom' }
                }
            }
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

// Logout function
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

// Document Ready
$(document).ready(function () {
    fetchDashboardData();

    $('#menuToggle').on('click', function () {
        $('#sidebar').toggleClass('open');
    });
});