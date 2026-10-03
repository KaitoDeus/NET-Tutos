// Dark/Light Theme Switcher
document.addEventListener('DOMContentLoaded', () => {
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIcon = document.getElementById('themeIcon');
    const htmlElement = document.documentElement;

    const savedTheme = localStorage.getItem('dotnet-theme') || 
        (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');

    setTheme(savedTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            const currentTheme = htmlElement.getAttribute('data-bs-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
        });
    }

    function setTheme(theme) {
        htmlElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('dotnet-theme', theme);

        if (themeIcon) {
            if (theme === 'dark') {
                themeIcon.className = 'bi bi-sun-fill text-warning';
            } else {
                themeIcon.className = 'bi bi-moon-stars-fill text-secondary';
            }
        }

        document.querySelectorAll('.mobile-theme-icon').forEach(icon => {
            if (theme === 'dark') {
                icon.className = 'bi bi-sun-fill text-warning mobile-theme-icon';
            } else {
                icon.className = 'bi bi-moon-stars-fill text-secondary mobile-theme-icon';
            }
        });
    }

    // Reading Progress Bar
    const progressBar = document.getElementById('readingProgressBar');
    if (progressBar) {
        window.addEventListener('scroll', () => {
            const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
            const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
            if (height > 0) {
                const scrolled = (winScroll / height) * 100;
                progressBar.style.width = scrolled + '%';
            }
        });
    }

    // Auto-detect and format Code Blocks for Prism
    document.querySelectorAll('.markdown-body pre code').forEach(block => {
        if (!block.className || block.className === '') {
            block.className = 'language-csharp';
        }
    });

    if (window.Prism) {
        Prism.highlightAll();
    }

    // Reset YouTube iframe video on modal close
    const videoModal = document.getElementById('videoModal');
    if (videoModal) {
        videoModal.addEventListener('hidden.bs.modal', () => {
            const iframe = document.getElementById('videoIframe');
            if (iframe) {
                const currentSrc = iframe.src;
                iframe.src = currentSrc;
            }
        });
    }

    // Daily Learning Streak System
    initStreakSystem();

    // Real-time Notification Center & Activity Feed
    initNotificationSystem();

    // Desktop Hover Navigation Dropdowns
    initNavDropdownHover();

    // Language Switcher System
    initLanguageSwitcher();
});

function initNavDropdownHover() {
    const navDropdowns = document.querySelectorAll('.navbar-nav .dropdown, .lang-dropdown-container');
    navDropdowns.forEach(dropdown => {
        let hideTimeout;
        const toggle = dropdown.querySelector('[data-bs-toggle="dropdown"]');
        const menu = dropdown.querySelector('.dropdown-menu');

        if (!toggle || !menu) return;

        // Hover enter: open immediately & clear pending close timeout
        dropdown.addEventListener('mouseenter', () => {
            if (window.innerWidth >= 992) {
                clearTimeout(hideTimeout);
                // Close other nav dropdowns
                navDropdowns.forEach(other => {
                    if (other !== dropdown) {
                        const otherMenu = other.querySelector('.dropdown-menu');
                        const otherToggle = other.querySelector('[data-bs-toggle="dropdown"]');
                        if (otherMenu) otherMenu.classList.remove('show');
                        if (otherToggle) {
                            otherToggle.classList.remove('show');
                            otherToggle.setAttribute('aria-expanded', 'false');
                        }
                    }
                });
                menu.classList.add('show');
                toggle.classList.add('show');
                toggle.setAttribute('aria-expanded', 'true');
            }
        });

        // Hover leave: 250ms grace period so cursor can effortlessly travel into dropdown
        dropdown.addEventListener('mouseleave', () => {
            if (window.innerWidth >= 992) {
                hideTimeout = setTimeout(() => {
                    menu.classList.remove('show');
                    toggle.classList.remove('show');
                    toggle.setAttribute('aria-expanded', 'false');
                }, 250);
            }
        });

        // Close on item click
        menu.querySelectorAll('.dropdown-item').forEach(item => {
            item.addEventListener('click', () => {
                if (window.innerWidth >= 992) {
                    menu.classList.remove('show');
                    toggle.classList.remove('show');
                    toggle.setAttribute('aria-expanded', 'false');
                }
            });
        });
    });
}

