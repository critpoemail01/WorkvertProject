(() => {
  const root = document.querySelector('[data-service-budget-selector]');
  if (!root) return;

  const valueInput = root.querySelector('[data-service-budget-value]');
  const currencyInput = root.querySelector('[data-service-budget-currency]');
  const summary = root.querySelector('[data-service-budget-summary]');
  const optionInputs = Array.from(root.querySelectorAll('[data-service-budget-option]'));
  const rangePanel = root.querySelector('[data-service-budget-range]');
  const minInput = root.querySelector('[data-service-budget-min]');
  const maxInput = root.querySelector('[data-service-budget-max]');
  const supportedCurrencies = Array.from(currencyInput?.options || []).map((option) => option.value);

  function selectedMode() {
    return optionInputs.find((input) => input.checked)?.value || 'Standard';
  }

  function currency() {
    return (currencyInput?.value || 'EUR').trim().toUpperCase() || 'EUR';
  }

  function amount(input) {
    const value = (input?.value || '').replace(',', '.').trim();
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) && parsed >= 0 ? parsed : null;
  }

  function formatAmount(value) {
    return Number.isInteger(value) ? value.toFixed(0) : value.toFixed(2).replace(/0+$/, '').replace(/\.$/, '');
  }

  function customLabel() {
    const min = amount(minInput);
    const max = amount(maxInput);
    const code = currency();
    if (min !== null && max !== null) return `${code} ${formatAmount(min)}-${formatAmount(max)}`;
    if (min !== null) return `${code} ${formatAmount(min)}+`;
    if (max !== null) return `${code} 0-${formatAmount(max)}`;
    return 'Need a quote';
  }

  function labelForMode() {
    const code = currency();
    switch (selectedMode()) {
      case 'Quote':
        return 'Need a quote';
      case 'Under300':
        return `${code} 0-300`;
      case 'Large':
        return `${code} 1500-5000`;
      case 'Custom':
        return customLabel();
      case 'Standard':
      default:
        return `${code} 800-1500`;
    }
  }

  function update() {
    const showRange = selectedMode() === 'Custom';
    if (rangePanel) rangePanel.hidden = !showRange;

    const label = labelForMode();
    if (valueInput) valueInput.value = label;
    if (summary) summary.textContent = label;
  }

  function applyCurrencySuggestion(event) {
    const suggestedCurrency = (event.detail?.currency || '').trim().toUpperCase();
    if (!currencyInput || !supportedCurrencies.includes(suggestedCurrency)) return;
    if (currencyInput.value === suggestedCurrency) return;

    currencyInput.value = suggestedCurrency;
    update();
  }

  optionInputs.forEach((input) => input.addEventListener('change', update));
  currencyInput?.addEventListener('change', update);
  document.addEventListener('workvert:location-currency-suggestion', applyCurrencySuggestion);
  minInput?.addEventListener('input', update);
  maxInput?.addEventListener('input', update);

  update();
})();
