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

    // PWA & Offline Network System
    initPwaAndServiceWorker();

    // Command Palette (Ctrl + K) & Quick Jump
    initCommandPalette();

    // Offline Lesson Reading Saver
    initOfflineLessonSaver();

    // Lesson Keyboard Navigation (Alt + Arrow Left/Right)
    initLessonShortcuts();
});

function initNavDropdownHover() {
    const navDropdowns = document.querySelectorAll('.navbar-nav .dropdown');
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

function isCurrentCultureEnglish() {
    return document.documentElement.lang === 'en' ||
           localStorage.getItem('nettutos-culture') === 'en' ||
           document.cookie.includes('uic=en') ||
           document.cookie.includes('c=en');
}

function updateStreakUI(data) {
    const isEn = isCurrentCultureEnglish();

    const navStreakCount = document.getElementById('navStreakCountText');
    const btnNavStreak = document.getElementById('btnNavStreak');
    const modalStreakCount = document.getElementById('modalStreakCount');
    const modalStreakDaysLabel = document.getElementById('modalStreakDaysLabel');
    const modalStreakStatus = document.getElementById('modalStreakStatusText');
    const btnDoCheckIn = document.getElementById('btnDoCheckIn');
    const btnCheckInText = document.getElementById('btnCheckInText');
    const checkInIcon = document.getElementById('checkInIcon');
    const checkInSubtext = document.getElementById('checkInSubtext');
    const totalStreakXpText = document.getElementById('totalStreakXpText');
    const modalPast7DaysTitle = document.getElementById('modalPast7DaysTitle');
    const past7DaysContainer = document.getElementById('past7DaysContainer');

    // Update navbar badge
    if (navStreakCount) {
        const dayLabel = isEn ? (data.currentStreak === 1 ? 'day' : 'days') : 'ngày';
        navStreakCount.textContent = `${data.currentStreak} ${dayLabel}`;
    }
    if (btnNavStreak) {
        const dayWord = isEn ? (data.currentStreak === 1 ? 'day' : 'days') : 'ngày';
        if (data.hasCheckedInToday) {
            btnNavStreak.className = 'btn btn-sm btn-warning text-dark rounded-pill px-3 py-1.5 d-flex align-items-center gap-2 text-nowrap fw-bold shadow-sm';
            btnNavStreak.title = isEn 
                ? `${data.currentStreak} ${dayWord} streak (Checked in today)` 
                : `Chuỗi ${data.currentStreak} ngày (Hôm nay đã điểm danh)`;
        } else {
            btnNavStreak.className = 'btn btn-sm btn-outline-warning rounded-pill px-3 py-1.5 d-flex align-items-center gap-2 text-nowrap fw-bold shadow-none';
            btnNavStreak.title = isEn 
                ? `${data.currentStreak} ${dayWord} streak (Click to check in today)` 
                : `Chuỗi ${data.currentStreak} ngày (Bấm để điểm danh hôm nay)`;
        }
    }

    // Update Modal
    if (modalStreakCount) {
        modalStreakCount.textContent = data.currentStreak;
    }
    if (modalStreakDaysLabel) {
        modalStreakDaysLabel.textContent = isEn ? 'Consecutive Days' : 'Ngày Liên Tục';
    }
    if (modalStreakStatus) {
        const streakDaysEn = `${data.currentStreak} ${data.currentStreak === 1 ? 'day' : 'days'}`;
        const recordDaysEn = `${data.longestStreak} ${data.longestStreak === 1 ? 'day' : 'days'}`;
        if (data.hasCheckedInToday) {
            modalStreakStatus.innerHTML = isEn
                ? `🔥 You have checked in today! Streak of <strong>${streakDaysEn}</strong> continues to burn strong. Your record: <strong>${recordDaysEn}</strong>.`
                : `🔥 Bạn đã điểm danh hôm nay! Chuỗi <strong>${data.currentStreak} ngày</strong> tiếp tục bùng cháy. Kỷ lục của bạn: <strong>${data.longestStreak} ngày</strong>.`;
        } else if (data.currentStreak > 0) {
            modalStreakStatus.innerHTML = isEn
                ? `⚠️ Check in today to maintain your <strong>${streakDaysEn}</strong> streak and receive <strong>+${data.nextRewardXp} XP</strong>!`
                : `⚠️ Hãy điểm danh hôm nay để duy trì chuỗi <strong>${data.currentStreak} ngày</strong> và nhận ngay <strong>+${data.nextRewardXp} XP</strong>!`;
        } else {
            modalStreakStatus.innerHTML = isEn
                ? `Start building your daily study streak today and receive <strong>+${data.nextRewardXp} XP</strong>!`
                : `Bắt đầu xây dựng chuỗi học tập ngay hôm nay và nhận ngay <strong>+${data.nextRewardXp} XP</strong>!`;
        }
    }

    if (btnDoCheckIn && btnCheckInText) {
        if (data.hasCheckedInToday) {
            btnDoCheckIn.disabled = true;
            btnDoCheckIn.className = 'btn btn-success btn-lg rounded-pill fw-bold px-4 py-2.5 shadow-sm w-100 d-inline-flex align-items-center justify-content-center gap-2 text-white';
            btnCheckInText.textContent = isEn ? '✓ Checked in today!' : '✓ Đã điểm danh hôm nay!';
            if (checkInIcon) checkInIcon.className = 'bi bi-check-circle-fill fs-5 text-white me-1';
        } else {
            btnDoCheckIn.disabled = false;
            btnDoCheckIn.className = 'btn btn-warning btn-lg rounded-pill fw-bold px-4 py-2.5 shadow w-100 d-inline-flex align-items-center justify-content-center gap-2 text-dark';
            btnCheckInText.textContent = isEn ? `Check in today (+${data.nextRewardXp} XP)` : `Điểm danh hôm nay (+${data.nextRewardXp} XP)`;
            if (checkInIcon) checkInIcon.className = 'bi bi-lightning-charge-fill text-danger fs-5 me-1';
        }
    }

    if (checkInSubtext) {
        checkInSubtext.innerHTML = `<i class="bi bi-clock-history me-2"></i>` + (isEn ? 'Log in and check in daily to accumulate XP' : 'Mỗi ngày đăng nhập và điểm danh để tích lũy XP');
    }

    if (modalPast7DaysTitle) {
        modalPast7DaysTitle.textContent = isEn ? 'Past 7 days progress' : 'Tiến độ 7 ngày gần nhất';
    }

    if (totalStreakXpText) {
        totalStreakXpText.textContent = isEn ? `+${data.totalXpEarnedFromStreaks} XP accumulated` : `+${data.totalXpEarnedFromStreaks} XP đã tích lũy`;
    }

    // Render Past 7 Days Progress
    if (past7DaysContainer && data.past7Days && data.past7Days.length > 0) {
        past7DaysContainer.innerHTML = '';
        const viToEnDays = { 'T2': 'Mon', 'T3': 'Tue', 'T4': 'Wed', 'T5': 'Thu', 'T6': 'Fri', 'T7': 'Sat', 'CN': 'Sun' };

        data.past7Days.forEach(day => {
            const dayCol = document.createElement('div');
            dayCol.className = 'text-center flex-fill';

            const rawDayName = day.dayName || '';
            const dayDisplayName = isEn ? (day.dayNameEn || viToEnDays[rawDayName] || rawDayName) : rawDayName;

            let circleClass = 'day-tracker-circle unchecked mx-auto mb-1';
            let circleContent = dayDisplayName;

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
                <div class="small fw-semibold text-muted" style="font-size: 0.72rem;">${escapeHtml(dayDisplayName)}</div>
            `;
            past7DaysContainer.appendChild(dayCol);
        });
    }
}

async function handleDailyCheckIn() {
    const isEn = isCurrentCultureEnglish();
    const btnDoCheckIn = document.getElementById('btnDoCheckIn');
    const btnCheckInText = document.getElementById('btnCheckInText');
    const checkInSpinner = document.getElementById('checkInSpinner');
    const checkInIcon = document.getElementById('checkInIcon');

    if (!btnDoCheckIn || btnDoCheckIn.disabled) return;

    btnDoCheckIn.disabled = true;
    if (checkInSpinner) checkInSpinner.classList.remove('d-none');
    if (checkInIcon) checkInIcon.classList.add('d-none');
    if (btnCheckInText) btnCheckInText.textContent = isEn ? 'Recording...' : 'Đang ghi nhận...';

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
            const badges = isEn && result.newlyUnlockedBadgesEn && result.newlyUnlockedBadgesEn.length > 0
                ? result.newlyUnlockedBadgesEn
                : (result.newlyUnlockedBadges || []);

            if (badges.length > 0) {
                const title = isEn ? '🎉 CONGRATULATIONS!' : '🎉 CHÚC MỪNG!';
                const ach = isEn ? '🏆 NEW ACHIEVEMENT:' : '🏆 THÀNH TỰU MỚI:';
                const msg = isEn ? (result.messageEn || result.message) : result.message;
                alert(`${title}\n${msg}\n\n${ach}\n${badges.join('\n')}`);
            }
        } else {
            const failMsg = isEn 
                ? (result.messageEn || 'Unable to check in right now. Please try again later.') 
                : (result.message || 'Không thể điểm danh lúc này, vui lòng thử lại sau.');
            alert(failMsg);
            if (btnDoCheckIn) btnDoCheckIn.disabled = false;
        }
    } catch (err) {
        console.error('Error during check-in:', err);
        alert(isEn ? 'An error occurred during check-in. Please verify your connection.' : 'Đã có lỗi xảy ra trong quá trình điểm danh. Vui lòng kiểm tra lại kết nối mạng.');
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
    const isEn = isCurrentCultureEnglish();
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
            unreadPill.textContent = `${count} ${isEn ? 'new' : 'mới'}`;
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
    const isEn = isCurrentCultureEnglish();
    const container = document.getElementById('notificationListItems');
    if (!container) return;

    if (!items || items.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5 text-muted px-3">
                <i class="bi bi-bell-slash fs-1 d-block mb-2 text-secondary opacity-50"></i>
                <div class="small fw-semibold">${isEn ? 'You have no new notifications' : 'Bạn không có thông báo mới nào'}</div>
                <div class="text-muted" style="font-size: 0.78rem;">${isEn ? 'Learning activities will appear here' : 'Các hoạt động học tập sẽ hiển thị tại đây'}</div>
            </div>
        `;
        return;
    }

    container.innerHTML = '';
    items.forEach(item => {
        container.appendChild(createNotificationElement(item));
    });
}

function getLocalizedNotification(item) {
    const isEn = isCurrentCultureEnglish();
    if (!isEn) {
        return {
            title: item.title,
            message: item.message,
            typeName: item.typeName || '',
            timeAgo: item.timeAgo || 'Vừa xong'
        };
    }

    let title = item.titleEn || item.title || '';
    let message = item.messageEn || item.message || '';

    // Robust client-side translation fallback if server passed un-translated string
    if (title.includes('Điểm danh nhận thưởng thành công')) title = 'Check-in Reward Claimed! 🔥';
    else if (title.includes('Huy hiệu mới đã mở khóa')) title = 'New Badge Unlocked! 🏆';
    else if (title.includes('Hoàn thành bài học') || title.includes('hoàn thành bài học')) title = 'Lesson Completed! 🎉';
    else if (title.includes('kỳ thi tốt nghiệp') || title.includes('thi tốt nghiệp')) title = 'Graduation Exam Passed! 🎓';
    else if (title.includes('Chứng chỉ')) title = 'Digital Certificate Ready! 📜';
    else if (title.includes('thử thách C#') || title.includes('thử thách')) title = 'C# Challenge Solved! ⚡';
    else if (title.includes('trả lời')) title = 'New Reply to Discussion';
    else if (title.includes('giải pháp')) title = 'Accepted Solution! 🌟';
    else if (title.includes('đồ án')) title = 'Capstone Project Update 🚀';

    // Streak message
    const streakMatch = message.match(/(?:chuỗi|streak)\s*(\d+)\s*(?:ngày|days?)?\s*(?:liên tiếp|consecutive)?\s*(?:\(\+?(\d+)\s*XP\))?/i);
    if (streakMatch && streakMatch[1]) {
        const days = streakMatch[1];
        const xp = streakMatch[2] || '';
        const dayLabel = days === '1' ? 'day' : 'days';
        message = xp ? `You maintained a streak of ${days} consecutive ${dayLabel} (+${xp} XP)!` : `You maintained a streak of ${days} consecutive ${dayLabel}!`;
    } else if (message.includes('Mở khóa huy hiệu:') || message.includes('Huy hiệu')) {
        message = message
            .replace(/Mở khóa huy hiệu:\s*/g, 'Badge unlocked: ')
            .replace(/Mở khóa huy hiệu\s*/g, 'Badge unlocked: ')
            .replace(/XP thưởng/g, 'bonus XP')
            .replace(/thưởng XP/g, 'bonus XP')
            .replace(/Ngọn Lửa Bền Bỉ/g, 'Persistent Flame')
            .replace(/Chiến Binh Kỷ Luật/g, 'Disciplined Warrior')
            .replace(/Huyền Thoại Bất Bại/g, 'Unstoppable Legend')
            .replace(/Thuật Toán Săn Bàn/g, 'Algorithm Hunter')
            .replace(/Vua Thuật Toán/g, 'Algorithm Master')
            .replace(/Kỹ Sư .NET/g, 'Certified Engineer')
            .replace(/Học Giả Chăm Chỉ/g, 'Diligent Scholar')
            .replace(/Tân Binh .NET/g, 'NET Rookie')
            .replace(/Người Hùng Cộng Đồng/g, 'Community Torchbearer')
            .replace(/Kiến Trúc Sư .NET/g, '.NET Architect');
    } else if (message.includes('hoàn thành bài học')) {
        const lessonMatch = message.match(/(?:Chúc mừng bạn đã hoàn thành bài học|Bạn đã hoàn thành bài học)\s*'([^']+)'(?:\s*\(\+?(\d+)\s*XP\))?/i);
        if (lessonMatch) {
            let lessonTitle = lessonMatch[1]
                .replace('Bài 1:', 'Lesson 1:')
                .replace('Bài 2:', 'Lesson 2:')
                .replace('Bài 3:', 'Lesson 3:')
                .replace('Bài 4:', 'Lesson 4:')
                .replace('Bài 5:', 'Lesson 5:')
                .replace('Tổng quan hệ sinh thái .NET & Cài đặt môi trường', '.NET Ecosystem Overview & Environment Setup')
                .replace('Cú pháp C# căn bản - Biến, Kiểu dữ liệu & Điều khiển luồng', 'Core C# Syntax - Variables, Data Types & Control Flow');
            const xp = lessonMatch[2] ? `(+${lessonMatch[2]} XP)` : '(+20 XP)';
            message = `You completed lesson '${lessonTitle}' ${xp}.`;
        }
    }

    return {
        title: title,
        message: message,
        typeName: item.typeNameEn || item.typeName || '',
        timeAgo: item.timeAgoEn || item.timeAgo || 'Just now'
    };
}

function createNotificationElement(item) {
    const isEn = isCurrentCultureEnglish();
    const loc = getLocalizedNotification(item);
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
                    <span class="fw-bold small text-truncate text-body-emphasis">${escapeHtml(loc.title)}</span>
                    ${!item.isRead ? `<span class="notification-unread-dot ms-1" title="${isEn ? 'Unread' : 'Chưa đọc'}"></span>` : ''}
                </div>
                <p class="text-body-secondary small mb-1 lh-sm" style="font-size: 0.8rem;">
                    ${escapeHtml(loc.message)}
                </p>
                <div class="d-flex align-items-center gap-2" style="font-size: 0.72rem;">
                    <span class="text-muted"><i class="bi bi-clock me-1"></i>${escapeHtml(loc.timeAgo)}</span>
                    <span class="badge bg-secondary bg-opacity-10 text-secondary border py-0 px-1.5">${escapeHtml(loc.typeName)}</span>
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

    const loc = getLocalizedNotification(item);
    const isEn = isCurrentCultureEnglish();

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
            <strong class="me-auto small fw-bold">${escapeHtml(loc.title)}</strong>
            <small class="text-muted">${escapeHtml(loc.timeAgo || (isEn ? 'Just now' : 'Vừa xong'))}</small>
            <button type="button" class="btn-close ms-2" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body bg-body py-2.5 px-3">
            <p class="small mb-1 text-body-secondary">${escapeHtml(loc.message)}</p>
            ${item.targetUrl ? `<a href="${item.targetUrl}" class="btn btn-sm btn-primary rounded-pill px-2.5 py-1 small fw-semibold text-decoration-none mt-1 d-inline-block">${isEn ? 'View details' : 'Xem chi tiết'} <i class="bi bi-arrow-right"></i></a>` : ''}
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

    const isEn = isCurrentCultureEnglish();
    const title = isEn ? (activity.titleEn || activity.title) : activity.title;
    const typeLabel = isEn ? (activity.typeLabelEn || activity.typeLabel) : activity.typeLabel;
    const desc = isEn ? (activity.descriptionEn || activity.description) : activity.description;
    const timeAgo = isEn ? (activity.timeAgoEn || 'Just now') : (activity.timeAgo || 'Vừa xong');

    card.innerHTML = `
        <div class="d-flex align-items-start gap-3">
            <div class="position-relative flex-shrink-0">
                <div class="rounded-circle bg-primary bg-opacity-10 text-primary fw-bold d-flex align-items-center justify-content-center fs-5 shadow-xs"
                     style="width: 48px; height: 48px;">
                    ${initials}
                </div>
            </div>
            <div class="flex-grow-1 min-w-0">
                <div class="d-flex align-items-center justify-content-between gap-2 flex-wrap mb-1">
                    <div class="d-flex align-items-center gap-2 flex-wrap">
                        <span class="fw-bold text-body-emphasis">${escapeHtml(activity.userDisplayName)}</span>
                        <span class="text-body-secondary small">${escapeHtml(title)}</span>
                        <span class="badge bg-${activity.typeBadgeColor || 'primary'} bg-opacity-10 text-${activity.typeBadgeColor || 'primary'} rounded-pill px-2 py-0.5 small fw-semibold">
                            ${escapeHtml(typeLabel || '')}
                        </span>
                    </div>
                    <span class="text-muted small text-nowrap">
                        <i class="bi bi-clock me-1"></i>${escapeHtml(timeAgo)}
                    </span>
                </div>
                <div class="mt-1">
                    ${activity.targetUrl
                        ? `<a href="${activity.targetUrl}" class="text-decoration-none fw-semibold text-primary hover-underline">${escapeHtml(desc)} <i class="bi bi-arrow-up-right small"></i></a>`
                        : `<span class="text-body-secondary fw-medium">${escapeHtml(desc)}</span>`
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

/* ==========================================================================
   PWA & Service Worker Offline Mode
   ========================================================================== */
let deferredPwaPrompt = null;

function initPwaAndServiceWorker() {
    const toast = document.getElementById('networkStatusToast');
    const toastTitle = document.getElementById('networkStatusTitle');
    const toastDesc = document.getElementById('networkStatusDesc');
    const toastIcon = document.getElementById('networkStatusIcon');
    const isEn = isCurrentCultureEnglish();

    function showNetworkToast(isOnline) {
        if (!toast) return;
        if (isOnline) {
            toast.className = 'network-status-toast online';
            if (toastIcon) toastIcon.className = 'bi bi-wifi fs-5';
            if (toastTitle) toastTitle.textContent = isEn ? 'Back Online' : 'Đã có kết nối Internet';
            if (toastDesc) toastDesc.textContent = isEn ? 'Connected to server' : 'Đã khôi phục kết nối máy chủ';
            setTimeout(() => {
                toast.className = 'network-status-toast';
            }, 3500);
        } else {
            toast.className = 'network-status-toast offline';
            if (toastIcon) toastIcon.className = 'bi bi-wifi-off fs-5';
            if (toastTitle) toastTitle.textContent = isEn ? 'Offline Mode' : 'Chế độ Ngoại tuyến';
            if (toastDesc) toastDesc.textContent = isEn ? 'Viewing cached content' : 'Đang dùng dữ liệu lưu trữ đệm';
        }
    }

    window.addEventListener('online', () => showNetworkToast(true));
    window.addEventListener('offline', () => showNetworkToast(false));

    if (!navigator.onLine) {
        showNetworkToast(false);
    }

    // Register Service Worker
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js')
                .then(reg => {
                    reg.onupdatefound = () => {
                        const installingWorker = reg.installing;
                        if (installingWorker) {
                            installingWorker.onstatechange = () => {
                                if (installingWorker.state === 'installed' && navigator.serviceWorker.controller) {
                                    console.log('NET-Tutos: Service Worker updated.');
                                }
                            };
                        }
                    };
                })
                .catch(err => {
                    console.warn('ServiceWorker registration error:', err);
                });
        });
    }

    // Capture PWA install prompt
    window.addEventListener('beforeinstallprompt', e => {
        e.preventDefault();
        deferredPwaPrompt = e;
    });
}

/* ==========================================================================
   Command Palette (Ctrl + K) & Quick Jump
   ========================================================================== */
let commandPaletteItems = [];
let activePaletteIndex = -1;
let currentPaletteFilter = 'all';

function openCommandPaletteModal(event) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    const modalEl = document.getElementById('commandPaletteModal');
    if (!modalEl) return;
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    setTimeout(() => {
        const input = document.getElementById('commandPaletteInput');
        if (input) {
            input.focus();
            input.select();
        }
        renderCommandPaletteResults('');
    }, 150);
}

function initCommandPalette() {
    // Load tutorials search data
    const dataEl = document.getElementById('commandPaletteData');
    if (dataEl && dataEl.textContent) {
        try {
            commandPaletteItems = JSON.parse(dataEl.textContent);
        } catch (e) {
            commandPaletteItems = [];
        }
    }

    // Global keyboard shortcut Ctrl + K / Cmd + K
    window.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            openCommandPaletteModal();
        }
    });

    const input = document.getElementById('commandPaletteInput');
    if (input) {
        input.addEventListener('input', (e) => {
            renderCommandPaletteResults(e.target.value.trim());
        });

        input.addEventListener('keydown', (e) => {
            const resultsContainer = document.getElementById('commandPaletteResults');
            if (!resultsContainer) return;
            const items = resultsContainer.querySelectorAll('.command-palette-item');
            if (!items.length) return;

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                activePaletteIndex = (activePaletteIndex + 1) % items.length;
                updatePaletteHighlight(items);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                activePaletteIndex = (activePaletteIndex - 1 + items.length) % items.length;
                updatePaletteHighlight(items);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (activePaletteIndex >= 0 && items[activePaletteIndex]) {
                    items[activePaletteIndex].click();
                }
            }
        });
    }

    // Filter tags click
    document.querySelectorAll('.cp-filter-tag').forEach(tag => {
        tag.addEventListener('click', () => {
            document.querySelectorAll('.cp-filter-tag').forEach(t => {
                t.className = 'badge rounded-pill bg-body-secondary text-body border-0 cp-filter-tag';
            });
            tag.className = 'badge rounded-pill bg-primary text-white border-0 cp-filter-tag';
            currentPaletteFilter = tag.getAttribute('data-filter') || 'all';
            const query = input ? input.value.trim() : '';
            renderCommandPaletteResults(query);
        });
    });
}

function updatePaletteHighlight(items) {
    items.forEach((item, idx) => {
        if (idx === activePaletteIndex) {
            item.classList.add('active');
            item.scrollIntoView({ block: 'nearest' });
        } else {
            item.classList.remove('active');
        }
    });
}

function renderCommandPaletteResults(query) {
    const container = document.getElementById('commandPaletteResults');
    if (!container) return;
    const isEn = isCurrentCultureEnglish();
    const q = query.toLowerCase();
    activePaletteIndex = 0;

    // Static Navigation & Actions items
    const quickActions = [
        {
            type: 'action',
            icon: 'bi-moon-stars-fill text-warning',
            title: isEn ? 'Toggle Theme (Dark / Light)' : 'Chuyển giao diện Sáng / Tối',
            subtitle: isEn ? 'Switch appearance mode' : 'Đổi chế độ màu hệ thống',
            action: () => {
                const themeBtn = document.getElementById('themeToggleBtn');
                if (themeBtn) themeBtn.click();
                const modalEl = document.getElementById('commandPaletteModal');
                if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
            }
        },
        {
            type: 'nav',
            icon: 'bi-cpu-fill text-danger',
            title: isEn ? 'C# Code Playground' : 'Trình thực hành C# Playground',
            subtitle: isEn ? 'Compile & run C# in browser' : 'Viết và chạy mã C# trực tuyến (.NET 10)',
            url: '/Playground'
        },
        {
            type: 'nav',
            icon: 'bi-robot text-info',
            title: isEn ? 'AI .NET Tutor & Mentor' : 'Trợ lý AI Gia sư .NET',
            subtitle: isEn ? 'Ask AI about C#, error diagnostics' : 'Hỏi đáp lập trình, sửa lỗi và hướng dẫn học',
            url: '/AiTutor'
        },
        {
            type: 'nav',
            icon: 'bi-collection-play-fill text-danger',
            title: isEn ? 'Full Curriculum (101 Lessons)' : 'Chương trình C# (101 bài học)',
            subtitle: isEn ? 'Standard Microsoft .NET syllabus' : 'Lộ trình 7 module chuẩn Microsoft .NET',
            url: '/Curriculum'
        },
        {
            type: 'nav',
            icon: 'bi-signpost-split text-warning',
            title: isEn ? 'Standard Roadmap' : 'Lộ trình học chuẩn .NET',
            subtitle: isEn ? 'From Zero to Senior .NET Developer' : 'Bản đồ từ Zero đến Senior .NET Developer',
            url: '/Roadmap'
        },
        {
            type: 'nav',
            icon: 'bi-code-square text-info',
            title: isEn ? 'C# Cheatsheet' : 'Tra cứu cú pháp C# Cheatsheet',
            subtitle: isEn ? 'Quick syntax guide & samples' : 'Sổ tay cú pháp & ví dụ mã mẫu',
            url: '/CheatSheet'
        },
        {
            type: 'nav',
            icon: 'bi-trophy-fill text-warning',
            title: isEn ? 'Hall of Fame (Leaderboard)' : 'Bảng vinh danh (Leaderboard)',
            subtitle: isEn ? 'Top learners & XP ranking' : 'Bảng xếp hạng học viên xuất sắc',
            url: '/Leaderboard'
        },
        {
            type: 'nav',
            icon: 'bi-calendar2-check-fill text-success',
            title: isEn ? 'Study Planner & Streak' : 'Kế hoạch học tập & Mục tiêu',
            subtitle: isEn ? 'Track goals and streaks' : 'Theo dõi mục tiêu & duy trì streak',
            url: '/Planner'
        }
    ];

    let html = '';

    // Filter quick actions
    const filteredActions = quickActions.filter(item => {
        if (currentPaletteFilter === 'lesson') return false;
        if (currentPaletteFilter === 'nav' && item.type !== 'nav') return false;
        if (currentPaletteFilter === 'action' && item.type !== 'action') return false;
        if (!q) return true;
        return item.title.toLowerCase().includes(q) || item.subtitle.toLowerCase().includes(q);
    });

    if (filteredActions.length > 0) {
        html += `<div class="command-palette-group-header">${isEn ? 'Quick Navigation & Actions' : 'Điều hướng & Tác vụ'}</div>`;
        filteredActions.forEach(item => {
            const clickAttr = item.url 
                ? `onclick="window.location.href='${item.url}'"` 
                : `onclick="window.__triggerPaletteAction('${item.title}')"`;
            html += `
                <div class="command-palette-item" ${clickAttr}>
                    <div class="item-icon bg-body-tertiary">
                        <i class="bi ${item.icon}"></i>
                    </div>
                    <div class="flex-grow-1 min-w-0">
                        <div class="fw-semibold text-truncate">${escapeHtml(item.title)}</div>
                        <div class="small text-muted text-truncate">${escapeHtml(item.subtitle)}</div>
                    </div>
                    <i class="bi bi-arrow-return-left text-muted small opacity-50"></i>
                </div>
            `;
        });
    }

    // Filter lessons
    if (currentPaletteFilter === 'all' || currentPaletteFilter === 'lesson') {
        const filteredLessons = commandPaletteItems.filter(item => {
            if (!q) return true;
            return (item.title && item.title.toLowerCase().includes(q)) ||
                   (item.category && item.category.toLowerCase().includes(q)) ||
                   (item.slug && item.slug.toLowerCase().includes(q));
        }).slice(0, 15);

        if (filteredLessons.length > 0) {
            html += `<div class="command-palette-group-header">${isEn ? 'Lessons & Tutorials' : 'Bài học & Hướng dẫn'}</div>`;
            filteredLessons.forEach(item => {
                html += `
                    <div class="command-palette-item" onclick="window.location.href='/Tutorials/Details/${encodeURIComponent(item.slug)}'">
                        <div class="item-icon bg-primary-subtle text-primary">
                            <i class="bi bi-book"></i>
                        </div>
                        <div class="flex-grow-1 min-w-0">
                            <div class="fw-semibold text-truncate">${escapeHtml(item.title)}</div>
                            <div class="small text-muted text-truncate">
                                <span class="badge bg-secondary-subtle text-secondary me-1 py-0.5">${escapeHtml(item.category)}</span>
                                <span>~${item.readingMinutes} ${isEn ? 'min' : 'phút'}</span>
                            </div>
                        </div>
                        <i class="bi bi-arrow-return-left text-muted small opacity-50"></i>
                    </div>
                `;
            });
        }
    }

    if (!html) {
        html = `
            <div class="p-4 text-center text-muted">
                <i class="bi bi-search fs-3 d-block mb-2 text-secondary opacity-50"></i>
                <div>${isEn ? 'No results found for' : 'Không tìm thấy kết quả phù hợp cho'} "${escapeHtml(query)}"</div>
                <small class="text-secondary">${isEn ? 'Try searching with another keyword.' : 'Hãy thử lại bằng từ khóa khác.'}</small>
            </div>
        `;
    }

    container.innerHTML = html;

    window.__triggerPaletteAction = function(title) {
        const found = quickActions.find(a => a.title === title);
        if (found && found.action) found.action();
    };

    const firstItem = container.querySelector('.command-palette-item');
    if (firstItem) firstItem.classList.add('active');
}

/* ==========================================================================
   Offline Lesson Reading Saver
   ========================================================================== */
function initOfflineLessonSaver() {
    const btn = document.getElementById('btnSaveOffline');
    if (!btn) return;
    const isEn = isCurrentCultureEnglish();
    const currentUrl = window.location.href;

    const icon = document.getElementById('offlineSaveIcon');
    const text = document.getElementById('offlineSaveText');

    let isSaved = false;

    function setBtnSavedState(saved) {
        isSaved = saved;
        if (saved) {
            btn.className = 'btn btn-sm btn-success rounded-pill px-2.5 py-1 d-inline-flex align-items-center gap-1.5 shadow-none';
            if (icon) icon.className = 'bi bi-check2-circle';
            if (text) text.textContent = isEn ? 'Saved Offline' : 'Đã lưu ngoại tuyến';
            btn.title = isEn ? 'Saved in cache! Click to remove' : 'Đã lưu trong bộ nhớ đệm! Bấm để xóa';
        } else {
            btn.className = 'btn btn-sm btn-outline-secondary rounded-pill px-2.5 py-1 d-inline-flex align-items-center gap-1.5 shadow-none';
            if (icon) icon.className = 'bi bi-cloud-arrow-down';
            if (text) text.textContent = isEn ? 'Save Offline' : 'Lưu ngoại tuyến';
            btn.title = isEn ? 'Save offline for reading without Internet' : 'Lưu đọc ngoại tuyến khi không có mạng';
        }
    }

    // Check offline status via Service Worker message
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        navigator.serviceWorker.controller.postMessage({
            type: 'CHECK_OFFLINE_SAVED',
            url: currentUrl
        });

        navigator.serviceWorker.addEventListener('message', (event) => {
            const data = event.data;
            if (!data) return;

            if (data.type === 'CHECK_OFFLINE_RESULT' && data.url === currentUrl) {
                setBtnSavedState(data.isSaved);
            } else if (data.type === 'LESSON_SAVED_SUCCESS' && data.url === currentUrl) {
                setBtnSavedState(true);
                showGenericNotificationToast(
                    isEn ? 'Offline Lesson Ready' : 'Đã Lưu Ngoại Tuyến Thành Công',
                    isEn ? 'You can now read this lesson without an Internet connection.' : 'Bạn có thể đọc lại bài học này bất kỳ lúc nào mà không cần kết nối mạng.',
                    'success'
                );
            } else if (data.type === 'LESSON_REMOVED_SUCCESS' && data.url === currentUrl) {
                setBtnSavedState(false);
                showGenericNotificationToast(
                    isEn ? 'Removed Offline Lesson' : 'Đã Xóa Bản Ngoại Tuyến',
                    isEn ? 'Cached copy has been removed.' : 'Bản lưu đệm ngoại tuyến của bài học đã được xóa.',
                    'secondary'
                );
            }
        });
    }

    btn.addEventListener('click', () => {
        if (!('serviceWorker' in navigator) || !navigator.serviceWorker.controller) {
            showGenericNotificationToast(
                isEn ? 'Offline Mode' : 'Chế độ Ngoại tuyến',
                isEn ? 'Service Worker is starting. Please try again in a few moments.' : 'Trình duyệt đang khởi tạo Service Worker. Vui lòng thử lại sau vài giây.',
                'info'
            );
            return;
        }

        if (isSaved) {
            navigator.serviceWorker.controller.postMessage({
                type: 'REMOVE_LESSON_OFFLINE',
                url: currentUrl
            });
        } else {
            if (icon) icon.className = 'spinner-border spinner-border-sm';
            navigator.serviceWorker.controller.postMessage({
                type: 'SAVE_LESSON_OFFLINE',
                url: currentUrl
            });
        }
    });
}

/* ==========================================================================
   Lesson Keyboard Navigation (Alt + Arrow Left / Right)
   ========================================================================== */
function initLessonShortcuts() {
    window.addEventListener('keydown', (e) => {
        const tag = document.activeElement ? document.activeElement.tagName.toLowerCase() : '';
        if (tag === 'input' || tag === 'textarea' || document.activeElement?.isContentEditable) return;

        if (e.altKey && e.key === 'ArrowLeft') {
            const prev = document.getElementById('prevLessonLink');
            if (prev) {
                e.preventDefault();
                prev.click();
            }
        } else if (e.altKey && e.key === 'ArrowRight') {
            const next = document.getElementById('nextLessonLink');
            if (next) {
                e.preventDefault();
                next.click();
            }
        }
    });
}

function showGenericNotificationToast(title, message, badgeType) {
    const container = document.getElementById('notificationToastContainer');
    if (!container) return;

    const toastId = 'toast_' + Date.now();
    const toastEl = document.createElement('div');
    toastEl.className = 'toast show border-0 shadow-lg rounded-4 overflow-hidden mb-2';
    toastEl.setAttribute('role', 'alert');
    toastEl.id = toastId;

    const bgBadge = badgeType === 'success' ? 'bg-success text-white' : (badgeType === 'secondary' ? 'bg-secondary text-white' : 'bg-primary text-white');

    toastEl.innerHTML = `
        <div class="toast-header border-0 bg-body-tertiary d-flex align-items-center justify-content-between p-2.5 px-3">
            <span class="badge ${bgBadge} rounded-pill px-2 py-0.5 me-2">NET-Tutos</span>
            <strong class="me-auto text-body">${escapeHtml(title)}</strong>
            <button type="button" class="btn-close small shadow-none" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body p-3 small text-body-secondary bg-body">
            ${escapeHtml(message)}
        </div>
    `;

    container.appendChild(toastEl);
    setTimeout(() => {
        toastEl.remove();
    }, 4500);
}
