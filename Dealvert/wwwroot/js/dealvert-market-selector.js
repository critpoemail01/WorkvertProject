(() => {
  const root = document.querySelector('[data-product-market-selector]');
  if (!root) return;

  const scopeInputs = Array.from(root.querySelectorAll('[data-market-scope]'));
  const countryInput = root.querySelector('[data-market-country]');
  const cityInput = root.querySelector('[data-market-city]');
  const categoryInput = root.querySelector('[data-market-category]');
  const countryPanel = root.querySelector('[data-market-panel="country"]');
  const cityPanel = root.querySelector('[data-market-panel="city"]');
  const mapPanel = root.querySelector('[data-market-map-panel]');
  const mapEl = root.querySelector('[data-market-map]');
  const latInput = root.querySelector('[data-market-lat]');
  const lngInput = root.querySelector('[data-market-lng]');
  const radiusInput = root.querySelector('[data-market-radius]');
  const radiusRange = root.querySelector('[data-market-radius-range]');
  const radiusLabel = root.querySelector('[data-market-radius-label]');
  const areaSummary = root.querySelector('[data-market-area-summary]');
  const coordinates = root.querySelector('[data-market-coordinates]');
  const suggestionSummary = root.querySelector('[data-market-suggestion-summary]');

  const countryProfiles = {
    world: {
      label: 'Worldwide',
      stores: ['Amazon', 'eBay', 'Back Market', 'AliExpress', 'Apple Store', 'Samsung Store', 'Google Store'],
      resale: ['eBay', 'Facebook Marketplace', 'Vinted', 'Wallapop', 'Depop']
    },
    portugal: {
      label: 'Portugal',
      stores: ['Amazon.es', 'Worten', 'Fnac', 'PcComponentes', 'MediaMarkt', 'Radio Popular', 'PCDiga'],
      resale: ['OLX', 'Vinted', 'Wallapop', 'CustoJusto', 'Facebook Marketplace']
    },
    spain: {
      label: 'Spain',
      stores: ['Amazon.es', 'PcComponentes', 'MediaMarkt', 'El Corte Ingles', 'Fnac', 'Carrefour', 'Worten'],
      resale: ['Wallapop', 'Milanuncios', 'Vinted', 'eBay', 'Facebook Marketplace']
    },
    france: {
      label: 'France',
      stores: ['Amazon.fr', 'Fnac', 'Darty', 'Boulanger', 'Cdiscount', 'Rakuten'],
      resale: ['Leboncoin', 'Vinted', 'Rakuten', 'eBay', 'Facebook Marketplace']
    },
    'united kingdom': {
      label: 'United Kingdom',
      stores: ['Amazon.co.uk', 'Currys', 'Argos', 'John Lewis', 'Very', 'CeX'],
      resale: ['eBay', 'Gumtree', 'Vinted', 'Facebook Marketplace', 'CeX']
    },
    germany: {
      label: 'Germany',
      stores: ['Amazon.de', 'MediaMarkt', 'Saturn', 'Otto', 'Cyberport', 'Conrad'],
      resale: ['Kleinanzeigen', 'Vinted', 'eBay', 'Facebook Marketplace', 'Rebuy']
    },
    italy: {
      label: 'Italy',
      stores: ['Amazon.it', 'MediaWorld', 'Unieuro', 'Euronics', 'ePRICE'],
      resale: ['Subito', 'Vinted', 'eBay', 'Facebook Marketplace']
    },
    'united states': {
      label: 'United States',
      stores: ['Amazon.com', 'Best Buy', 'Walmart', 'Target', 'B&H', 'Newegg'],
      resale: ['eBay', 'Facebook Marketplace', 'Craigslist', 'Mercari', 'OfferUp', 'Swappa']
    },
    canada: {
      label: 'Canada',
      stores: ['Amazon.ca', 'Best Buy Canada', 'Walmart Canada', 'The Source', 'Newegg Canada'],
      resale: ['Kijiji', 'Facebook Marketplace', 'eBay', 'VarageSale']
    },
    brazil: {
      label: 'Brazil',
      stores: ['Amazon.com.br', 'Mercado Livre', 'Magalu', 'Americanas', 'Kabum'],
      resale: ['OLX Brazil', 'Enjoei', 'Facebook Marketplace', 'Mercado Livre']
    }
  };

  const cityOptions = [
    { city: 'Lisbon', country: 'Portugal', lat: 38.7223, lng: -9.1393 },
    { city: 'Porto', country: 'Portugal', lat: 41.1579, lng: -8.6291 },
    { city: 'Braga', country: 'Portugal', lat: 41.5454, lng: -8.4265 },
    { city: 'Coimbra', country: 'Portugal', lat: 40.2033, lng: -8.4103 },
    { city: 'Aveiro', country: 'Portugal', lat: 40.6405, lng: -8.6538 },
    { city: 'Faro', country: 'Portugal', lat: 37.0194, lng: -7.9304 },
    { city: 'Madrid', country: 'Spain', lat: 40.4168, lng: -3.7038 },
    { city: 'Barcelona', country: 'Spain', lat: 41.3874, lng: 2.1686 },
    { city: 'Valencia', country: 'Spain', lat: 39.4699, lng: -0.3763 },
    { city: 'Seville', country: 'Spain', lat: 37.3891, lng: -5.9845 },
    { city: 'Paris', country: 'France', lat: 48.8566, lng: 2.3522 },
    { city: 'Lyon', country: 'France', lat: 45.764, lng: 4.8357 },
    { city: 'London', country: 'United Kingdom', lat: 51.5072, lng: -0.1276 },
    { city: 'Manchester', country: 'United Kingdom', lat: 53.4808, lng: -2.2426 },
    { city: 'Berlin', country: 'Germany', lat: 52.52, lng: 13.405 },
    { city: 'Munich', country: 'Germany', lat: 48.1351, lng: 11.582 },
    { city: 'Rome', country: 'Italy', lat: 41.9028, lng: 12.4964 },
    { city: 'Milan', country: 'Italy', lat: 45.4642, lng: 9.19 },
    { city: 'New York', country: 'United States', lat: 40.7128, lng: -74.006 },
    { city: 'Los Angeles', country: 'United States', lat: 34.0522, lng: -118.2437 },
    { city: 'San Francisco', country: 'United States', lat: 37.7749, lng: -122.4194 },
    { city: 'Miami', country: 'United States', lat: 25.7617, lng: -80.1918 },
    { city: 'Toronto', country: 'Canada', lat: 43.6532, lng: -79.3832 },
    { city: 'Vancouver', country: 'Canada', lat: 49.2827, lng: -123.1207 },
    { city: 'Sao Paulo', country: 'Brazil', lat: -23.5558, lng: -46.6396 },
    { city: 'Rio de Janeiro', country: 'Brazil', lat: -22.9068, lng: -43.1729 }
  ];

  const categoryProfiles = [
    {
      label: 'Electronics',
      aliases: ['electronics', 'tech', 'phones', 'smartphones', 'laptops', 'consoles', 'gaming'],
      stores: ['Back Market', 'Apple Store', 'Samsung Store', 'Google Store', 'Nintendo Store'],
      resale: ['Swappa', 'CeX', 'Back Market', 'eBay']
    },
    {
      label: 'Fashion',
      aliases: ['fashion', 'clothing', 'sneakers', 'shoes', 'streetwear'],
      stores: ['Zalando', 'Nike', 'Adidas', 'Farfetch', 'About You'],
      resale: ['Vinted', 'Depop', 'Grailed', 'Vestiaire Collective']
    },
    {
      label: 'Home and appliances',
      aliases: ['home', 'appliances', 'kitchen', 'furniture'],
      stores: ['IKEA', 'Leroy Merlin', 'Amazon', 'MediaMarkt'],
      resale: ['Facebook Marketplace', 'OLX', 'Wallapop', 'eBay']
    },
    {
      label: 'Collectibles',
      aliases: ['collectibles', 'cards', 'lego', 'toys', 'watches'],
      stores: ['Amazon', 'eBay', 'StockX', 'Chrono24'],
      resale: ['eBay', 'Cardmarket', 'StockX', 'Chrono24', 'Facebook Marketplace']
    }
  ];

  const selected = {
    trusted: splitList(root.querySelector('[data-market-value="trusted"]')?.value),
    resale: splitList(root.querySelector('[data-market-value="resale"]')?.value)
  };
  const dirty = { trusted: false, resale: false };
  let map = null;
  let marker = null;
  let circle = null;

  function normalize(value) {
    return (value || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  function splitList(value) {
    return (value || '')
      .split(/[,;\n\r]+/)
      .map((item) => item.trim())
      .filter(Boolean)
      .filter((item, index, items) => items.findIndex((candidate) => normalize(candidate) === normalize(item)) === index);
  }

  function unique(items) {
    const seen = new Set();
    return items.filter((item) => {
      const key = normalize(item);
      if (!key || seen.has(key)) return false;
      seen.add(key);
      return true;
    });
  }

  function selectedScope() {
    return scopeInputs.find((input) => input.checked)?.value || 'City';
  }

  function resolveCountry(value) {
    const key = normalize(value);
    if (!key || key === 'world') return countryProfiles.world;
    return Object.values(countryProfiles).find((profile) => normalize(profile.label) === key) ||
      Object.values(countryProfiles).find((profile) => normalize(profile.label).startsWith(key)) ||
      { label: value.trim(), stores: countryProfiles.world.stores, resale: countryProfiles.world.resale };
  }

  function countryProfile() {
    return selectedScope() === 'World' ? countryProfiles.world : resolveCountry(countryInput?.value || 'Portugal');
  }

  function categoryProfile() {
    const value = normalize(categoryInput?.value || '');
    return categoryProfiles.find((profile) => profile.aliases.some((alias) => value.includes(alias) || alias.includes(value))) ||
      categoryProfiles[0];
  }

  function suggestionsFor(type) {
    const geo = countryProfile();
    const category = categoryProfile();
    return type === 'trusted'
      ? unique([...category.stores, ...geo.stores]).slice(0, 10)
      : unique([...category.resale, ...geo.resale]).slice(0, 10);
  }

  function setValue(type) {
    const field = root.querySelector(`[data-market-value="${type}"]`);
    if (field) {
      field.value = selected[type].join(', ');
      field.textContent = field.value;
    }
  }

  function setSelected(type, items, markDirty) {
    selected[type] = unique(items);
    if (markDirty) dirty[type] = true;
    setValue(type);
    renderSelected(type);
    renderSuggestionChips(type);
  }

  function renderSelected(type) {
    const container = root.querySelector(`[data-market-selected="${type}"]`);
    if (!container) return;

    container.replaceChildren();
    if (!selected[type].length) {
      const empty = document.createElement('span');
      empty.className = 'market-selected-empty';
      empty.textContent = type === 'trusted' ? 'No trusted stores selected yet.' : 'No resale sites selected yet.';
      container.appendChild(empty);
      return;
    }

    selected[type].forEach((item) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'market-selected-chip';
      button.setAttribute('aria-label', `Remove ${item}`);
      button.textContent = item;
      const icon = document.createElement('i');
      icon.className = 'bi bi-x-lg';
      button.appendChild(icon);
      button.addEventListener('click', () => {
        setSelected(type, selected[type].filter((candidate) => normalize(candidate) !== normalize(item)), true);
      });
      container.appendChild(button);
    });
  }

  function renderSuggestionChips(type) {
    const container = root.querySelector(`[data-market-suggestions="${type}"]`);
    if (!container) return;

    container.replaceChildren();
    suggestionsFor(type).forEach((item) => {
      const active = selected[type].some((candidate) => normalize(candidate) === normalize(item));
      const button = document.createElement('button');
      button.type = 'button';
      button.className = `market-suggestion-chip${active ? ' active' : ''}`;
      button.textContent = item;
      button.addEventListener('click', () => {
        const next = active
          ? selected[type].filter((candidate) => normalize(candidate) !== normalize(item))
          : [...selected[type], item];
        setSelected(type, next, true);
      });
      container.appendChild(button);
    });
  }

  function applySuggestions(type, markDirty) {
    setSelected(type, suggestionsFor(type), markDirty);
  }

  function optionScore(label, query) {
    const haystack = normalize(label);
    const needle = normalize(query);
    if (!needle) return 1;
    if (haystack === needle) return 0;
    if (haystack.startsWith(needle)) return 1;
    if (haystack.includes(needle)) return 2;
    return 99;
  }

  function renderMenu(kind, items, onPick) {
    const menu = root.querySelector(`[data-market-menu="${kind}"]`);
    if (!menu) return;

    menu.replaceChildren();
    items.slice(0, 8).forEach((item) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'market-autocomplete-option';
      const strong = document.createElement('strong');
      strong.textContent = item.label;
      const small = document.createElement('small');
      small.textContent = item.meta || '';
      button.append(strong, small);
      button.addEventListener('mousedown', (event) => event.preventDefault());
      button.addEventListener('click', () => {
        onPick(item);
        closeMenus();
      });
      menu.appendChild(button);
    });
    menu.hidden = items.length === 0;
  }

  function closeMenus() {
    root.querySelectorAll('[data-market-menu]').forEach((menu) => {
      menu.hidden = true;
    });
  }

  function refreshSuggestions() {
    if (!dirty.trusted) applySuggestions('trusted', false);
    if (!dirty.resale) applySuggestions('resale', false);
    renderSuggestionChips('trusted');
    renderSuggestionChips('resale');
    updateSummary();
  }

  function updateSummary() {
    const scope = selectedScope();
    const country = scope === 'World' ? 'worldwide' : countryProfile().label;
    const city = cityInput?.value.trim();
    const category = categoryInput?.value.trim() || categoryProfile().label;
    const place = scope === 'World'
      ? 'worldwide'
      : scope === 'Country'
        ? country
        : [city, country].filter(Boolean).join(', ');
    const radius = radiusKm();

    if (suggestionSummary) {
      suggestionSummary.textContent = scope === 'Custom'
        ? `Suggestions are based on ${place || country}, ${category}, and a ${radius} km radius.`
        : `Suggestions are based on ${place || country} and ${category}.`;
    }
    if (areaSummary) {
      areaSummary.textContent = scope === 'Custom'
        ? `${place || 'Custom area'} within ${radius} km`
        : place || 'Worldwide';
    }
    if (coordinates) {
      const lat = numberValue(latInput);
      const lng = numberValue(lngInput);
      coordinates.textContent = lat !== null && lng !== null
        ? `${lat.toFixed(5)}, ${lng.toFixed(5)}`
        : 'No map point selected';
    }
    if (radiusLabel) radiusLabel.textContent = `${radius} km`;
  }

  function setPanels() {
    const scope = selectedScope();
    if (countryPanel) countryPanel.hidden = scope === 'World';
    if (cityPanel) cityPanel.hidden = scope === 'World' || scope === 'Country';
    if (mapPanel) mapPanel.hidden = scope !== 'Custom';

    if (scope === 'World' && countryInput) countryInput.value = 'World';
    if (scope !== 'World' && countryInput && normalize(countryInput.value) === 'world') countryInput.value = 'Portugal';

    if (scope === 'Custom') {
      ensureMap();
      applySelectedCityToMap();
      setTimeout(() => {
        if (map) map.invalidateSize();
      }, 60);
    }
    refreshSuggestions();
  }

  function countryOptions(query) {
    return Object.values(countryProfiles)
      .filter((profile) => optionScore(profile.label, query) < 99)
      .map((profile) => ({ label: profile.label, value: profile.label, meta: 'Country' }))
      .sort((first, second) => optionScore(first.label, query) - optionScore(second.label, query));
  }

  function citySuggestions(query) {
    const country = selectedScope() === 'World' ? '' : countryProfile().label;
    return cityOptions
      .filter((entry) => !country || entry.country === country)
      .filter((entry) => optionScore(`${entry.city}, ${entry.country}`, query) < 99)
      .map((entry) => ({ label: entry.city, value: entry.city, meta: entry.country, entry }))
      .sort((first, second) => optionScore(`${first.label}, ${first.meta}`, query) - optionScore(`${second.label}, ${second.meta}`, query));
  }

  function categorySuggestions(query) {
    return categoryProfiles
      .filter((profile) => optionScore(profile.label, query) < 99 || profile.aliases.some((alias) => optionScore(alias, query) < 99))
      .map((profile) => ({ label: profile.label, value: profile.label, meta: 'Category' }));
  }

  function marketAutocompleteOptions(type, query) {
    const pool = unique([
      ...suggestionsFor(type),
      ...Object.values(countryProfiles).flatMap((profile) => type === 'trusted' ? profile.stores : profile.resale),
      ...categoryProfiles.flatMap((profile) => type === 'trusted' ? profile.stores : profile.resale)
    ]);
    return pool
      .filter((item) => optionScore(item, query) < 99)
      .map((item) => ({ label: item, value: item, meta: type === 'trusted' ? 'Trusted store' : 'Resale site' }))
      .sort((first, second) => optionScore(first.label, query) - optionScore(second.label, query));
  }

  function bindAutocomplete(input, kind, suggestions, onPick) {
    if (!input) return;
    const render = () => renderMenu(kind, suggestions(input.value), onPick);
    input.addEventListener('focus', render);
    input.addEventListener('input', render);
    input.addEventListener('blur', () => setTimeout(closeMenus, 120));
  }

  function addCustom(type) {
    const input = root.querySelector(`[data-market-autocomplete="${type}"]`);
    const value = input?.value.trim();
    if (!value) return;
    setSelected(type, [...selected[type], value], true);
    input.value = '';
    closeMenus();
  }

  function numberValue(input) {
    const parsed = Number.parseFloat(input?.value || '');
    return Number.isFinite(parsed) ? parsed : null;
  }

  function radiusKm() {
    const parsed = Number.parseInt(radiusInput?.value || '25', 10);
    return Number.isFinite(parsed) ? Math.max(1, Math.min(parsed, 1000)) : 25;
  }

  function setPoint(lat, lng, moveMap) {
    if (latInput) latInput.value = lat.toFixed(6);
    if (lngInput) lngInput.value = lng.toFixed(6);
    if (!map) {
      updateSummary();
      return;
    }
    const point = [lat, lng];
    if (!marker) {
      marker = L.marker(point, { draggable: true }).addTo(map);
      marker.on('dragend', () => {
        const current = marker.getLatLng();
        setPoint(current.lat, current.lng, false);
      });
    } else {
      marker.setLatLng(point);
    }
    updateCircle();
    if (moveMap) map.setView(point, Math.max(map.getZoom(), 10));
    updateSummary();
  }

  function updateCircle() {
    if (!map) return;
    const lat = numberValue(latInput);
    const lng = numberValue(lngInput);
    if (lat === null || lng === null) return;
    const point = [lat, lng];
    const meters = radiusKm() * 1000;
    if (!circle) {
      circle = L.circle(point, {
        radius: meters,
        color: '#22c55e',
        weight: 2,
        fillColor: '#38bdf8',
        fillOpacity: 0.14
      }).addTo(map);
    } else {
      circle.setLatLng(point);
      circle.setRadius(meters);
    }
  }

  function findSelectedCity() {
    const city = normalize(cityInput?.value || '');
    const country = countryProfile().label;
    return cityOptions.find((entry) => normalize(entry.city) === city && entry.country === country) ||
      cityOptions.find((entry) => normalize(entry.city) === city);
  }

  function applySelectedCityToMap() {
    const selectedCity = findSelectedCity();
    if (selectedCity) setPoint(selectedCity.lat, selectedCity.lng, true);
  }

  function nearestCity(lat, lng) {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    cityOptions.forEach((entry) => {
      const distance = Math.hypot(entry.lat - lat, entry.lng - lng);
      if (distance < bestDistance) {
        best = entry;
        bestDistance = distance;
      }
    });
    return best;
  }

  function ensureMap() {
    if (map || !mapEl) return;
    if (!window.L) {
      mapEl.classList.add('market-map-unavailable');
      mapEl.textContent = 'Map unavailable. You can still type a city and radius.';
      return;
    }

    const lat = numberValue(latInput) ?? 38.7223;
    const lng = numberValue(lngInput) ?? -9.1393;
    map = L.map(mapEl, { scrollWheelZoom: false }).setView([lat, lng], 8);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);
    map.on('click', (event) => {
      const picked = nearestCity(event.latlng.lat, event.latlng.lng);
      if (picked) {
        if (countryInput) countryInput.value = picked.country;
        if (cityInput) cityInput.value = picked.city;
      }
      setPoint(event.latlng.lat, event.latlng.lng, false);
      refreshSuggestions();
    });
    setPoint(lat, lng, false);
  }

  bindAutocomplete(countryInput, 'country', countryOptions, (item) => {
    countryInput.value = item.value;
    if (selectedScope() === 'Country') cityInput.value = '';
    refreshSuggestions();
  });
  bindAutocomplete(cityInput, 'city', citySuggestions, (item) => {
    cityInput.value = item.value;
    if (countryInput) countryInput.value = item.entry.country;
    if (selectedScope() === 'Custom') setPoint(item.entry.lat, item.entry.lng, true);
    refreshSuggestions();
  });
  bindAutocomplete(categoryInput, 'category', categorySuggestions, (item) => {
    categoryInput.value = item.value;
    refreshSuggestions();
  });

  ['trusted', 'resale'].forEach((type) => {
    bindAutocomplete(
      root.querySelector(`[data-market-autocomplete="${type}"]`),
      type,
      (query) => marketAutocompleteOptions(type, query),
      (item) => {
        setSelected(type, [...selected[type], item.value], true);
        const input = root.querySelector(`[data-market-autocomplete="${type}"]`);
        if (input) input.value = '';
      });
    root.querySelector(`[data-market-add="${type}"]`)?.addEventListener('click', () => addCustom(type));
    root.querySelector(`[data-market-autocomplete="${type}"]`)?.addEventListener('keydown', (event) => {
      if (event.key === 'Enter') {
        event.preventDefault();
        addCustom(type);
      }
    });
    root.querySelector(`[data-market-apply="${type}"]`)?.addEventListener('click', () => applySuggestions(type, true));
  });

  scopeInputs.forEach((input) => input.addEventListener('change', setPanels));
  [countryInput, cityInput, categoryInput].forEach((input) => {
    input?.addEventListener('change', refreshSuggestions);
    input?.addEventListener('input', () => {
      if (input === cityInput && selectedScope() === 'Custom') applySelectedCityToMap();
      refreshSuggestions();
    });
  });
  radiusInput?.addEventListener('input', () => {
    if (radiusRange) radiusRange.value = Math.min(radiusKm(), Number.parseInt(radiusRange.max || '250', 10)).toString();
    updateCircle();
    updateSummary();
  });
  radiusRange?.addEventListener('input', () => {
    if (radiusInput) radiusInput.value = radiusRange.value;
    updateCircle();
    updateSummary();
  });
  document.addEventListener('click', (event) => {
    if (!root.contains(event.target)) closeMenus();
  });

  setValue('trusted');
  setValue('resale');
  setPanels();
})();
