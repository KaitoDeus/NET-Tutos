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
});

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
