(() => {
  const configs = {
    PriceAbove: {
      fields: ['threshold', 'ema'],
      label: 'Target reach',
      help: 'Estimated users to reach with TikTok short-form content.',
      summary: 'TikTok campaign: generate hooks, short scripts and a fast call to action for discovery.'
    },
    PriceBelow: {
      fields: ['threshold', 'ema'],
      label: 'Target engagement',
      help: 'Expected profile visits, saves, replies or link taps from Instagram content.',
      summary: 'Instagram campaign: prepare posts, reels and captions built for interaction.'
    },
    PercentDrop24h: {
      fields: ['threshold', 'zone'],
      label: 'Target reach',
      help: 'Expected users reached by Facebook ads or organic posts.',
      summary: 'Facebook campaign: package the offer, audience angle and follow-up message.'
    },
    PercentRise24h: {
      fields: ['threshold', 'rsi'],
      label: 'Target leads',
      help: 'Expected professional leads or demo requests from LinkedIn.',
      summary: 'LinkedIn campaign: write credible B2B posts, founder updates and outreach angles.'
    },
    VolumeAbove24h: {
      fields: ['threshold', 'rsi', 'ema'],
      label: 'Target recipients',
      help: 'Number of potential clients in the personalized email list.',
      summary: 'Email campaign: create segmented copy, subject lines and follow-up steps.'
    },
    PriceZone: {
      fields: ['threshold', 'zone'],
      label: 'Target recipients',
      help: 'Number of contacts to receive the SMS promotion.',
      summary: 'SMS campaign: keep the offer short, urgent and easy to act on.'
    },
    RsiBelow: {
      fields: ['threshold', 'rsi'],
      label: 'Retargeting audience',
      help: 'Users to retarget after visiting, clicking or starting checkout.',
      summary: 'Retargeting campaign: bring warm users back with a clear reason to return.'
    },
    RsiAbove: {
      fields: ['threshold', 'ema'],
      label: 'Launch reach',
      help: 'Expected users to reach during the launch announcement.',
      summary: 'Launch campaign: coordinate social, email and direct messages around one release.'
    },
    EmaCrossUp: {
      fields: ['ema'],
      summary: 'Multi-channel push: prepare coordinated posts, emails and SMS touches.'
    },
    EmaCrossDown: {
      fields: ['ema'],
      summary: 'Win-back campaign: re-engage users who tried the product but did not convert.'
    },
    RsiOversoldEmaCrossUp: {
      fields: ['threshold', 'rsi', 'ema'],
      label: 'Influencer audience',
      help: 'Expected audience size for creator, partner or affiliate content.',
      summary: 'Influencer brief: define the hook, talking points and conversion path.'
    },
    RsiOverboughtEmaCrossDown: {
      fields: ['threshold', 'rsi', 'ema'],
      label: 'Nurture list size',
      help: 'Potential clients to enter a lead nurture sequence.',
      summary: 'Lead nurture sequence: turn interested users into subscribers or buyers.'
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
