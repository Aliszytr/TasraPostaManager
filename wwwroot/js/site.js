/* ==========================================================================
   TASRA POSTA MANAGER — SITE JS v3.0
   Sidebar toggle, Theme toggle, Toast system, DataTable Turkish locale
   ========================================================================== */

// ═══════════════════ SIDEBAR TOGGLE ═══════════════════
(function initSidebar() {
    document.addEventListener('DOMContentLoaded', function () {
        const sidebar = document.getElementById('tpSidebar');
        const toggleBtn = document.getElementById('sidebarToggle');
        const overlay = document.getElementById('sidebarOverlay');
        const mobileToggle = document.getElementById('mobileMenuToggle');

        if (!sidebar) return;

        // Desktop collapse/expand
        if (toggleBtn) {
            toggleBtn.addEventListener('click', function () {
                sidebar.classList.toggle('collapsed');
                // Persist state
                if (sidebar.classList.contains('collapsed')) {
                    document.cookie = 'tp-sidebar=collapsed;path=/;max-age=31536000';
                } else {
                    document.cookie = 'tp-sidebar=expanded;path=/;max-age=31536000';
                }
            });
        }

        // Mobile open/close
        if (mobileToggle) {
            mobileToggle.addEventListener('click', function () {
                sidebar.classList.toggle('mobile-open');
            });
        }
        if (overlay) {
            overlay.addEventListener('click', function () {
                sidebar.classList.remove('mobile-open');
            });
        }
    });
})();

// ═══════════════════ THEME TOGGLE ═══════════════════
(function initTheme() {
    document.addEventListener('DOMContentLoaded', function () {
        const themeToggle = document.getElementById('themeToggle');
        if (!themeToggle) return;

        themeToggle.addEventListener('click', function () {
            const currentTheme = document.documentElement.getAttribute('data-theme');
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('tp-theme', newTheme);
        });
    });
})();

// ═══════════════════ TOAST SYSTEM ═══════════════════
const ToastSystem = {
    container: null,
    init() {
        this.container = document.getElementById('toastContainer');
    },
    show(message, type = 'info', duration = 4000) {
        if (!this.container) this.init();
        if (!this.container) return;

        const icons = {
            success: 'fa-check-circle',
            danger: 'fa-exclamation-triangle',
            warning: 'fa-exclamation-circle',
            info: 'fa-info-circle'
        };
        const colors = {
            success: 'var(--color-success)',
            danger: 'var(--color-danger)',
            warning: 'var(--color-warning)',
            info: 'var(--color-info)'
        };

        const toast = document.createElement('div');
        toast.className = 'toast-notification slide-in-up';
        toast.style.cssText = `
            background: var(--bg-card);
            border: 1px solid var(--border-color);
            border-left: 4px solid ${colors[type] || colors.info};
            border-radius: var(--radius-lg);
            padding: 0.75rem 1rem;
            box-shadow: var(--shadow-lg);
            display: flex;
            align-items: center;
            gap: 0.75rem;
            min-width: 320px;
            max-width: 450px;
            font-size: 0.875rem;
            cursor: pointer;
        `;
        toast.innerHTML = `
            <i class="fas ${icons[type] || icons.info}" style="color: ${colors[type] || colors.info}; font-size: 1.1rem;"></i>
            <span style="flex: 1; color: var(--text-primary);">${message}</span>
            <i class="fas fa-times" style="color: var(--text-muted); cursor: pointer; font-size: 0.8rem;"></i>
        `;

        toast.addEventListener('click', () => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100px)';
            toast.style.transition = '0.3s ease';
            setTimeout(() => toast.remove(), 300);
        });

        this.container.appendChild(toast);

        setTimeout(() => {
            if (toast.parentElement) {
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(100px)';
                toast.style.transition = '0.3s ease';
                setTimeout(() => toast.remove(), 300);
            }
        }, duration);
    }
};

// ═══════════════════ BUTTON LOADING STATE ═══════════════════
function showLoading(button) {
    if (!button) return;
    button._originalHTML = button.innerHTML;
    button._originalDisabled = button.disabled;
    const spinnerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>İşleniyor...';
    button.innerHTML = spinnerHTML;
    button.disabled = true;
}

function hideLoading(button) {
    if (!button || !button._originalHTML) return;
    button.innerHTML = button._originalHTML;
    button.disabled = button._originalDisabled || false;
}

// ═══════════════════ PAGE OVERLAY ═══════════════════
function showPageOverlay(message) {
    const existing = document.querySelector('.page-overlay');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.className = 'page-overlay';
    overlay.innerHTML = `
        <div class="page-overlay-content">
            <div class="spinner-border text-primary mb-3" role="status"></div>
            <div style="font-weight: 600; color: var(--text-primary);">${message || 'Yükleniyor...'}</div>
        </div>
    `;
    document.body.appendChild(overlay);
    return overlay;
}

function hidePageOverlay() {
    const overlay = document.querySelector('.page-overlay');
    if (overlay) overlay.remove();
}

// ═══════════════════ DATATABLES TURKISH LOCALE ═══════════════════
if (typeof $.fn !== 'undefined' && typeof $.fn.dataTable !== 'undefined') {
    $.extend(true, $.fn.dataTable.defaults, {
        language: {
            processing: "İşleniyor...",
            search: "Ara:",
            lengthMenu: "_MENU_ kayıt göster",
            info: "_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor",
            infoEmpty: "Kayıt yok",
            infoFiltered: "(_MAX_ toplam kayıttan filtrelendi)",
            loadingRecords: "Yükleniyor...",
            zeroRecords: "Eşleşen kayıt bulunamadı",
            emptyTable: "Tabloda veri yok",
            paginate: {
                first: "İlk",
                previous: "Önceki",
                next: "Sonraki",
                last: "Son"
            }
        }
    });
}

// ═══════════════════ LEGACY ALERT (compat) ═══════════════════
function showLegacyAlert(type, message) {
    if (ToastSystem && ToastSystem.show) {
        ToastSystem.show(message, type);
    }
}

// ═══════════════════ TOOLTIP INIT ═══════════════════
document.addEventListener('DOMContentLoaded', function () {
    // Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (el) {
        return new bootstrap.Tooltip(el);
    });

    // Init toast system
    ToastSystem.init();
});