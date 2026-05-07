// ========================================
// GLOBAL FUNCTIONS
// ========================================

// Toast Notification
function showToast(message, type = 'success') {
    const toast = document.getElementById('toast');
    if (!toast) return;

    toast.textContent = message;
    toast.className = 'toast ' + type;
    toast.style.display = 'block';

    setTimeout(() => {
        toast.style.display = 'none';
    }, 3000);
}

// Get Anti-Forgery Token
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// Logout Function
function logout() {
    fetch('/Account/Logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                window.location.href = '/';
            } else {
                showToast(data.message || 'Logout failed', 'error');
            }
        })
        .catch(error => {
            console.error('Logout error:', error);
            window.location.href = '/';
        });
}

// Mobile menu toggle
document.addEventListener('DOMContentLoaded', function () {
    const menuToggle = document.getElementById('menuToggle');
    const sidebar = document.getElementById('sidebar');

    if (menuToggle && sidebar) {
        menuToggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });

        // Close sidebar when clicking outside on mobile
        document.addEventListener('click', function (e) {
            if (window.innerWidth <= 768) {
                if (!e.target.closest('#sidebar') && !e.target.closest('#menuToggle')) {
                    sidebar.classList.remove('open');
                }
            }
        });
    }
});

// ========================================
// DASHBOARD SPECIFIC FUNCTIONS
// ========================================

let categoryChart = null;
let statusChart = null;

// Load Dashboard Data
function loadDashboardData() {
    // Load stats
    fetch('/Dashboard/GetStats')
        .then(response => response.json())
        .then(data => {
            document.getElementById('totalProducts').textContent = data.totalProducts || 0;
            document.getElementById('lowStockCount').textContent = data.lowStockCount || 0;
            document.getElementById('totalValue').textContent = '$' + (data.totalValue || 0).toLocaleString();
            document.getElementById('categoryCount').textContent = data.categoryCount || 0;
        })
        .catch(error => console.error('Error loading stats:', error));

    // Load products table
    fetch('/Dashboard/GetRecentProducts')
        .then(response => response.json())
        .then(data => {
            const tbody = document.getElementById('tableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;">No products found</td></tr>';
                return;
            }

            tbody.innerHTML = data.map(product => `
                <tr>
                    <td>${escapeHtml(product.name)}</td>
                    <td><span class="category-badge">${escapeHtml(product.categoryName || 'Uncategorized')}</span></td>
                    <td>${product.stockQuantity}</td>
                    <td>$${product.price.toLocaleString()}</td>
                    <td><span class="status ${getStockStatusClass(product.stockQuantity, product.minStockLevel)}">${getStockStatusText(product.stockQuantity, product.minStockLevel)}</span></td>
                    <td class="action-buttons">
                        <button class="action-btn" onclick="openSellModal(${product.id}, '${escapeHtml(product.name)}', ${product.stockQuantity})"><i class="fas fa-shopping-cart"></i> Sell</button>
                    </td>
                </tr>
            `).join('');
        })
        .catch(error => console.error('Error loading products:', error));

    // Load category chart
    fetch('/Dashboard/GetCategoryStockData')
        .then(response => response.json())
        .then(data => {
            if (categoryChart) categoryChart.destroy();

            const ctx = document.getElementById('categoryChart')?.getContext('2d');
            if (!ctx) return;

            categoryChart = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: data.labels || [],
                    datasets: [{
                        label: 'Stock Quantity',
                        data: data.values || [],
                        backgroundColor: 'rgba(102, 126, 234, 0.7)',
                        borderColor: '#667eea',
                        borderWidth: 1
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
        })
        .catch(error => console.error('Error loading category chart:', error));

    // Load status chart
    fetch('/Dashboard/GetStockStatusData')
        .then(response => response.json())
        .then(data => {
            if (statusChart) statusChart.destroy();

            const ctx = document.getElementById('statusChart')?.getContext('2d');
            if (!ctx) return;

            statusChart = new Chart(ctx, {
                type: 'pie',
                data: {
                    labels: ['In Stock', 'Low Stock', 'Out of Stock'],
                    datasets: [{
                        data: [data.inStock || 0, data.lowStock || 0, data.outStock || 0],
                        backgroundColor: ['#28a745', '#ffc107', '#dc3545'],
                        borderWidth: 0
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
        })
        .catch(error => console.error('Error loading status chart:', error));
}

// Helper Functions
function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, function (m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}

function getStockStatusClass(quantity, minLevel) {
    if (quantity <= 0) return 'out-stock';
    if (quantity <= (minLevel || 5)) return 'low-stock';
    return 'in-stock';
}

function getStockStatusText(quantity, minLevel) {
    if (quantity <= 0) return 'Out of Stock';
    if (quantity <= (minLevel || 5)) return 'Low Stock';
    return 'In Stock';
}

// Sell Modal Functions
let currentProductId = null;
let currentProductName = null;
let currentAvailableStock = null;

function openSellModal(productId, productName, availableStock) {
    currentProductId = productId;
    currentProductName = productName;
    currentAvailableStock = availableStock;

    document.getElementById('modalProductName').value = productName;
    document.getElementById('modalProductId').value = productId;
    document.getElementById('modalAvailableStock').value = availableStock;
    document.getElementById('modalQuantity').value = 1;
    document.getElementById('modalQuantity').max = availableStock;
    document.getElementById('modalReason').value = 'Sale';
    document.getElementById('modalRemarks').value = '';

    document.getElementById('sellModal').style.display = 'block';
}

function closeSellModal() {
    document.getElementById('sellModal').style.display = 'none';
    currentProductId = null;
}

// Sell Form Submit
function setupSellForm() {
    const sellForm = document.getElementById('sellForm');
    if (!sellForm) return;

    sellForm.addEventListener('submit', function (e) {
        e.preventDefault();

        const quantity = parseInt(document.getElementById('modalQuantity').value);

        if (quantity > currentAvailableStock) {
            showToast('Not enough stock available!', 'error');
            return;
        }

        const sellData = {
            productId: currentProductId,
            quantity: quantity,
            reason: document.getElementById('modalReason').value,
            remarks: document.getElementById('modalRemarks').value
        };

        fetch('/Stock/SellProduct', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify(sellData)
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showToast('Sale completed successfully!', 'success');
                    closeSellModal();
                    loadDashboardData();
                } else {
                    showToast(data.message || 'Sale failed', 'error');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                showToast('An error occurred', 'error');
            });
    });
}

// Close modal on cancel or close button
document.addEventListener('DOMContentLoaded', function () {
    const closeBtn = document.getElementById('closeSellModalBtn');
    const cancelBtn = document.getElementById('cancelSellBtn');
    const sellModal = document.getElementById('sellModal');

    if (closeBtn) {
        closeBtn.addEventListener('click', closeSellModal);
    }
    if (cancelBtn) {
        cancelBtn.addEventListener('click', closeSellModal);
    }
    if (sellModal) {
        sellModal.addEventListener('click', function (e) {
            if (e.target === sellModal) closeSellModal();
        });
    }

    setupSellForm();

    // Load dashboard data if on dashboard page
    if (document.getElementById('tableBody')) {
        loadDashboardData();
    }
});