function initLanguageSwitcher() {
    const langItems = document.querySelectorAll('.lang-item');
    langItems.forEach(item => {
        item.addEventListener('click', function() {
            const href = this.getAttribute('href') || '';
            const isEn = href.includes('culture=en');
            const targetCulture = isEn ? 'en' : 'vi';
            localStorage.setItem('nettutos-culture', targetCulture);

            // Instantly update flag images before reload for zero flicker UX
            document.querySelectorAll('.lang-flag-current').forEach(flag => {
                flag.src = isEn ? '/images/flags/en.svg' : '/images/flags/vn.svg';
                flag.alt = isEn ? 'English' : 'Tiếng Việt';
            });
        });
    });
}

function initStreakSystem() {
    const navStreakBtn = document.getElementById('btnNavStreak');
    const streakModal = document.getElementById('streakModal');
    if (!navStreakBtn && !streakModal) return;

    // Fetch current status on load
    loadStreakStatus();

    // Hook up modal check-in button
    const btnDoCheckIn = document.getElementById('btnDoCheckIn');
    if (btnDoCheckIn) {
        btnDoCheckIn.addEventListener('click', handleDailyCheckIn);
    }

    // Refresh status when modal is opened
    if (streakModal) {
        streakModal.addEventListener('show.bs.modal', loadStreakStatus);
    }
}

async function loadStreakStatus() {
    try {
        const response = await fetch('/Streak/Status');
        if (!response.ok) return;
        const data = await response.json();
        if (!data.isAuthenticated) return;

        updateStreakUI(data);
    } catch (err) {
        console.error('Error fetching streak status:', err);
    }
}

