// Notification.js - NO SIGNALR, ONLY POLLING

let notificationCheckInterval = null;
let lastNotificationId = 0;

$(document).ready(function () {
    console.log("Notification.js loaded - POLLING MODE");
    initializeNotifications();
    setupNotificationEventHandlers();
});

function initializeNotifications() {
    loadNotifications();
    startPolling();
}

function startPolling() {
    if (notificationCheckInterval) clearInterval(notificationCheckInterval);

    // Initial check
    checkNewNotifications();

    // Poll every 10 seconds
    notificationCheckInterval = setInterval(function () {
        checkNewNotifications();
    }, 10000);
}

function checkNewNotifications() {
    $.ajax({
        url: '/Notification/GetLatestNotifications',
        type: 'GET',
        data: { lastId: lastNotificationId },
        success: function (notifications) {
            if (notifications && notifications.length > 0) {
                for (var i = 0; i < notifications.length; i++) {
                    addNotificationToUI(notifications[i]);
                    showToastNotification(notifications[i]);
                }
                updateBadgeCount();
                if (notifications.length > 0) {
                    lastNotificationId = notifications[notifications.length - 1].notificationId || lastNotificationId;
                }
            }
        },
        error: function (xhr) {
            console.log("Polling error:", xhr.status);
        }
    });
}

function loadNotifications() {
    $('#notificationList').html('<div class="loading"><i class="fas fa-spinner fa-pulse"></i> Loading...</div>');

    $.ajax({
        url: '/Notification/GetUserNotifications',
        type: 'GET',
        success: function (notifications) {
            console.log("Notifications loaded:", notifications);
            displayNotifications(notifications);
            updateBadgeCount();
            if (notifications && notifications.length > 0) {
                lastNotificationId = notifications[0].notificationId || 0;
            }
        },
        error: function (xhr) {
            console.error("Error loading notifications:", xhr);
            $('#notificationList').html('<div class="error"><i class="fas fa-exclamation-triangle"></i> Error loading notifications (Status: ' + xhr.status + ')</div>');
        }
    });
}

function displayNotifications(notifications) {
    var container = $('#notificationList');

    if (!notifications || notifications.length === 0) {
        container.html('<div class="empty-state"><i class="fas fa-bell-slash"></i> No notifications yet</div>');
        return;
    }

    var html = '';
    for (var i = 0; i < notifications.length; i++) {
        var n = notifications[i];
        var isRead = n.isRead ? 'read' : 'unread';
        var iconClass = getNotificationIcon(n.type);

        html += `
            <div class="notification-item ${isRead}" data-id="${n.notificationId}">
                <div class="notification-icon ${(n.type || 'System').toLowerCase()}">
                    <i class="${iconClass}"></i>
                </div>
                <div class="notification-content">
                    <div class="notification-title">${escapeHtml(n.title)}</div>
                    <div class="notification-message">${escapeHtml(n.message)}</div>
                    <div class="notification-time">${getTimeAgo(n.createdAt)}</div>
                </div>
                <div class="notification-actions">
                    <button class="mark-read-btn" onclick="markAsRead(${n.notificationId})" title="Mark as read">
                        <i class="fas fa-check-circle"></i>
                    </button>
                </div>
            </div>
        `;
    }

    container.html(html);
}

function addNotificationToUI(notification) {
    var container = $('#notificationList');
    var isRead = notification.isRead ? 'read' : 'unread';
    var iconClass = getNotificationIcon(notification.type);
    var notificationId = notification.notificationId;

    var newHtml = `
        <div class="notification-item ${isRead} new-notification" data-id="${notificationId}">
            <div class="notification-icon ${(notification.type || 'System').toLowerCase()}">
                <i class="${iconClass}"></i>
            </div>
            <div class="notification-content">
                <div class="notification-title">${escapeHtml(notification.title)}</div>
                <div class="notification-message">${escapeHtml(notification.message)}</div>
                <div class="notification-time">Just now</div>
            </div>
            <div class="notification-actions">
                <button class="mark-read-btn" onclick="markAsRead(${notificationId})" title="Mark as read">
                    <i class="fas fa-check-circle"></i>
                </button>
            </div>
        </div>
    `;

    if (container.find('.empty-state').length) {
        container.html(newHtml);
    } else {
        container.prepend(newHtml);
    }

    setTimeout(function () {
        $('.new-notification').removeClass('new-notification');
    }, 1000);
}

