// Reports.js - NO ALERTS, ONLY TOAST NOTIFICATIONS
var stockChart, movementChart;
var currentHistoryData = [];
var currentDisplayedData = [];
var currentTypeFilter = "";

$(document).ready(function () {
    console.log("=== REPORTS.JS LOADED - UPDATED VERSION ===");

    loadSummary();
    loadMovementChart();
    loadStockHistory();

    $('#filterDateBtn').on('click', function () {
        console.log("Date filter clicked");
        loadStockHistory();
    });

    $('#resetDateBtn').on('click', function () {
        console.log("Reset clicked");
        $('#fromDate').val('');
        $('#toDate').val('');
        $('#typeFilter').val('');
        currentTypeFilter = "";
        loadStockHistory();
        showToast("Filters reset", "success");
    });

    $('#applyTypeFilter').on('click', function () {
        var selectedType = $('#typeFilter').val();
        currentTypeFilter = selectedType;
        console.log("=== APPLY FILTER CLICKED ===");
        console.log("Selected type:", selectedType);
        console.log("Total history data:", currentHistoryData.length);

        if (!selectedType || selectedType === "") {
            currentDisplayedData = [...currentHistoryData];
            console.log("No filter - showing all:", currentDisplayedData.length);
        } else {
            currentDisplayedData = [];
            for (var i = 0; i < currentHistoryData.length; i++) {
                if (currentHistoryData[i].type === selectedType) {
                    currentDisplayedData.push(currentHistoryData[i]);
                }
            }
            console.log("Filtered data - found:", currentDisplayedData.length, "records");
        }

        displayHistory(currentDisplayedData);
        showToast("Showing " + currentDisplayedData.length + " records", "success");
    });

    $('#exportBtn').on('click', function () {
        console.log("=== EXPORT BUTTON CLICKED ===");
        console.log("Current type filter:", currentTypeFilter);

        var dataToExport = [];
        var selectedFilter = currentTypeFilter !== "" ? currentTypeFilter : $('#typeFilter').val();

        console.log("Filter to use:", selectedFilter);

        if (selectedFilter && selectedFilter !== "") {
            for (var i = 0; i < currentHistoryData.length; i++) {
                if (currentHistoryData[i].type === selectedFilter) {
                    dataToExport.push(currentHistoryData[i]);
                }
            }
            console.log("Exporting filtered data:", dataToExport.length, "records");
        } else {
            dataToExport = [...currentDisplayedData];
            console.log("Exporting all data:", dataToExport.length, "records");
        }

        if (dataToExport.length === 0) {
            showToast("No data to export!", "error");
            return;
        }

        // Create CSV
        var csvContent = "Date,Type,Product,Quantity,Reference\n";
        for (var i = 0; i < dataToExport.length; i++) {
            var item = dataToExport[i];
            csvContent += '"' + (item.date ? new Date(item.date).toLocaleDateString() : '') + '",';
            csvContent += '"' + (item.type || '') + '",';
            csvContent += '"' + (item.productName || '') + '",';
            csvContent += '"' + (item.quantity || 0) + '",';
            csvContent += '"' + (item.reference || '') + '"';
            if (i < dataToExport.length - 1) csvContent += "\n";
        }

        var filename = 'stock_report_' + new Date().toISOString().split('T')[0];
        if (selectedFilter && selectedFilter !== "") {
            filename += '_' + selectedFilter.replace(/ /g, '_');
        }
        filename += '.csv';

        var blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement('a');
        var url = URL.createObjectURL(blob);
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);

        showToast("Exported " + dataToExport.length + " records!", "success");
        console.log("Export completed!");
    });
});

function showToast(message, type) {
    var toast = $('#toast');
    toast.removeClass('success error').addClass(type).text(message);
    toast.fadeIn(300);
    setTimeout(function () { toast.fadeOut(300); }, 3000);
}

function loadSummary() {
    console.log("Loading summary...");
    $.ajax({
        url: '/Stock/GetDashboardStats',
        type: 'GET',
        success: function (data) {
            console.log("Summary loaded");
            if (data) {
                $('#totalProducts').text(data.totalProducts || 0);
                $('#lowStockCount').text(data.lowStockCount || 0);
                $('#outOfStockCount').text(data.outOfStockCount || 0);
                $('#totalValue').text('$' + (data.totalStockValue || 0).toFixed(2));
                createPieChart(data.totalProducts || 0, data.lowStockCount || 0, data.outOfStockCount || 0);
            }
        }
    });
}