function updateStreakUI(data) {
    const navStreakCount = document.getElementById('navStreakCountText');
    const btnNavStreak = document.getElementById('btnNavStreak');
    const modalStreakCount = document.getElementById('modalStreakCount');
    const modalStreakStatus = document.getElementById('modalStreakStatusText');
    const btnDoCheckIn = document.getElementById('btnDoCheckIn');
    const btnCheckInText = document.getElementById('btnCheckInText');
    const checkInIcon = document.getElementById('checkInIcon');
    const totalStreakXpText = document.getElementById('totalStreakXpText');
    const past7DaysContainer = document.getElementById('past7DaysContainer');

    // Update navbar badge
    if (navStreakCount) {
        navStreakCount.textContent = `${data.currentStreak} ngày`;
    }
    if (btnNavStreak) {
        if (data.hasCheckedInToday) {
            btnNavStreak.className = 'btn btn-sm btn-warning text-dark rounded-pill px-2.5 py-1.5 d-flex align-items-center gap-1.5 text-nowrap fw-bold shadow-sm';
            btnNavStreak.title = `Chuỗi ${data.currentStreak} ngày (Hôm nay đã điểm danh)`;
        } else {
            btnNavStreak.className = 'btn btn-sm btn-outline-warning rounded-pill px-2.5 py-1.5 d-flex align-items-center gap-1.5 text-nowrap fw-bold shadow-none';
            btnNavStreak.title = `Chuỗi ${data.currentStreak} ngày (Bấm để điểm danh hôm nay)`;
        }
    }

    // Update Modal
    if (modalStreakCount) {
        modalStreakCount.textContent = data.currentStreak;
    }
    if (modalStreakStatus) {
        if (data.hasCheckedInToday) {
            modalStreakStatus.innerHTML = `🔥 Bạn đã điểm danh hôm nay! Chuỗi <strong>${data.currentStreak} ngày</strong> tiếp tục bùng cháy. Kỷ lục của bạn: <strong>${data.longestStreak} ngày</strong>.`;
        } else if (data.currentStreak > 0) {
            modalStreakStatus.innerHTML = `⚠️ Hãy điểm danh hôm nay để duy trì chuỗi <strong>${data.currentStreak} ngày</strong> và nhận ngay <strong>+${data.nextRewardXp} XP</strong>!`;
        } else {
            modalStreakStatus.innerHTML = `Bắt đầu xây dựng chuỗi học tập ngay hôm nay và nhận ngay <strong>+${data.nextRewardXp} XP</strong>!`;
        }
    }

    if (btnDoCheckIn && btnCheckInText) {
        if (data.hasCheckedInToday) {
            btnDoCheckIn.disabled = true;
            btnDoCheckIn.className = 'btn btn-success btn-lg rounded-pill fw-bold px-4 py-2.5 shadow-sm w-100 d-inline-flex align-items-center justify-content-center gap-2 text-white';
            btnCheckInText.textContent = `✓ Đã điểm danh hôm nay!`;
            if (checkInIcon) checkInIcon.className = 'bi bi-check-circle-fill fs-5 text-white';
        } else {
            btnDoCheckIn.disabled = false;
            btnDoCheckIn.className = 'btn btn-warning btn-lg rounded-pill fw-bold px-4 py-2.5 shadow w-100 d-inline-flex align-items-center justify-content-center gap-2 text-dark';
            btnCheckInText.textContent = `Điểm danh hôm nay (+${data.nextRewardXp} XP)`;
            if (checkInIcon) checkInIcon.className = 'bi bi-lightning-charge-fill text-danger fs-5';
        }
    }

    if (totalStreakXpText) {
        totalStreakXpText.textContent = `+${data.totalXpEarnedFromStreaks} XP đã tích lũy`;
    }

    // Render Past 7 Days Progress
    if (past7DaysContainer && data.past7Days && data.past7Days.length > 0) {
        past7DaysContainer.innerHTML = '';
        data.past7Days.forEach(day => {
            const dayCol = document.createElement('div');
            dayCol.className = 'text-center flex-fill';

            let circleClass = 'day-tracker-circle unchecked mx-auto mb-1';
            let circleContent = day.dayName;

            if (day.isCheckedIn) {
                if (day.isToday) {
                    circleClass = 'day-tracker-circle today-active mx-auto mb-1';
                    circleContent = '<i class="bi bi-fire"></i>';
                } else {
                    circleClass = 'day-tracker-circle checked mx-auto mb-1';
                    circleContent = '<i class="bi bi-check-lg"></i>';
                }
            } else if (day.isToday) {
                circleClass = 'day-tracker-circle border border-2 border-warning text-warning mx-auto mb-1';
                circleContent = '<i class="bi bi-fire"></i>';
            }

            dayCol.innerHTML = `
                <div class="${circleClass}">
                    ${circleContent}
                </div>
                <div class="small fw-semibold text-muted" style="font-size: 0.72rem;">${day.dayName}</div>
            `;
            past7DaysContainer.appendChild(dayCol);
        });
    }
}

async function handleDailyCheckIn() {
    const btnDoCheckIn = document.getElementById('btnDoCheckIn');
    const btnCheckInText = document.getElementById('btnCheckInText');
    const checkInSpinner = document.getElementById('checkInSpinner');
    const checkInIcon = document.getElementById('checkInIcon');

    if (!btnDoCheckIn || btnDoCheckIn.disabled) return;

    btnDoCheckIn.disabled = true;
    if (checkInSpinner) checkInSpinner.classList.remove('d-none');
    if (checkInIcon) checkInIcon.classList.add('d-none');
    if (btnCheckInText) btnCheckInText.textContent = 'Đang ghi nhận...';

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    try {
        const response = await fetch('/Streak/CheckIn', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/x-www-form-urlencoded'
            }
        });

        const result = await response.json();
        if (result.success) {
            // Re-fetch fresh full status
            await loadStreakStatus();

            // Display celebration message
            if (result.newlyUnlockedBadges && result.newlyUnlockedBadges.length > 0) {
                alert(`🎉 CHÚC MỪNG!\n${result.message}\n\n🏆 THÀNH TỰU MỚI:\n${result.newlyUnlockedBadges.join('\n')}`);
            }
        } else {
            alert(result.message || 'Không thể điểm danh lúc này, vui lòng thử lại sau.');
            if (btnDoCheckIn) btnDoCheckIn.disabled = false;
        }
    } catch (err) {
        console.error('Error during check-in:', err);
        alert('Đã có lỗi xảy ra trong quá trình điểm danh. Vui lòng kiểm tra lại kết nối mạng.');
        if (btnDoCheckIn) btnDoCheckIn.disabled = false;
    } finally {
        if (checkInSpinner) checkInSpinner.classList.add('d-none');
        if (checkInIcon) checkInIcon.classList.remove('d-none');
    }
}