function markAsRead(notificationId) {
    var token = $('input[name="__RequestVerificationToken"]').val() ||
        $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Notification/MarkAsRead',
        type: 'POST',
        data: { id: notificationId, __RequestVerificationToken: token },
        success: function (response) {
            if (response.success) {
                $('#notificationList .notification-item[data-id="' + notificationId + '"]')
                    .removeClass('unread').addClass('read');
                updateBadgeCount();
            }
        },
        error: function (xhr) {
            console.error("Error marking as read:", xhr);
        }
    });
}

function markAllAsRead() {
    var token = $('input[name="__RequestVerificationToken"]').val() ||
        $('#antiForgeryForm input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Notification/MarkAllAsRead',
        type: 'POST',
        data: { __RequestVerificationToken: token },
        success: function (response) {
            if (response.success) {
                $('#notificationList .notification-item').removeClass('unread').addClass('read');
                updateBadgeCount();
                showToast("All notifications marked as read", "success");
            }
        },
        error: function () {
            console.error("Error marking all as read");
        }
    });
}

function updateBadgeCount() {
    $.ajax({
        url: '/Notification/GetUnreadCount',
        type: 'GET',
        success: function (count) {
            var badge = $('#notificationBadge');
            if (count > 0) {
                badge.text(count > 99 ? '99+' : count);
                badge.show();
            } else {
                badge.hide();
            }
        },
        error: function () {
            var unreadCount = $('#notificationList .notification-item.unread').length;
            var badge = $('#notificationBadge');
            if (unreadCount > 0) {
                badge.text(unreadCount > 99 ? '99+' : unreadCount);
                badge.show();
            } else {
                badge.hide();
            }
        }
    });
}

function getNotificationIcon(type) {
    var icons = {
        'LowStock': 'fas fa-exclamation-triangle',
        'OutOfStock': 'fas fa-times-circle',
        'StockIn': 'fas fa-arrow-down',
        'StockOut': 'fas fa-arrow-up',
        'InvalidStock': 'fas fa-ban',
        'NewProduct': 'fas fa-box',
        'UserActivity': 'fas fa-user-clock',
        'System': 'fas fa-bell'
    };
    return icons[type] || 'fas fa-bell';
}

function showToastNotification(notification) {
    var iconClass = getNotificationIcon(notification.type);
    var toastHtml = `
        <div class="custom-toast ${(notification.type || 'System').toLowerCase()}">
            <div class="toast-icon">
                <i class="${iconClass}"></i>
            </div>
            <div class="toast-content">
                <div class="toast-title">${escapeHtml(notification.title)}</div>
                <div class="toast-message">${escapeHtml(notification.message)}</div>
            </div>
            <div class="toast-close">
                <i class="fas fa-times"></i>
            </div>
        </div>
    `;

    var $toast = $(toastHtml);
    $('body').append($toast);

    setTimeout(function () { $toast.addClass('show'); }, 100);
    setTimeout(function () {
        $toast.removeClass('show');
        setTimeout(function () { $toast.remove(); }, 300);
    }, 5000);

    $toast.find('.toast-close').on('click', function () {
        $toast.removeClass('show');
        setTimeout(function () { $toast.remove(); }, 300);
    });
}

function showToast(message, type) {
    var toast = $('#toast');
    toast.removeClass('success error warning info').addClass(type || 'success');
    toast.html(message);
    toast.fadeIn(300);
    setTimeout(function () { toast.fadeOut(300); }, 3000);
}

function getTimeAgo(dateString) {
    if (!dateString) return 'Just now';
    var date = new Date(dateString);
    var now = new Date();
    var seconds = Math.floor((now - date) / 1000);
    if (isNaN(seconds)) return 'Just now';
    if (seconds < 60) return 'Just now';
    var minutes = Math.floor(seconds / 60);
    if (minutes < 60) return minutes + ' min' + (minutes > 1 ? 's' : '') + ' ago';
    var hours = Math.floor(minutes / 60);
    if (hours < 24) return hours + ' hour' + (hours > 1 ? 's' : '') + ' ago';
    var days = Math.floor(hours / 24);
    if (days < 7) return days + ' day' + (days > 1 ? 's' : '') + ' ago';
    return date.toLocaleDateString();
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

function setupNotificationEventHandlers() {
    $('#notificationBell').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $('#notificationDropdown').toggle();
        if ($('#notificationDropdown').is(':visible')) {
            loadNotifications();
        }
    });

    $('#markAllReadBtn').on('click', function () {
        markAllAsRead();
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.notification-container').length) {
            $('#notificationDropdown').hide();
        }
    });
}

$(window).on('beforeunload', function () {
    if (notificationCheckInterval) {
        clearInterval(notificationCheckInterval);
    }
});