function createPieChart(total, low, out) {
    var healthy = total - low - out;
    if (healthy < 0) healthy = 0;
    var canvas = document.getElementById('stockChart');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    if (stockChart) stockChart.destroy();
    stockChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Healthy Stock', 'Low Stock', 'Out of Stock'],
            datasets: [{ data: [healthy, low, out], backgroundColor: ['#28a745', '#ffc107', '#dc3545'], borderWidth: 0 }]
        },
        options: { responsive: true, maintainAspectRatio: true, plugins: { legend: { position: 'bottom' } } }
    });
}

function loadMovementChart() {
    console.log("Loading movement chart...");
    $.ajax({
        url: '/Stock/GetStockHistory',
        type: 'GET',
        success: function (data) { createMovementChart(data || []); }
    });
}

function createMovementChart(historyData) {
    var months = [], stockInData = [], stockOutData = [];
    for (var i = 5; i >= 0; i--) {
        var date = new Date();
        date.setMonth(date.getMonth() - i);
        months.push(date.toLocaleString('default', { month: 'short' }) + ' ' + date.getFullYear());
        var stockIn = 0, stockOut = 0;
        if (historyData && historyData.length) {
            for (var j = 0; j < historyData.length; j++) {
                var item = historyData[j];
                if (item.date) {
                    var itemDate = new Date(item.date);
                    if (itemDate.getMonth() === date.getMonth() && itemDate.getFullYear() === date.getFullYear()) {
                        if (item.type === 'Stock In') stockIn += item.quantity || 0;
                        else if (item.type === 'Stock Out') stockOut += item.quantity || 0;
                    }
                }
            }
        }
        stockInData.push(stockIn);
        stockOutData.push(stockOut);
    }
    var canvas = document.getElementById('movementChart');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    if (movementChart) movementChart.destroy();
    movementChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: months,
            datasets: [
                { label: 'Stock In', data: stockInData, borderColor: '#28a745', backgroundColor: 'rgba(40, 167, 69, 0.1)', borderWidth: 2, fill: true },
                { label: 'Stock Out', data: stockOutData, borderColor: '#dc3545', backgroundColor: 'rgba(220, 53, 69, 0.1)', borderWidth: 2, fill: true }
            ]
        },
        options: { responsive: true, maintainAspectRatio: true }
    });
}

function loadStockHistory() {
    var fromDate = $('#fromDate').val(), toDate = $('#toDate').val();
    $('#historyTableBody').html('<tr><td colspan="5">Loading...<\/td><\/tr>');
    $.ajax({
        url: '/Stock/GetStockHistory',
        type: 'GET',
        data: { fromDate: fromDate, toDate: toDate },
        success: function (data) {
            currentHistoryData = data || [];
            currentTypeFilter = "";
            $('#typeFilter').val("");
            currentDisplayedData = [...currentHistoryData];
            displayHistory(currentDisplayedData);
            console.log("Loaded", currentHistoryData.length, "records");
        }
    });
}

function displayHistory(data) {
    if (!data || data.length === 0) {
        $('#historyTableBody').html('<tr><td colspan="5">No records found<\/td><\/tr>');
        $('#showingCount').text('0');
        return;
    }
    var html = '';
    for (var i = 0; i < data.length; i++) {
        var item = data[i];
        var typeClass = (item.type === 'Stock In') ? 'badge-success' : 'badge-danger';
        html += '<tr>';
        html += '<td>' + (item.date ? new Date(item.date).toLocaleDateString() : '-') + '</td>';
        html += '<td><span class="' + typeClass + '">' + escapeHtml(item.type) + '</span></td>';
        html += '<td>' + escapeHtml(item.productName) + '</td>';
        html += '<td>' + (item.quantity || 0) + '</td>';
        html += '<td>' + escapeHtml(item.reference || '-') + '</td>';
        html += '</tr>';
    }
    $('#historyTableBody').html(html);
    $('#showingCount').text(data.length);
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str).replace(/[&<>]/g, function (m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}