(() => {
  const root = document.querySelector('[data-location-targeting]');
  if (!root) return;

  const scopeInputs = Array.from(root.querySelectorAll('input[name="Input.AudienceLocationScope"]'));
  const countryInput = root.querySelector('[data-location-country]');
  const cityInput = root.querySelector('[data-location-city]');
  const countryList = root.querySelector('[data-location-country-list]');
  const cityList = root.querySelector('[data-location-city-list]');
  const countryMenu = root.querySelector('[data-location-country-menu]');
  const cityMenu = root.querySelector('[data-location-city-menu]');
  const radiusInput = root.querySelector('[data-location-radius]');
  const radiusRange = root.querySelector('[data-location-radius-range]');
  const latInput = root.querySelector('[data-location-lat]');
  const lngInput = root.querySelector('[data-location-lng]');
  const summary = root.querySelector('[data-location-summary]');
  const coordinates = root.querySelector('[data-location-coordinates]');
  const mapEl = root.querySelector('[data-location-map]');
  const countryPanels = Array.from(root.querySelectorAll('[data-location-panel="country"]'));
  const cityPanels = Array.from(root.querySelectorAll('[data-location-panel="city"]'));

  let map = null;
  let marker = null;
  let circle = null;

  const countryOptions = [
    'Portugal',
    'Spain',
    'France',
    'United Kingdom',
    'Ireland',
    'Germany',
    'Italy',
    'Netherlands',
    'Belgium',
    'Switzerland',
    'Austria',
    'Denmark',
    'Sweden',
    'Norway',
    'Finland',
    'Poland',
    'Czech Republic',
    'United States',
    'Canada',
    'Brazil',
    'Mexico',
    'Argentina',
    'Chile',
    'Colombia',
    'Angola',
    'Mozambique',
    'South Africa',
    'United Arab Emirates',
    'India',
    'China',
    'Japan',
    'Australia'
  ];

  const cityOptions = [
    { city: 'Lisbon', country: 'Portugal', lat: 38.7223, lng: -9.1393 },
    { city: 'Porto', country: 'Portugal', lat: 41.1579, lng: -8.6291 },
    { city: 'Braga', country: 'Portugal', lat: 41.5454, lng: -8.4265 },
    { city: 'Coimbra', country: 'Portugal', lat: 40.2033, lng: -8.4103 },
    { city: 'Aveiro', country: 'Portugal', lat: 40.6405, lng: -8.6538 },
    { city: 'Faro', country: 'Portugal', lat: 37.0194, lng: -7.9304 },
    { city: 'Funchal', country: 'Portugal', lat: 32.6669, lng: -16.9241 },
    { city: 'Madrid', country: 'Spain', lat: 40.4168, lng: -3.7038 },
    { city: 'Barcelona', country: 'Spain', lat: 41.3874, lng: 2.1686 },
    { city: 'Valencia', country: 'Spain', lat: 39.4699, lng: -0.3763 },
    { city: 'Seville', country: 'Spain', lat: 37.3891, lng: -5.9845 },
    { city: 'Paris', country: 'France', lat: 48.8566, lng: 2.3522 },
    { city: 'Lyon', country: 'France', lat: 45.764, lng: 4.8357 },
    { city: 'London', country: 'United Kingdom', lat: 51.5072, lng: -0.1276 },
    { city: 'Manchester', country: 'United Kingdom', lat: 53.4808, lng: -2.2426 },
    { city: 'Dublin', country: 'Ireland', lat: 53.3498, lng: -6.2603 },
    { city: 'Berlin', country: 'Germany', lat: 52.52, lng: 13.405 },
    { city: 'Munich', country: 'Germany', lat: 48.1351, lng: 11.582 },
    { city: 'Rome', country: 'Italy', lat: 41.9028, lng: 12.4964 },
    { city: 'Milan', country: 'Italy', lat: 45.4642, lng: 9.19 },
    { city: 'Amsterdam', country: 'Netherlands', lat: 52.3676, lng: 4.9041 },
    { city: 'Brussels', country: 'Belgium', lat: 50.8503, lng: 4.3517 },
    { city: 'Zurich', country: 'Switzerland', lat: 47.3769, lng: 8.5417 },
    { city: 'Vienna', country: 'Austria', lat: 48.2082, lng: 16.3738 },
    { city: 'Copenhagen', country: 'Denmark', lat: 55.6761, lng: 12.5683 },
    { city: 'Stockholm', country: 'Sweden', lat: 59.3293, lng: 18.0686 },
    { city: 'Oslo', country: 'Norway', lat: 59.9139, lng: 10.7522 },
    { city: 'Helsinki', country: 'Finland', lat: 60.1699, lng: 24.9384 },
    { city: 'Warsaw', country: 'Poland', lat: 52.2297, lng: 21.0122 },
    { city: 'Prague', country: 'Czech Republic', lat: 50.0755, lng: 14.4378 },
    { city: 'New York', country: 'United States', lat: 40.7128, lng: -74.006 },
    { city: 'Los Angeles', country: 'United States', lat: 34.0522, lng: -118.2437 },
    { city: 'San Francisco', country: 'United States', lat: 37.7749, lng: -122.4194 },
    { city: 'Miami', country: 'United States', lat: 25.7617, lng: -80.1918 },
    { city: 'Toronto', country: 'Canada', lat: 43.6532, lng: -79.3832 },
    { city: 'Vancouver', country: 'Canada', lat: 49.2827, lng: -123.1207 },
    { city: 'Sao Paulo', country: 'Brazil', lat: -23.5558, lng: -46.6396 },
    { city: 'Rio de Janeiro', country: 'Brazil', lat: -22.9068, lng: -43.1729 },
    { city: 'Mexico City', country: 'Mexico', lat: 19.4326, lng: -99.1332 },
    { city: 'Buenos Aires', country: 'Argentina', lat: -34.6037, lng: -58.3816 },
    { city: 'Santiago', country: 'Chile', lat: -33.4489, lng: -70.6693 },
    { city: 'Bogota', country: 'Colombia', lat: 4.711, lng: -74.0721 },
    { city: 'Luanda', country: 'Angola', lat: -8.839, lng: 13.2894 },
    { city: 'Maputo', country: 'Mozambique', lat: -25.9692, lng: 32.5732 },
    { city: 'Johannesburg', country: 'South Africa', lat: -26.2041, lng: 28.0473 },
    { city: 'Dubai', country: 'United Arab Emirates', lat: 25.2048, lng: 55.2708 },
    { city: 'Mumbai', country: 'India', lat: 19.076, lng: 72.8777 },
    { city: 'Delhi', country: 'India', lat: 28.7041, lng: 77.1025 },
    { city: 'Shanghai', country: 'China', lat: 31.2304, lng: 121.4737 },
    { city: 'Tokyo', country: 'Japan', lat: 35.6762, lng: 139.6503 },
    { city: 'Sydney', country: 'Australia', lat: -33.8688, lng: 151.2093 },
    { city: 'Melbourne', country: 'Australia', lat: -37.8136, lng: 144.9631 }
  ];

  function selectedScope() {
    const checked = scopeInputs.find((input) => input.checked);
    return checked ? checked.value : 'World';
  }

  function normalizeText(value) {
    return (value || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  function createOption(value, label) {
    const option = document.createElement('option');
    option.value = value;
    if (label) option.label = label;
    return option;
  }

  function optionScore(label, query) {
    const normalizedLabel = normalizeText(label);
    const normalizedQuery = normalizeText(query);
    if (!normalizedQuery) return 1;
    if (normalizedLabel === normalizedQuery) return 0;
    if (normalizedLabel.startsWith(normalizedQuery)) return 1;
    if (normalizedLabel.includes(normalizedQuery)) return 2;
    return 99;
  }

  function sortSuggestions(items, query, labelSelector) {
    return [...items].sort((first, second) => {
      const firstScore = optionScore(labelSelector(first), query);
      const secondScore = optionScore(labelSelector(second), query);
      if (firstScore !== secondScore) return firstScore - secondScore;
      return labelSelector(first).localeCompare(labelSelector(second));
    });
  }

  function clearMenu(menu) {
    if (!menu) return;
    menu.replaceChildren();
    menu.hidden = true;
  }

  function renderMenu(menu, suggestions, onPick) {
    if (!menu) return;

    menu.replaceChildren();
    if (!suggestions.length) {
      clearMenu(menu);
      return;
    }

    suggestions.slice(0, 8).forEach((suggestion) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'planner-location-autocomplete-option';
      button.innerHTML = `<strong>${suggestion.label}</strong><small>${suggestion.meta}</small>`;
      button.addEventListener('mousedown', (event) => event.preventDefault());
      button.addEventListener('click', () => onPick(suggestion));
      menu.appendChild(button);
    });

    menu.hidden = false;
  }

  function renderCountryAutocomplete() {
    if (!countryInput || !countryMenu) return;

    const query = countryInput.value;
    const normalizedQuery = normalizeText(query);
    const matches = countryOptions
      .filter((country) => !normalizedQuery || optionScore(country, query) < 99)
      .map((country) => ({ value: country, label: country, meta: 'Pais' }));

    renderMenu(countryMenu, sortSuggestions(matches, query, (item) => item.label), (suggestion) => {
      countryInput.value = suggestion.value;
      populateCityOptions();
      clearMenu(countryMenu);
      updateSummary();
    });
  }

  function renderCityAutocomplete() {
    if (!cityInput || !cityMenu) return;

    const query = cityInput.value;
    const normalizedQuery = normalizeText(query);
    const selectedCountry = resolveCountry(countryInput && countryInput.value);
    const source = selectedCountry
      ? cityOptions.filter((entry) => entry.country === selectedCountry)
      : cityOptions;
    const matches = source
      .filter((entry) =>
        !normalizedQuery ||
        optionScore(entry.city, query) < 99 ||
        optionScore(`${entry.city}, ${entry.country}`, query) < 99)
      .map((entry) => ({
        value: entry.city,
        label: entry.city,
        meta: entry.country,
        entry
      }));

    renderMenu(cityMenu, sortSuggestions(matches, query, (item) => `${item.label}, ${item.meta}`), (suggestion) => {
      cityInput.value = suggestion.value;
      if (countryInput) countryInput.value = suggestion.entry.country;
      populateCityOptions();
      clearMenu(cityMenu);
      applySelectedCity();
    });
  }

  function resolveCountry(value) {
    const normalized = normalizeText(value);
    if (!normalized) return '';

    const exact = countryOptions.find((country) => normalizeText(country) === normalized);
    if (exact) return exact;

    return countryOptions.find((country) => normalizeText(country).startsWith(normalized)) || '';
  }

  function populateCountryOptions() {
    if (!countryList) return;
    countryList.replaceChildren(...countryOptions.map((country) => createOption(country)));
  }

  function populateCityOptions() {
    if (!cityList) return;

    const selectedCountry = resolveCountry(countryInput && countryInput.value);
    const cities = selectedCountry
      ? cityOptions.filter((entry) => entry.country === selectedCountry)
      : cityOptions;

    cityList.replaceChildren(
      ...cities
        .slice(0, 120)
        .map((entry) => createOption(entry.city, entry.country))
    );
  }

  function findSelectedCity() {
    const city = normalizeText(cityInput && cityInput.value);
    if (!city) return null;

    const selectedCountry = resolveCountry(countryInput && countryInput.value);
    return cityOptions.find((entry) =>
      normalizeText(entry.city) === city &&
      (!selectedCountry || entry.country === selectedCountry));
  }

  function applySelectedCity() {
    const selected = findSelectedCity();
    if (!selected) {
      updateSummary();
      return;
    }

    if (countryInput && countryInput.value.trim() !== selected.country) {
      countryInput.value = selected.country;
      populateCityOptions();
    }

    if (selectedScope() === 'City') {
      ensureMap();
      setPoint(selected.lat, selected.lng, true);
    } else {
      updateSummary();
    }
  }

  function numberValue(input) {
    const parsed = Number.parseFloat(input && input.value ? input.value : '');
    return Number.isFinite(parsed) ? parsed : null;
  }

  function radiusKm() {
    const parsed = Number.parseInt(radiusInput && radiusInput.value ? radiusInput.value : '25', 10);
    return Number.isFinite(parsed) ? Math.max(1, Math.min(parsed, 1000)) : 25;
  }

  function locationLabel() {
    const scope = selectedScope();
    const country = countryInput && countryInput.value ? countryInput.value.trim() : '';
    const city = cityInput && cityInput.value ? cityInput.value.trim() : '';

    if (scope === 'Country') return country || 'Country campaign';
    if (scope === 'City') {
      const place = [city, country].filter(Boolean).join(', ') || 'Selected city';
      return `${place} within ${radiusKm()} km`;
    }

    return 'Worldwide campaign';
  }

  function updateSummary() {
    if (summary) summary.textContent = locationLabel();

    const lat = numberValue(latInput);
    const lng = numberValue(lngInput);
    if (!coordinates) return;

    if (selectedScope() === 'City' && lat !== null && lng !== null) {
      coordinates.textContent = `${lat.toFixed(5)}, ${lng.toFixed(5)} - ${radiusKm()} km radius`;
    } else {
      coordinates.textContent = 'Nenhum ponto selecionado.';
    }
  }

  function setPanels() {
    const scope = selectedScope();
    countryPanels.forEach((panel) => {
      panel.hidden = scope === 'World';
    });
    cityPanels.forEach((panel) => {
      panel.hidden = scope !== 'City';
    });

    if (scope === 'City') {
      ensureMap();
      setTimeout(() => {
        if (map) map.invalidateSize();
      }, 50);
    }

    updateSummary();
  }

  function syncRadiusFromInput() {
    const value = radiusKm();
    if (radiusInput) radiusInput.value = value.toString();
    if (radiusRange) radiusRange.value = Math.min(value, Number.parseInt(radiusRange.max || '250', 10)).toString();
    updateCircle();
    updateSummary();
  }

  function syncRadiusFromRange() {
    if (!radiusRange || !radiusInput) return;
    radiusInput.value = radiusRange.value;
    updateCircle();
    updateSummary();
  }

  function setPoint(lat, lng, moveMap) {
    if (!latInput || !lngInput) return;

    latInput.value = lat.toFixed(6);
    lngInput.value = lng.toFixed(6);

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
    if (!map || !latInput || !lngInput) return;

    const lat = numberValue(latInput);
    const lng = numberValue(lngInput);
    if (lat === null || lng === null) return;

    const point = [lat, lng];
    const meters = radiusKm() * 1000;
    if (!circle) {
      circle = L.circle(point, {
        radius: meters,
        color: '#84cc16',
        weight: 2,
        fillColor: '#22d3ee',
        fillOpacity: 0.14
      }).addTo(map);
    } else {
      circle.setLatLng(point);
      circle.setRadius(meters);
    }
  }

  function ensureMap() {
    if (map || !mapEl || !window.L) return;

    const lat = numberValue(latInput);
    const lng = numberValue(lngInput);
    const start = lat !== null && lng !== null ? [lat, lng] : [38.7223, -9.1393];
    const zoom = lat !== null && lng !== null ? 11 : 6;

    map = L.map(mapEl, {
      scrollWheelZoom: false,
      attributionControl: true
    }).setView(start, zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    map.on('click', (event) => {
      setPoint(event.latlng.lat, event.latlng.lng, false);
    });

    if (lat !== null && lng !== null) {
      setPoint(lat, lng, false);
    }
  }

  scopeInputs.forEach((input) => input.addEventListener('change', setPanels));
  if (countryInput) {
    countryInput.addEventListener('focus', renderCountryAutocomplete);
    countryInput.addEventListener('input', () => {
      populateCityOptions();
      renderCountryAutocomplete();
      updateSummary();
    });
    countryInput.addEventListener('change', () => {
      populateCityOptions();
      applySelectedCity();
      renderCityAutocomplete();
      updateSummary();
    });
    countryInput.addEventListener('blur', () => {
      setTimeout(() => clearMenu(countryMenu), 120);
    });
  }
  if (cityInput) {
    cityInput.addEventListener('focus', renderCityAutocomplete);
    cityInput.addEventListener('input', () => {
      renderCityAutocomplete();
      applySelectedCity();
    });
    cityInput.addEventListener('change', () => {
      applySelectedCity();
      clearMenu(cityMenu);
    });
    cityInput.addEventListener('blur', () => {
      setTimeout(() => clearMenu(cityMenu), 120);
    });
  }
  if (radiusInput) radiusInput.addEventListener('input', syncRadiusFromInput);
  if (radiusRange) radiusRange.addEventListener('input', syncRadiusFromRange);
  document.addEventListener('click', (event) => {
    if (root.contains(event.target)) return;
    clearMenu(countryMenu);
    clearMenu(cityMenu);
  });

  if (!window.L && mapEl) {
    mapEl.classList.add('planner-map-unavailable');
    mapEl.textContent = 'Map unavailable. You can still type a city and radius.';
  }

  populateCountryOptions();
  populateCityOptions();
  setPanels();
})();