/* ==========================================================================
   Real-time Notification Center & Activity Feed System
   ========================================================================== */
let notificationHubConn = null;

function initNotificationSystem() {
    const btnNavNotification = document.getElementById('btnNavNotification');
    if (!btnNavNotification) return;

    // Load initial notifications
    loadNotifications();

    // Hook up Mark All As Read button
    const btnMarkAllRead = document.getElementById('btnMarkAllRead');
    if (btnMarkAllRead) {
        btnMarkAllRead.addEventListener('click', handleMarkAllNotificationsRead);
    }

    // Connect to SignalR NotificationHub
    if (window.signalR) {
        setupNotificationHubConnection();
    }
}

async function setupNotificationHubConnection() {
    try {
        notificationHubConn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect()
            .build();

        notificationHubConn.on('ReceiveNotification', (notification) => {
            handleReceiveNotification(notification);
        });

        notificationHubConn.on('UpdateUnreadCount', (count) => {
            updateNotificationBadges(count);
        });

        notificationHubConn.on('ReceiveActivity', (activity) => {
            handleReceiveActivity(activity);
        });

        await notificationHubConn.start();
        console.log('[SignalR] Notification Hub connected successfully.');
    } catch (err) {
        console.warn('[SignalR] Notification Hub connection failed:', err);
    }
}

async function loadNotifications() {
    try {
        const response = await fetch('/Notification/GetLatest');
        if (!response.ok) return;

        const data = await response.json();
        updateNotificationBadges(data.unreadCount || 0);
        renderNotificationItems(data.items || []);
    } catch (err) {
        console.warn('Error loading notifications:', err);
    }
}

function updateNotificationBadges(count) {
    const navBadge = document.getElementById('navNotificationBadge');
    const mobileBadge = document.getElementById('mobileNotificationBadge');
    const unreadPill = document.getElementById('notificationUnreadPill');
    const bellIcon = document.getElementById('navBellIcon');

    if (count > 0) {
        if (navBadge) {
            navBadge.textContent = count > 99 ? '99+' : count;
            navBadge.classList.remove('d-none');
        }
        if (mobileBadge) {
            mobileBadge.textContent = count > 99 ? '99+' : count;
            mobileBadge.classList.remove('d-none');
        }
        if (unreadPill) {
            unreadPill.textContent = `${count} mới`;
            unreadPill.classList.remove('d-none');
        }
        if (bellIcon) {
            bellIcon.classList.add('bell-ring');
            setTimeout(() => bellIcon.classList.remove('bell-ring'), 800);
        }
    } else {
        if (navBadge) navBadge.classList.add('d-none');
        if (mobileBadge) mobileBadge.classList.add('d-none');
        if (unreadPill) unreadPill.classList.add('d-none');
    }
}

