(() => {
  const root = document.querySelector('[data-service-urgency-selector]');
  if (!root) return;

  const valueInput = root.querySelector('[data-service-urgency-value]');
  const summary = root.querySelector('[data-service-urgency-summary]');
  const optionInputs = Array.from(root.querySelectorAll('[data-service-urgency-option]'));
  const monthsPanel = root.querySelector('[data-service-urgency-months]');
  const monthsInput = root.querySelector('[data-service-urgency-months-input]');
  const monthsRange = root.querySelector('[data-service-urgency-months-range]');
  const monthsLabel = root.querySelector('[data-service-urgency-months-label]');

  function selectedPreset() {
    return optionInputs.find((input) => input.checked)?.value || 'ThisMonth';
  }

  function months() {
    const parsed = Number.parseInt(monthsInput?.value || '3', 10);
    return Number.isFinite(parsed) ? Math.max(1, Math.min(parsed, 24)) : 3;
  }

  function labelForPreset() {
    switch (selectedPreset()) {
      case 'Urgent':
        return 'Urgent';
      case 'ThisWeek':
        return 'This week';
      case 'NextMonths':
        return `Next ${months()} months`;
      case 'Flexible':
        return 'Flexible';
      case 'ThisMonth':
      default:
        return 'This month';
    }
  }

  function syncMonthsControls(source) {
    const current = months();
    if (monthsInput && source !== monthsInput) monthsInput.value = current.toString();
    if (monthsRange && source !== monthsRange) monthsRange.value = current.toString();
    if (monthsLabel) monthsLabel.textContent = `${current} months`;
  }

  function update() {
    const isMonthRange = selectedPreset() === 'NextMonths';
    if (monthsPanel) monthsPanel.hidden = !isMonthRange;
    syncMonthsControls();

    const label = labelForPreset();
    if (valueInput) valueInput.value = label;
    if (summary) summary.textContent = label;
  }

  optionInputs.forEach((input) => input.addEventListener('change', update));
  monthsInput?.addEventListener('input', () => {
    syncMonthsControls(monthsInput);
    update();
  });
  monthsRange?.addEventListener('input', () => {
    if (monthsInput) monthsInput.value = monthsRange.value;
    syncMonthsControls(monthsRange);
    update();
  });

  update();
})();
