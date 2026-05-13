(() => {
  const defaults = {
    Crypto: [
      { symbol: 'BTCUSDT', name: 'BTC/USDT', exchange: 'Binance' },
      { symbol: 'ETHUSDT', name: 'ETH/USDT', exchange: 'Binance' },
      { symbol: 'SOLUSDT', name: 'SOL/USDT', exchange: 'Binance' },
      { symbol: 'BNBUSDT', name: 'BNB/USDT', exchange: 'Binance' },
      { symbol: 'XRPUSDT', name: 'XRP/USDT', exchange: 'Binance' }
    ],
    Traditional: [
      { symbol: 'AAPL', name: 'Apple Inc.', exchange: 'Nasdaq' },
      { symbol: 'MSFT', name: 'Microsoft Corporation', exchange: 'Nasdaq' },
      { symbol: 'SPY', name: 'SPDR S&P 500 ETF', exchange: 'NYSE Arca' },
      { symbol: '^GSPC', name: 'S&P 500', exchange: 'Yahoo Finance' },
      { symbol: 'GC=F', name: 'Gold futures', exchange: 'Yahoo Finance' }
    ]
  };

  function normalizeMarket(value) {
    if (value === '1') return 'Crypto';
    if (value === '2') return 'Traditional';
    return value || 'Crypto';
  }

  function setMarketValue(input, normalized) {
    if (input instanceof HTMLSelectElement) {
      const option = Array.from(input.options).find((item) => normalizeMarket(item.value) === normalized);
      input.value = option?.value || normalized;
      return;
    }

    input.value = normalized;
  }

  function setupSymbolAutocomplete(root) {
    const scope = root.closest('form') || document;
    const marketInput = scope.querySelector('[data-symbol-market]');
    const symbolInput = root.querySelector('[data-symbol-input]');
    const menu = root.querySelector('[data-symbol-results]');
    const help = root.querySelector('[data-symbol-help]');
    const marketOptions = Array.from(scope.querySelectorAll('[data-market-option]'));
    if (!marketInput || !symbolInput || !menu) return;

    let timer = null;
    let activeIndex = -1;
    let lastRequestKey = '';
    let abortController = null;
    let blurTimer = null;

    function currentMarket() {
      return normalizeMarket(marketInput.value);
    }

    function setHelp(message, isLoading = false) {
      if (!help) return;
      help.textContent = message;
      help.classList.toggle('loading', isLoading);
    }

    function hideMenu() {
      window.clearTimeout(blurTimer);
      menu.classList.remove('open');
      menu.innerHTML = '';
      activeIndex = -1;
      symbolInput.setAttribute('aria-expanded', 'false');
    }

    function focusStillBelongsToPicker() {
      const active = document.activeElement;
      return root.contains(active) || marketOptions.some((option) => option === active || option.contains(active));
    }

    function scheduleHideMenu() {
      window.clearTimeout(blurTimer);
      blurTimer = window.setTimeout(() => {
        if (!focusStillBelongsToPicker()) {
          hideMenu();
        }
      }, 140);
    }

    function setMarket(value, shouldClearSymbol) {
      const normalized = normalizeMarket(value);
      setMarketValue(marketInput, normalized);

      marketOptions.forEach((option) => {
        const active = option.getAttribute('data-market-option') === normalized;
        option.classList.toggle('active', active);
        option.setAttribute('aria-pressed', active ? 'true' : 'false');
      });

      if (shouldClearSymbol) {
        symbolInput.value = '';
        lastRequestKey = '';
      }

      hideMenu();
      symbolInput.placeholder = normalized === 'Crypto'
        ? 'Search BTCUSDT, ETHUSDT, SOLUSDT...'
        : 'Search AAPL, MSFT, SPY, ^GSPC...';
      setHelp(normalized === 'Crypto'
        ? 'Search Binance spot symbols such as BTCUSDT or ETHUSDT.'
        : 'Search Yahoo Finance symbols such as AAPL, SPY, ^GSPC or GC=F.');
    }

    function optionElements() {
      return Array.from(menu.querySelectorAll('[data-symbol-option]'));
    }

    function setActive(index) {
      const options = optionElements();
      if (!options.length) {
        activeIndex = -1;
        return;
      }

      activeIndex = (index + options.length) % options.length;
      options.forEach((option, i) => {
        option.classList.toggle('active', i === activeIndex);
        option.setAttribute('aria-selected', i === activeIndex ? 'true' : 'false');
      });
      options[activeIndex].scrollIntoView({ block: 'nearest' });
    }

    function selectItem(item) {
      if (!item || !item.symbol) return;
      symbolInput.value = String(item.symbol).toUpperCase();
      symbolInput.dispatchEvent(new Event('change', { bubbles: true }));
      hideMenu();
      setHelp(`${symbolInput.value} selected from ${item.exchange || currentMarket()}.`);
    }

    function renderItems(items, emptyMessage) {
      window.clearTimeout(blurTimer);
      menu.innerHTML = '';

      if (!items.length) {
        const empty = document.createElement('div');
        empty.className = 'symbol-autocomplete-empty';
        empty.textContent = emptyMessage;
        menu.appendChild(empty);
        menu.classList.add('open');
        symbolInput.setAttribute('aria-expanded', 'true');
        activeIndex = -1;
        return;
      }

      items.forEach((item, index) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'symbol-autocomplete-option';
        button.setAttribute('role', 'option');
        button.setAttribute('data-symbol-option', '');
        button.setAttribute('aria-selected', 'false');

        const symbol = document.createElement('span');
        symbol.className = 'symbol-autocomplete-code';
        symbol.textContent = item.symbol || item.Symbol || '';

        const detail = document.createElement('span');
        detail.className = 'symbol-autocomplete-detail';
        const name = item.name || item.Name || '';
        const exchange = item.exchange || item.Exchange || '';
        detail.textContent = exchange ? `${name} - ${exchange}` : name;

        button.append(symbol, detail);
        button.addEventListener('mousedown', (event) => {
          event.preventDefault();
          selectItem({
            symbol: item.symbol || item.Symbol,
            name,
            exchange
          });
        });
        button.addEventListener('mouseenter', () => setActive(index));
        menu.appendChild(button);
      });

      menu.classList.add('open');
      symbolInput.setAttribute('aria-expanded', 'true');
      setActive(0);
    }

    async function searchSymbols() {
      const query = symbolInput.value.trim();
      const market = currentMarket();

      if (query.length < 1) {
        renderItems(defaults[market] || [], 'Start typing to search symbols.');
        setHelp(market === 'Crypto'
          ? 'Popular Binance pairs shown. Type to search the full Binance catalog.'
          : 'Popular Yahoo symbols shown. Type to search Yahoo Finance.');
        return;
      }

      const requestKey = `${market}:${query}`;
      if (requestKey === lastRequestKey && menu.classList.contains('open')) return;
      lastRequestKey = requestKey;

      if (abortController) abortController.abort();
      abortController = new AbortController();

      setHelp('Searching symbols...', true);
      try {
        const res = await fetch(`/symbols/search?market=${encodeURIComponent(market)}&q=${encodeURIComponent(query)}`, {
          signal: abortController.signal
        });
        if (!res.ok) throw new Error('Symbol search failed.');

        const items = await res.json();
        renderItems(items, `No ${market === 'Crypto' ? 'Binance' : 'Yahoo Finance'} symbols found.`);
        setHelp(items.length
          ? 'Choose a result with mouse or Enter.'
          : 'No match found. You can still type a symbol and validation will check it.');
      } catch (error) {
        if (error.name === 'AbortError') return;
        renderItems([], 'Symbol search is unavailable right now.');
        setHelp('Could not load symbols. You can still type the symbol manually.');
      }
    }

    function queueSearch(delay = 180) {
      window.clearTimeout(timer);
      timer = window.setTimeout(searchSymbols, delay);
    }

    function showPopularSymbols() {
      window.clearTimeout(timer);
      if (abortController) abortController.abort();
      lastRequestKey = '';
      const market = currentMarket();
      renderItems(defaults[market] || [], 'Start typing to search symbols.');
      setHelp(market === 'Crypto'
        ? 'Popular Binance pairs shown. Type to search the full Binance catalog.'
        : 'Popular Yahoo symbols shown. Type to search Yahoo Finance.');
    }

    marketOptions.forEach((option) => {
      option.addEventListener('click', () => {
        setMarket(option.getAttribute('data-market-option'), true);
        symbolInput.focus();
        showPopularSymbols();
      });
    });

    marketInput.addEventListener('change', () => {
      setMarket(marketInput.value, true);
      symbolInput.focus();
      showPopularSymbols();
    });

    symbolInput.addEventListener('focus', () => queueSearch(0));
    symbolInput.addEventListener('input', () => {
      symbolInput.value = symbolInput.value.toUpperCase();
      queueSearch();
    });
    symbolInput.addEventListener('blur', () => {
      scheduleHideMenu();
    });
    symbolInput.addEventListener('keydown', (event) => {
      const options = optionElements();
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        if (!menu.classList.contains('open')) queueSearch(0);
        else setActive(activeIndex + 1);
      } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        if (options.length) setActive(activeIndex - 1);
      } else if (event.key === 'Enter' && menu.classList.contains('open') && activeIndex >= 0 && options[activeIndex]) {
        event.preventDefault();
        options[activeIndex].dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true }));
      } else if (event.key === 'Escape') {
        hideMenu();
      }
    });

    setMarket(marketInput.value, false);
  }

  document.querySelectorAll('[data-symbol-autocomplete]').forEach(setupSymbolAutocomplete);
})();