function renderNotificationItems(items) {
    const container = document.getElementById('notificationListItems');
    if (!container) return;

    if (!items || items.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5 text-muted px-3">
                <i class="bi bi-bell-slash fs-1 d-block mb-2 text-secondary opacity-50"></i>
                <div class="small fw-semibold">Bạn không có thông báo mới nào</div>
                <div class="text-muted" style="font-size: 0.78rem;">Các hoạt động học tập sẽ hiển thị tại đây</div>
            </div>
        `;
        return;
    }

    container.innerHTML = '';
    items.forEach(item => {
        container.appendChild(createNotificationElement(item));
    });
}

function createNotificationElement(item) {
    const div = document.createElement('div');
    div.className = `notification-item ${item.isRead ? '' : 'unread'}`;
    div.setAttribute('data-id', item.id);

    div.innerHTML = `
        <div class="d-flex align-items-start gap-2.5">
            <div class="rounded-circle bg-body-secondary ${item.colorClass} p-2 d-flex align-items-center justify-content-center flex-shrink-0" style="width: 36px; height: 36px;">
                <i class="bi ${item.iconClass || 'bi-bell'} fs-6"></i>
            </div>
            <div class="flex-grow-1 min-w-0">
                <div class="d-flex align-items-center justify-content-between gap-1 mb-0.5">
                    <span class="fw-bold small text-truncate text-body-emphasis">${escapeHtml(item.title)}</span>
                    ${!item.isRead ? '<span class="notification-unread-dot ms-1" title="Chưa đọc"></span>' : ''}
                </div>
                <p class="text-body-secondary small mb-1 lh-sm" style="font-size: 0.8rem;">
                    ${escapeHtml(item.message)}
                </p>
                <div class="d-flex align-items-center gap-2" style="font-size: 0.72rem;">
                    <span class="text-muted"><i class="bi bi-clock me-1"></i>${escapeHtml(item.timeAgo)}</span>
                    <span class="badge bg-secondary bg-opacity-10 text-secondary border py-0 px-1.5">${escapeHtml(item.typeName || '')}</span>
                </div>
            </div>
        </div>
    `;

    div.addEventListener('click', async (e) => {
        e.preventDefault();
        if (!item.isRead) {
            await handleMarkNotificationRead(item.id, div);
        }
        if (item.targetUrl) {
            window.location.href = item.targetUrl;
        }
    });

    return div;
}

function handleReceiveNotification(notification) {
    const container = document.getElementById('notificationListItems');
    if (container) {
        const emptyState = container.querySelector('.bi-bell-slash');
        if (emptyState) container.innerHTML = '';

        const elem = createNotificationElement(notification);
        container.prepend(elem);
    }

    const bellIcon = document.getElementById('navBellIcon');
    if (bellIcon) {
        bellIcon.classList.add('bell-ring');
        setTimeout(() => bellIcon.classList.remove('bell-ring'), 800);
    }

    showNotificationToast(notification);
}

function showNotificationToast(item) {
    const toastContainer = document.getElementById('notificationToastContainer');
    if (!toastContainer) return;

    const toastId = 'toast_' + Date.now();
    const toastEl = document.createElement('div');
    toastEl.id = toastId;
    toastEl.className = 'toast align-items-center border-0 shadow-lg rounded-4 overflow-hidden mb-2';
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');

    toastEl.innerHTML = `
        <div class="toast-header bg-body-tertiary border-0 py-2.5 px-3">
            <i class="bi ${item.iconClass || 'bi-bell-fill'} ${item.colorClass || 'text-primary'} me-2 fs-6"></i>
            <strong class="me-auto small fw-bold">${escapeHtml(item.title)}</strong>
            <small class="text-muted">Vừa xong</small>
            <button type="button" class="btn-close ms-2" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body bg-body py-2.5 px-3">
            <p class="small mb-1 text-body-secondary">${escapeHtml(item.message)}</p>
            ${item.targetUrl ? `<a href="${item.targetUrl}" class="btn btn-sm btn-primary rounded-pill px-2.5 py-1 small fw-semibold text-decoration-none mt-1 d-inline-block">Xem chi tiết <i class="bi bi-arrow-right"></i></a>` : ''}
        </div>
    `;

    toastContainer.appendChild(toastEl);
    if (window.bootstrap && bootstrap.Toast) {
        const bsToast = new bootstrap.Toast(toastEl, { delay: 6000 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }
}

async function handleMarkNotificationRead(id, element) {
    const token = getCsrfToken();
    try {
        const response = await fetch(`/Notification/MarkAsRead?id=${id}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/x-www-form-urlencoded'
            }
        });
        if (response.ok) {
            if (element) {
                element.classList.remove('unread');
                const dot = element.querySelector('.notification-unread-dot');
                if (dot) dot.remove();
            }
            const remaining = document.querySelectorAll('#notificationListItems .notification-item.unread').length;
            updateNotificationBadges(remaining);
        }
    } catch (err) {
        console.error('Error marking notification as read:', err);
    }
}

