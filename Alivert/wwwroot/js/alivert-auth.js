(() => {
  const modalEl = document.getElementById('authModal');
  if (!modalEl) return;

  const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

  const loginTabBtn = document.getElementById('tab-login');
  const registerTabBtn = document.getElementById('tab-register');
  const loginForm = document.getElementById('loginForm');
  const registerForm = document.getElementById('registerForm');
  const loginError = document.getElementById('loginError');
  const registerError = document.getElementById('registerError');

  const loginReturnUrl = document.getElementById('loginReturnUrl');
  const registerReturnUrl = document.getElementById('registerReturnUrl');

  function csrfToken() {
    const meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? (meta.getAttribute('content') || '') : '';
  }

  function currentReturnUrl() {
    const url = new URL(window.location.href);
    url.searchParams.delete('login');
    return url.pathname + url.search + url.hash;
  }

  function setReturnUrl(value) {
    const v = value || currentReturnUrl();
    if (loginReturnUrl) loginReturnUrl.value = v;
    if (registerReturnUrl) registerReturnUrl.value = v;
  }

  function showError(el, message) {
    if (!el) return;
    el.textContent = message || 'An error occurred. Please try again.';
    el.classList.remove('d-none');
  }

  function hideError(el) {
    if (!el) return;
    el.textContent = '';
    el.classList.add('d-none');
  }

  function openModal(tab) {
    hideError(loginError);
    hideError(registerError);

    if (tab === 'register' && registerTabBtn) {
      bootstrap.Tab.getOrCreateInstance(registerTabBtn).show();
    } else if (loginTabBtn) {
      bootstrap.Tab.getOrCreateInstance(loginTabBtn).show();
    }

    modal.show();
  }

  // Buttons in navbar
  document.querySelectorAll('[data-auth-open]').forEach((btn) => {
    btn.addEventListener('click', () => {
      const tab = btn.getAttribute('data-auth-open') || 'login';
      setReturnUrl(currentReturnUrl());
      openModal(tab);
    });
  });

  // Auto-open on redirect: /?login=1&returnUrl=...
  const params = new URLSearchParams(window.location.search);
  if (params.get('login') === '1') {
    setReturnUrl(params.get('returnUrl') || '/App/Dashboard');
    openModal('login');
  }

  async function postForm(url, form, errorEl) {
    hideError(errorEl);

    const fd = new FormData(form);
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'RequestVerificationToken': csrfToken()
      },
      body: fd
    });

    let payload = null;
    try {
      payload = await res.json();
    } catch {
      payload = null;
    }

    if (!res.ok) {
      const msg = payload && payload.message ? payload.message : 'Could not complete the request. Please check your details.';
      showError(errorEl, msg);
      return;
    }

    const redirect = payload && payload.redirect ? payload.redirect : '/App/Dashboard';
    window.location.assign(redirect);
  }

  if (loginForm) {
    loginForm.addEventListener('submit', (e) => {
      e.preventDefault();
      postForm('/auth/login', loginForm, loginError);
    });
  }

  if (registerForm) {
    registerForm.addEventListener('submit', (e) => {
      e.preventDefault();
      postForm('/auth/register', registerForm, registerError);
    });
  }
})();
