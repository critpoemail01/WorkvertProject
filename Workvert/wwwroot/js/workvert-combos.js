(() => {
  const selects = Array.from(document.querySelectorAll('select[data-combo]'));

  let openCombo = null;

  function closeCombo(combo) {
    if (!combo) return;
    combo.classList.remove('open');
    combo.button.setAttribute('aria-expanded', 'false');
  }

  function closeOpenCombo() {
    closeCombo(openCombo);
    openCombo = null;
  }

  function optionText(option) {
    return option ? option.textContent.trim() : '';
  }

  function buildCombo(select) {
    const wrap = document.createElement('div');
    wrap.className = 'combo-select';

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'combo-select-button';
    button.setAttribute('aria-haspopup', 'listbox');
    button.setAttribute('aria-expanded', 'false');

    const value = document.createElement('span');
    value.className = 'combo-select-value';

    const icon = document.createElement('i');
    icon.className = 'bi bi-chevron-down';
    button.append(value, icon);

    const menu = document.createElement('div');
    menu.className = 'combo-select-menu';
    menu.setAttribute('role', 'listbox');

    const combo = { select, wrap, button, value, menu, activeIndex: Math.max(select.selectedIndex, 0) };

    function syncButton() {
      value.textContent = optionText(select.options[select.selectedIndex]);
    }

    function setActive(index) {
      const items = Array.from(menu.querySelectorAll('[data-combo-option]'));
      if (!items.length) return;
      combo.activeIndex = (index + items.length) % items.length;
      items.forEach((item, i) => {
        const active = i === combo.activeIndex;
        item.classList.toggle('active', active);
        item.setAttribute('aria-selected', active ? 'true' : 'false');
      });
      items[combo.activeIndex].scrollIntoView({ block: 'nearest' });
    }

    function choose(index) {
      select.selectedIndex = index;
      select.dispatchEvent(new Event('change', { bubbles: true }));
      syncButton();
      closeOpenCombo();
    }

    Array.from(select.options).forEach((option, index) => {
      const item = document.createElement('button');
      item.type = 'button';
      item.className = 'combo-select-option';
      item.setAttribute('role', 'option');
      item.setAttribute('data-combo-option', '');
      item.textContent = optionText(option);
      item.addEventListener('click', () => choose(index));
      item.addEventListener('mouseenter', () => setActive(index));
      menu.appendChild(item);
    });

    button.addEventListener('click', (event) => {
      event.preventDefault();
      const isOpen = wrap.classList.contains('open');
      closeOpenCombo();
      if (!isOpen) {
        wrap.classList.add('open');
        button.setAttribute('aria-expanded', 'true');
        openCombo = combo;
        setActive(select.selectedIndex);
      }
    });

    button.addEventListener('keydown', (event) => {
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        if (!wrap.classList.contains('open')) button.click();
        else setActive(combo.activeIndex + 1);
      } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        if (!wrap.classList.contains('open')) button.click();
        else setActive(combo.activeIndex - 1);
      } else if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        if (!wrap.classList.contains('open')) button.click();
        else choose(combo.activeIndex);
      } else if (event.key === 'Escape') {
        event.preventDefault();
        closeOpenCombo();
      }
    });

    select.addEventListener('change', () => {
      combo.activeIndex = Math.max(select.selectedIndex, 0);
      syncButton();
      setActive(combo.activeIndex);
    });

    wrap.append(button, menu);
    select.classList.add('combo-native');
    select.insertAdjacentElement('afterend', wrap);
    syncButton();
    setActive(select.selectedIndex);
  }

  selects.forEach(buildCombo);

  function formatZoneTime(timeZone) {
    try {
      return new Intl.DateTimeFormat('en-US', {
        timeZone,
        weekday: 'short',
        hour: '2-digit',
        minute: '2-digit',
        timeZoneName: 'short'
      }).format(new Date());
    } catch {
      return 'time unavailable';
    }
  }

  function setupTimezoneHelper(root, selectors) {
    if (!root) return;

    const select = root.querySelector(selectors.select);
    const browserZoneOutput = root.querySelector(selectors.browserZone);
    const selectedClockOutput = root.querySelector(selectors.selectedClock);
    const useBrowserButton = root.querySelector(selectors.useBrowserButton);
    if (!select || !browserZoneOutput || !selectedClockOutput || !useBrowserButton) return;

    const browserTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

    function findOption(value) {
      return Array.from(select.options).find((option) => option.value === value);
    }

    function updateSelectedClock() {
      const selectedZone = select.value || 'UTC';
      selectedClockOutput.textContent = `${selectedZone} - now ${formatZoneTime(selectedZone)}`;
    }

    function updateBrowserZone() {
      const option = findOption(browserTimeZone);
      browserZoneOutput.textContent = `${browserTimeZone} - now ${formatZoneTime(browserTimeZone)}`;
      useBrowserButton.disabled = !option;
      useBrowserButton.textContent = option ? 'Use detected' : 'Choose manually';
    }

    useBrowserButton.addEventListener('click', () => {
      const option = findOption(browserTimeZone);
      if (!option) return;
      select.value = browserTimeZone;
      select.dispatchEvent(new Event('change', { bubbles: true }));
      updateSelectedClock();
    });

    select.addEventListener('change', updateSelectedClock);
    updateBrowserZone();
    updateSelectedClock();
    window.setInterval(() => {
      updateBrowserZone();
      updateSelectedClock();
    }, 60000);
  }

  function setupScheduleTimezoneHelper() {
    setupTimezoneHelper(document.querySelector('#scheduleModal'), {
      select: '#scheduleTimeZone',
      browserZone: '[data-browser-timezone]',
      selectedClock: '[data-selected-timezone-clock]',
      useBrowserButton: '[data-use-browser-timezone]'
    });
  }

  function setupSettingsTimezoneHelper() {
    setupTimezoneHelper(document.querySelector('[data-settings-timezone]'), {
      select: '[data-settings-timezone-select]',
      browserZone: '[data-settings-browser-timezone]',
      selectedClock: '[data-settings-selected-timezone-clock]',
      useBrowserButton: '[data-settings-use-browser-timezone]'
    });
  }

  setupScheduleTimezoneHelper();
  setupSettingsTimezoneHelper();

  document.addEventListener('click', (event) => {
    if (openCombo && !openCombo.wrap.contains(event.target)) {
      closeOpenCombo();
    }
  });
})();