async function handleMarkAllNotificationsRead() {
    const token = getCsrfToken();
    try {
        const response = await fetch('/Notification/MarkAllAsRead', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/x-www-form-urlencoded'
            }
        });
        if (response.ok) {
            document.querySelectorAll('#notificationListItems .notification-item.unread').forEach(el => {
                el.classList.remove('unread');
                const dot = el.querySelector('.notification-unread-dot');
                if (dot) dot.remove();
            });
            updateNotificationBadges(0);
        }
    } catch (err) {
        console.error('Error marking all notifications as read:', err);
    }
}

function handleReceiveActivity(activity) {
    const timeline = document.getElementById('activityFeedTimeline');
    if (!timeline) return;

    const initials = activity.userDisplayName ? activity.userDisplayName.substring(0, 1).toUpperCase() : 'U';

    const card = document.createElement('div');
    card.className = 'card border rounded-4 shadow-sm p-3 p-md-3.5 activity-feed-card transition-hover just-added';
    card.setAttribute('data-activity-id', activity.id);

    card.innerHTML = `
        <div class="d-flex align-items-start gap-3">
            <div class="position-relative flex-shrink-0">
                <div class="rounded-circle bg-primary bg-opacity-10 text-primary fw-bold d-flex align-items-center justify-content-center fs-5 shadow-xs"
                     style="width: 48px; height: 48px;">
                    ${initials}
                </div>
                <span class="position-absolute bottom-0 end-0 translate-middle-y badge rounded-circle bg-${activity.typeBadgeColor || 'primary'} text-white p-1 d-flex align-items-center justify-content-center"
                      style="width: 20px; height: 20px; font-size: 0.65rem;" title="${escapeHtml(activity.typeLabel || '')}">
                    <i class="bi ${activity.iconClass || 'bi-activity'}"></i>
                </span>
            </div>
            <div class="flex-grow-1 min-w-0">
                <div class="d-flex align-items-center justify-content-between gap-2 flex-wrap mb-1">
                    <div class="d-flex align-items-center gap-1.5 flex-wrap">
                        <span class="fw-bold text-body-emphasis">${escapeHtml(activity.userDisplayName)}</span>
                        <span class="text-body-secondary small">${escapeHtml(activity.title)}</span>
                        <span class="badge bg-${activity.typeBadgeColor || 'primary'} bg-opacity-10 text-${activity.typeBadgeColor || 'primary'} rounded-pill px-2 py-0.5 small fw-semibold">
                            ${escapeHtml(activity.typeLabel || '')}
                        </span>
                    </div>
                    <span class="text-muted small text-nowrap">
                        <i class="bi bi-clock me-1"></i>Vừa xong
                    </span>
                </div>
                <div class="mt-1">
                    ${activity.targetUrl
                        ? `<a href="${activity.targetUrl}" class="text-decoration-none fw-semibold text-primary hover-underline">${escapeHtml(activity.description)} <i class="bi bi-arrow-up-right small"></i></a>`
                        : `<span class="text-body-secondary fw-medium">${escapeHtml(activity.description)}</span>`
                    }
                </div>
            </div>
            ${activity.xpEarned > 0 ? `
                <div class="flex-shrink-0 text-end">
                    <span class="badge bg-warning bg-opacity-20 text-dark border border-warning border-opacity-50 rounded-pill px-2.5 py-1.5 fw-bold d-inline-flex align-items-center gap-1">
                        <i class="bi bi-lightning-charge-fill text-danger"></i> +${activity.xpEarned} XP
                    </span>
                </div>
            ` : ''}
        </div>
    `;

    timeline.prepend(card);
}

function getCsrfToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
