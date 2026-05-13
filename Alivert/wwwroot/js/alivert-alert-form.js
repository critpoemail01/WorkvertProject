(() => {
  const configs = {
    PriceAbove: {
      fields: ['threshold'],
      label: 'Price level',
      help: 'Triggers when the latest market price is at or above this value.',
      summary: 'Price alert: choose the price level that should trigger the notification.'
    },
    PriceBelow: {
      fields: ['threshold'],
      label: 'Price level',
      help: 'Triggers when the latest market price is at or below this value.',
      summary: 'Price alert: choose the price level that should trigger the notification.'
    },
    PercentDrop24h: {
      fields: ['threshold'],
      label: '24h drop percent',
      help: 'Use a negative value such as -3 for a 3% drop.',
      summary: 'Percent alert: compares the current 24h percentage change with your limit.'
    },
    PercentRise24h: {
      fields: ['threshold'],
      label: '24h rise percent',
      help: 'Use a positive value such as 3 for a 3% rise.',
      summary: 'Percent alert: compares the current 24h percentage change with your limit.'
    },
    VolumeAbove24h: {
      fields: ['threshold'],
      label: '24h volume threshold',
      help: 'Triggers when the reported 24h volume is at or above this value.',
      summary: 'Volume alert: watches 24h volume and fires when liquidity expands above your threshold.'
    },
    PriceZone: {
      fields: ['threshold', 'zone'],
      label: 'Price zone center',
      help: 'The center price for the zone. Zone percent creates a band around this value.',
      summary: 'Price zone alert: fires when price enters the threshold band defined by zone percent.'
    },
    RsiBelow: {
      fields: ['threshold', 'rsi'],
      label: 'RSI below limit',
      help: 'Triggers when RSI is at or below this value, usually 30 for oversold setups.',
      summary: 'RSI alert: uses the RSI period and limit to detect oversold conditions.'
    },
    RsiAbove: {
      fields: ['threshold', 'rsi'],
      label: 'RSI above limit',
      help: 'Triggers when RSI is at or above this value, usually 70 for overbought setups.',
      summary: 'RSI alert: uses the RSI period and limit to detect overbought conditions.'
    },
    EmaCrossUp: {
      fields: ['ema'],
      summary: 'EMA alert: fires when the fast EMA crosses above the slow EMA.'
    },
    EmaCrossDown: {
      fields: ['ema'],
      summary: 'EMA alert: fires when the fast EMA crosses below the slow EMA.'
    },
    RsiOversoldEmaCrossUp: {
      fields: ['threshold', 'rsi', 'ema'],
      label: 'Oversold RSI arm level',
      help: 'Arms when RSI is at or below this level, then waits for fast EMA to cross above slow EMA.',
      summary: 'RSI + EMA alert: first arms on oversold RSI, then triggers on bullish EMA confirmation.'
    },
    RsiOverboughtEmaCrossDown: {
      fields: ['threshold', 'rsi', 'ema'],
      label: 'Overbought RSI arm level',
      help: 'Arms when RSI is at or above this level, then waits for fast EMA to cross below slow EMA.',
      summary: 'RSI + EMA alert: first arms on overbought RSI, then triggers on bearish EMA confirmation.'
    }
  };

  const defaultConfig = configs.PriceAbove;

  document.querySelectorAll('[data-alert-form]').forEach((form) => {
    const ruleSelect = form.querySelector('[data-alert-rule-select]');
    if (!ruleSelect) return;

    const fieldGroups = Array.from(form.querySelectorAll('[data-alert-field]'));
    const thresholdLabel = form.querySelector('[data-alert-threshold-label]');
    const thresholdHelp = form.querySelector('[data-alert-threshold-help]');
    const summary = form.querySelector('[data-alert-rule-summary]');

    const refresh = () => {
      const config = configs[ruleSelect.value] || defaultConfig;
      const visible = new Set(config.fields || []);

      fieldGroups.forEach((group) => {
        const show = visible.has(group.dataset.alertField);
        group.classList.toggle('d-none', !show);
        group.setAttribute('aria-hidden', show ? 'false' : 'true');
      });

      if (thresholdLabel && config.label) {
        thresholdLabel.textContent = config.label;
      }

      if (thresholdHelp && config.help) {
        thresholdHelp.textContent = config.help;
      }

      if (summary && config.summary) {
        summary.textContent = config.summary;
      }
    };

    ruleSelect.addEventListener('change', refresh);
    refresh();
  });
})();
