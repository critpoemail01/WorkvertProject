(() => {
  const market = document.querySelector('[data-symbol-market]');
  const input = document.querySelector('[data-symbol-input]');
  const list = document.getElementById('symbolSuggestions');
  if (!market || !input || !list) return;

  let timer = null;
  let lastQuery = '';

  async function search() {
    const q = input.value.trim();
    const m = market.value;
    if (q.length < 1 || `${m}:${q}` === lastQuery) return;

    lastQuery = `${m}:${q}`;
    const res = await fetch(`/symbols/search?market=${encodeURIComponent(m)}&q=${encodeURIComponent(q)}`);
    if (!res.ok) return;

    const items = await res.json();
    list.innerHTML = '';

    for (const item of items) {
      const option = document.createElement('option');
      option.value = item.symbol || item.Symbol;
      const name = item.name || item.Name || '';
      const exchange = item.exchange || item.Exchange || '';
      option.label = exchange ? `${name} (${exchange})` : name;
      list.appendChild(option);
    }
  }

  input.addEventListener('input', () => {
    window.clearTimeout(timer);
    timer = window.setTimeout(search, 250);
  });

  market.addEventListener('change', () => {
    input.value = '';
    list.innerHTML = '';
    lastQuery = '';
  });
})();
