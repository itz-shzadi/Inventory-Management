
        $(document).ready(function() {
            loadCategories();
            loadSuppliers();
            loadCurrentStock();

            $('#searchBtn').on('click', function() {
                searchStock();
            });

            $('#resetBtn').on('click', function() {
                $('#searchInput').val('');
                $('#categoryFilter').val('');
                $('#supplierFilter').val('');
                loadCurrentStock();
            });

            $('#filterBtn').on('click', function() {
                filterStock();
            });

            $('#searchInput').on('keypress', function(e) {
                if (e.which === 13) searchStock();
            });
        });

        function showToast(message, type) {
            $('#toast').removeClass('success error').addClass(type).text(message).fadeIn(300);
            setTimeout(function() { $('#toast').fadeOut(300); }, 3000);
        }

        function loadCategories() {
            $.ajax({
                url: '/Product/GetCategories',
                type: 'GET',
                success: function(categories) {
                    var options = '<option value="">All Categories</option>';
                    for (var i = 0; i < categories.length; i++) {
                        options += '<option value="' + categories[i].id + '">' + categories[i].name + '</option>';
                    }
                    $('#categoryFilter').html(options);
                }
            });
        }

        function loadSuppliers() {
            $.ajax({
                url: '/Product/GetSuppliers',
                type: 'GET',
                success: function(suppliers) {
                    var options = '<option value="">All Suppliers</option>';
                    for (var i = 0; i < suppliers.length; i++) {
                        options += '<option value="' + suppliers[i].id + '">' + suppliers[i].name + '</option>';
                    }
                    $('#supplierFilter').html(options);
                }
            });
        }

        function loadCurrentStock() {
            $.ajax({
                url: '/Product/GetAllProducts',
                type: 'GET',
                success: function(products) {
                    displayStock(products);
                    updateSummary(products);
                },
                error: function() {
                    showToast('Error loading stock data', 'error');
                }
            });
        }

        function displayStock(products) {
            if (!products || products.length === 0) {
                $('#stockTableBody').html('<tr><td colspan="8" style="text-align:center;">No products found</td></tr>');
                $('#showingCount').text('0');
                return;
            }

            var html = '';
            for (var i = 0; i < products.length; i++) {
                var p = products[i];
                var stockClass = '';
                var stockStatus = '';
                
                if (p.quantity === 0) {
                    stockClass = 'stock-out';
                    stockStatus = '<span class="badge-danger">Out of Stock</span>';
                } else if (p.quantity <= 10) {
                    stockClass = 'stock-low';
                    stockStatus = '<span class="badge-warning">Low Stock</span>';
                } else {
                    stockClass = 'stock-good';
                    stockStatus = '<span class="badge-success">In Stock</span>';
                }
                
                var stockValue = p.quantity * p.purchasePrice;
                
                html += '<tr class="' + stockClass + '">';
                html += '<td><strong>' + escapeHtml(p.productName) + '</strong></td>';
                html += '<td>' + escapeHtml(p.categoryName) + '</td>';
                html += '<td>' + escapeHtml(p.supplierName) + '</td>';
                html += '<td>' + p.quantity + ' ' + (p.unit || '') + '<br>' + stockStatus + '</td>';
                html += '<td>' + (p.unit || '-') + '</td>';
                html += '<td>$' + p.purchasePrice.toFixed(2) + '</td>';
                html += '<td>$' + p.salePrice.toFixed(2) + '</td>';
                html += '<td>$' + stockValue.toFixed(2) + '</td>';
                html += '</tr>';
            }
            $('#stockTableBody').html(html);
            $('#showingCount').text(products.length);
        }

        function updateSummary(products) {
            var totalProducts = products.length;
            var totalValue = 0;
            for (var i = 0; i < products.length; i++) {
                totalValue += products[i].quantity * products[i].purchasePrice;
            }
            $('#totalProducts').text(totalProducts);
            $('#totalValue').text('$' + totalValue.toFixed(2));
        }

        function searchStock() {
            var searchTerm = $('#searchInput').val().trim();
            if (!searchTerm) {
                loadCurrentStock();
                return;
            }

            $.ajax({
                url: '/Product/SearchProducts',
                type: 'GET',
                data: { searchTerm: searchTerm },
                success: function(products) {
                    displayStock(products);
                    updateSummary(products);
                }
            });
        }

        function filterStock() {
            var categoryId = $('#categoryFilter').val();
            var supplierId = $('#supplierFilter').val();

            $.ajax({
                url: '/Product/FilterProducts',
                type: 'GET',
                data: { categoryId: categoryId, supplierId: supplierId },
                success: function(products) {
                    displayStock(products);
                    updateSummary(products);
                }
            });
        }

        function escapeHtml(str) {
            if (!str) return '';
            return $('<div>').text(str).html();
        }
   