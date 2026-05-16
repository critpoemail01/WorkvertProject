(() => {
  const nav = document.getElementById('nav');
  const toggle = document.querySelector('.workvert-menu-toggle');
  if (!nav || !toggle) return;

  const storageKey = 'workvert.mobileNavOpen';
  const mobileQuery = window.matchMedia('(max-width: 991.98px)');

  function updateToggleLabel(isOpen) {
    toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    toggle.setAttribute('aria-label', isOpen ? 'Close navigation menu' : 'Open navigation menu');
  }

  function openMobileNav() {
    nav.classList.add('show');
    updateToggleLabel(true);
  }

  function shouldRememberOpenState() {
    return mobileQuery.matches;
  }

  nav.addEventListener('shown.bs.collapse', () => {
    updateToggleLabel(true);
    if (shouldRememberOpenState()) {
      sessionStorage.setItem(storageKey, '1');
    }
  });

  nav.addEventListener('hidden.bs.collapse', () => {
    updateToggleLabel(false);
    sessionStorage.removeItem(storageKey);
  });

  nav.querySelectorAll('.nav-link').forEach((link) => {
    link.addEventListener('click', () => {
      if (shouldRememberOpenState()) {
        sessionStorage.setItem(storageKey, '1');
      }
    });
  });

  if (shouldRememberOpenState() && sessionStorage.getItem(storageKey) === '1') {
    openMobileNav();
  }

  mobileQuery.addEventListener('change', (event) => {
    if (!event.matches) {
      nav.classList.remove('show');
      updateToggleLabel(false);
    } else if (sessionStorage.getItem(storageKey) === '1') {
      openMobileNav();
    }
  });
})();
