(() => {
  const root = document.querySelector('[data-service-location-selector]');
  if (!root) return;

  const scopeInputs = Array.from(root.querySelectorAll('[data-service-location-scope]'));
  const locationValue = root.querySelector('[data-service-location-value]');
  const countryInput = root.querySelector('[data-service-country]');
  const placeInput = root.querySelector('[data-service-place]');
  const countryMenu = root.querySelector('[data-service-country-menu]');
  const placeMenu = root.querySelector('[data-service-place-menu]');
  const countryPanel = root.querySelector('[data-service-location-panel="country"]');
  const regionPanels = Array.from(root.querySelectorAll('[data-service-location-panel="region"]'));
  const mapPanel = root.querySelector('[data-service-map-panel]');
  const mapEl = root.querySelector('[data-service-map]');
  const latInput = root.querySelector('[data-service-lat]');
  const lngInput = root.querySelector('[data-service-lng]');
  const radiusInput = root.querySelector('[data-service-radius]');
  const radiusRange = root.querySelector('[data-service-radius-range]');
  const radiusLabel = root.querySelector('[data-service-radius-label]');
  const mapRadiusLabel = root.querySelector('[data-service-map-radius]');
  const summary = root.querySelector('[data-service-location-summary]');
  const coordinates = root.querySelector('[data-service-location-coordinates]');
  const locateButton = root.querySelector('[data-service-current-location]');
  const locationStatus = root.querySelector('[data-service-location-status]');

  const countries = [
    { label: 'Portugal', lat: 39.3999, lng: -8.2245, zoom: 6 },
    { label: 'Spain', lat: 40.4637, lng: -3.7492, zoom: 5 },
    { label: 'France', lat: 46.2276, lng: 2.2137, zoom: 5 },
    { label: 'United Kingdom', lat: 55.3781, lng: -3.436, zoom: 5 },
    { label: 'Germany', lat: 51.1657, lng: 10.4515, zoom: 5 },
    { label: 'Italy', lat: 41.8719, lng: 12.5674, zoom: 5 },
    { label: 'Netherlands', lat: 52.1326, lng: 5.2913, zoom: 7 },
    { label: 'Belgium', lat: 50.5039, lng: 4.4699, zoom: 7 },
    { label: 'Switzerland', lat: 46.8182, lng: 8.2275, zoom: 7 },
    { label: 'Ireland', lat: 53.1424, lng: -7.6921, zoom: 6 },
    { label: 'United States', lat: 37.0902, lng: -95.7129, zoom: 4 },
    { label: 'Canada', lat: 56.1304, lng: -106.3468, zoom: 4 },
    { label: 'Brazil', lat: -14.235, lng: -51.9253, zoom: 4 }
  ];

  const places = [
    { label: 'Abrantes', country: 'Portugal', lat: 39.4667, lng: -8.2 },
    { label: 'Agueda', country: 'Portugal', lat: 40.5772, lng: -8.4444 },
    { label: 'Albufeira', country: 'Portugal', lat: 37.0891, lng: -8.2479 },
    { label: 'Alcacer do Sal', country: 'Portugal', lat: 38.3733, lng: -8.5144 },
    { label: 'Alcobaca', country: 'Portugal', lat: 39.5522, lng: -8.9775 },
    { label: 'Alenquer', country: 'Portugal', lat: 39.0532, lng: -9.0093 },
    { label: 'Alentejo', country: 'Portugal', lat: 38.0, lng: -7.9 },
    { label: 'Algarve', country: 'Portugal', lat: 37.0179, lng: -7.9308 },
    { label: 'Almada', country: 'Portugal', lat: 38.6765, lng: -9.1651 },
    { label: 'Amadora', country: 'Portugal', lat: 38.7578, lng: -9.2244 },
    { label: 'Amarante', country: 'Portugal', lat: 41.2727, lng: -8.0825 },
    { label: 'Anadia', country: 'Portugal', lat: 40.4384, lng: -8.4335 },
    { label: 'Angra do Heroismo', country: 'Portugal', lat: 38.6548, lng: -27.2173 },
    { label: 'Aveiro', country: 'Portugal', lat: 40.6405, lng: -8.6538 },
    { label: 'Azores', country: 'Portugal', lat: 37.7412, lng: -25.6756 },
    { label: 'Barcelos', country: 'Portugal', lat: 41.5388, lng: -8.6151 },
    { label: 'Barreiro', country: 'Portugal', lat: 38.6631, lng: -9.0724 },
    { label: 'Beja', country: 'Portugal', lat: 38.0151, lng: -7.8632 },
    { label: 'Braga', country: 'Portugal', lat: 41.5454, lng: -8.4265 },
    { label: 'Braganca', country: 'Portugal', lat: 41.8061, lng: -6.7567 },
    { label: 'Caldas da Rainha', country: 'Portugal', lat: 39.4033, lng: -9.1384 },
    { label: 'Camara de Lobos', country: 'Portugal', lat: 32.6504, lng: -16.9772 },
    { label: 'Cascais', country: 'Portugal', lat: 38.6979, lng: -9.4215 },
    { label: 'Castelo Branco', country: 'Portugal', lat: 39.8197, lng: -7.4965 },
    { label: 'Centro', country: 'Portugal', lat: 40.2056, lng: -8.4196 },
    { label: 'Chaves', country: 'Portugal', lat: 41.7402, lng: -7.4688 },
    { label: 'Coimbra', country: 'Portugal', lat: 40.2033, lng: -8.4103 },
    { label: 'Covilha', country: 'Portugal', lat: 40.286, lng: -7.503 },
    { label: 'Elvas', country: 'Portugal', lat: 38.8815, lng: -7.1635 },
    { label: 'Entroncamento', country: 'Portugal', lat: 39.4667, lng: -8.4667 },
    { label: 'Ermesinde', country: 'Portugal', lat: 41.2165, lng: -8.5532 },
    { label: 'Espinho', country: 'Portugal', lat: 41.0076, lng: -8.6413 },
    { label: 'Esposende', country: 'Portugal', lat: 41.5323, lng: -8.7813 },
    { label: 'Estarreja', country: 'Portugal', lat: 40.7546, lng: -8.5708 },
    { label: 'Evora', country: 'Portugal', lat: 38.5714, lng: -7.9135 },
    { label: 'Fafe', country: 'Portugal', lat: 41.4542, lng: -8.168 },
    { label: 'Faro', country: 'Portugal', lat: 37.0194, lng: -7.9304 },
    { label: 'Fatima', country: 'Portugal', lat: 39.6258, lng: -8.6659 },
    { label: 'Felgueiras', country: 'Portugal', lat: 41.3681, lng: -8.1939 },
    { label: 'Figueira da Foz', country: 'Portugal', lat: 40.1509, lng: -8.8618 },
    { label: 'Funchal', country: 'Portugal', lat: 32.6508, lng: -16.9089 },
    { label: 'Gondomar', country: 'Portugal', lat: 41.1445, lng: -8.5322 },
    { label: 'Guarda', country: 'Portugal', lat: 40.5373, lng: -7.2658 },
    { label: 'Guimaraes', country: 'Portugal', lat: 41.4444, lng: -8.2962 },
    { label: 'Lagos', country: 'Portugal', lat: 37.102, lng: -8.6742 },
    { label: 'Lamego', country: 'Portugal', lat: 41.0974, lng: -7.8099 },
    { label: 'Leiria', country: 'Portugal', lat: 39.7436, lng: -8.8071 },
    { label: 'Lisbon', country: 'Portugal', lat: 38.7223, lng: -9.1393 },
    { label: 'Loule', country: 'Portugal', lat: 37.1377, lng: -8.0197 },
    { label: 'Loures', country: 'Portugal', lat: 38.8309, lng: -9.1685 },
    { label: 'Lousada', country: 'Portugal', lat: 41.2782, lng: -8.2799 },
    { label: 'Madeira', country: 'Portugal', lat: 32.7607, lng: -16.9595 },
    { label: 'Maia', country: 'Portugal', lat: 41.2357, lng: -8.6199 },
    { label: 'Marco de Canaveses', country: 'Portugal', lat: 41.1839, lng: -8.1486 },
    { label: 'Marinha Grande', country: 'Portugal', lat: 39.7477, lng: -8.9323 },
    { label: 'Matosinhos', country: 'Portugal', lat: 41.1821, lng: -8.6891 },
    { label: 'Mealhada', country: 'Portugal', lat: 40.3781, lng: -8.4507 },
    { label: 'Mirandela', country: 'Portugal', lat: 41.4874, lng: -7.1869 },
    { label: 'Moncao', country: 'Portugal', lat: 42.0786, lng: -8.4808 },
    { label: 'Montijo', country: 'Portugal', lat: 38.7067, lng: -8.9739 },
    { label: 'Norte', country: 'Portugal', lat: 41.4993, lng: -8.2721 },
    { label: 'Odivelas', country: 'Portugal', lat: 38.7926, lng: -9.1838 },
    { label: 'Oeiras', country: 'Portugal', lat: 38.6971, lng: -9.3017 },
    { label: 'Olhao', country: 'Portugal', lat: 37.026, lng: -7.841 },
    { label: 'Oliveira de Azemeis', country: 'Portugal', lat: 40.841, lng: -8.4756 },
    { label: 'Oliveira do Hospital', country: 'Portugal', lat: 40.3618, lng: -7.8617 },
    { label: 'Ourem', country: 'Portugal', lat: 39.6417, lng: -8.5919 },
    { label: 'Paredes', country: 'Portugal', lat: 41.2077, lng: -8.3358 },
    { label: 'Penafiel', country: 'Portugal', lat: 41.2084, lng: -8.2829 },
    { label: 'Peniche', country: 'Portugal', lat: 39.3558, lng: -9.3811 },
    { label: 'Peso da Regua', country: 'Portugal', lat: 41.1621, lng: -7.7899 },
    { label: 'Ponta Delgada', country: 'Portugal', lat: 37.7394, lng: -25.6687 },
    { label: 'Ponte de Lima', country: 'Portugal', lat: 41.7672, lng: -8.5839 },
    { label: 'Portalegre', country: 'Portugal', lat: 39.292, lng: -7.4312 },
    { label: 'Portimao', country: 'Portugal', lat: 37.1366, lng: -8.5392 },
    { label: 'Porto', country: 'Portugal', lat: 41.1579, lng: -8.6291 },
    { label: 'Pombal', country: 'Portugal', lat: 39.9167, lng: -8.6285 },
    { label: 'Povoa de Varzim', country: 'Portugal', lat: 41.3804, lng: -8.7609 },
    { label: 'Queluz', country: 'Portugal', lat: 38.7566, lng: -9.2545 },
    { label: 'Rio Tinto', country: 'Portugal', lat: 41.1787, lng: -8.5593 },
    { label: 'Santa Maria da Feira', country: 'Portugal', lat: 40.9254, lng: -8.5428 },
    { label: 'Santarem', country: 'Portugal', lat: 39.2362, lng: -8.6859 },
    { label: 'Santo Tirso', country: 'Portugal', lat: 41.3426, lng: -8.4775 },
    { label: 'Sao Joao da Madeira', country: 'Portugal', lat: 40.9005, lng: -8.4908 },
    { label: 'Seixal', country: 'Portugal', lat: 38.6401, lng: -9.1014 },
    { label: 'Setubal', country: 'Portugal', lat: 38.5244, lng: -8.8882 },
    { label: 'Silves', country: 'Portugal', lat: 37.1892, lng: -8.4382 },
    { label: 'Sines', country: 'Portugal', lat: 37.9562, lng: -8.8698 },
    { label: 'Sintra', country: 'Portugal', lat: 38.8029, lng: -9.3817 },
    { label: 'Tavira', country: 'Portugal', lat: 37.1273, lng: -7.6486 },
    { label: 'Tomar', country: 'Portugal', lat: 39.6033, lng: -8.4092 },
    { label: 'Torres Novas', country: 'Portugal', lat: 39.4811, lng: -8.5395 },
    { label: 'Torres Vedras', country: 'Portugal', lat: 39.0911, lng: -9.2586 },
    { label: 'Trofa', country: 'Portugal', lat: 41.3373, lng: -8.5596 },
    { label: 'Valongo', country: 'Portugal', lat: 41.1888, lng: -8.4983 },
    { label: 'Viana do Castelo', country: 'Portugal', lat: 41.6918, lng: -8.8344 },
    { label: 'Vila do Conde', country: 'Portugal', lat: 41.3548, lng: -8.7434 },
    { label: 'Vila Franca de Xira', country: 'Portugal', lat: 38.9553, lng: -8.9897 },
    { label: 'Vila Nova de Famalicao', country: 'Portugal', lat: 41.407, lng: -8.5198 },
    { label: 'Vila Nova de Gaia', country: 'Portugal', lat: 41.1239, lng: -8.6118 },
    { label: 'Vila Real', country: 'Portugal', lat: 41.3006, lng: -7.7441 },
    { label: 'Vila Real de Santo Antonio', country: 'Portugal', lat: 37.195, lng: -7.4177 },
    { label: 'Vila Verde', country: 'Portugal', lat: 41.6473, lng: -8.4371 },
    { label: 'Viseu', country: 'Portugal', lat: 40.6566, lng: -7.9125 },
    { label: 'Madrid', country: 'Spain', lat: 40.4168, lng: -3.7038 },
    { label: 'Barcelona', country: 'Spain', lat: 41.3874, lng: 2.1686 },
    { label: 'Valencia', country: 'Spain', lat: 39.4699, lng: -0.3763 },
    { label: 'Seville', country: 'Spain', lat: 37.3891, lng: -5.9845 },
    { label: 'Paris', country: 'France', lat: 48.8566, lng: 2.3522 },
    { label: 'Lyon', country: 'France', lat: 45.764, lng: 4.8357 },
    { label: 'London', country: 'United Kingdom', lat: 51.5072, lng: -0.1276 },
    { label: 'Manchester', country: 'United Kingdom', lat: 53.4808, lng: -2.2426 },
    { label: 'Berlin', country: 'Germany', lat: 52.52, lng: 13.405 },
    { label: 'Munich', country: 'Germany', lat: 48.1351, lng: 11.582 },
    { label: 'Rome', country: 'Italy', lat: 41.9028, lng: 12.4964 },
    { label: 'Milan', country: 'Italy', lat: 45.4642, lng: 9.19 },
    { label: 'Amsterdam', country: 'Netherlands', lat: 52.3676, lng: 4.9041 },
    { label: 'Brussels', country: 'Belgium', lat: 50.8503, lng: 4.3517 },
    { label: 'Zurich', country: 'Switzerland', lat: 47.3769, lng: 8.5417 },
    { label: 'Dublin', country: 'Ireland', lat: 53.3498, lng: -6.2603 },
    { label: 'New York', country: 'United States', lat: 40.7128, lng: -74.006 },
    { label: 'Los Angeles', country: 'United States', lat: 34.0522, lng: -118.2437 },
    { label: 'San Francisco', country: 'United States', lat: 37.7749, lng: -122.4194 },
    { label: 'Toronto', country: 'Canada', lat: 43.6532, lng: -79.3832 },
    { label: 'Vancouver', country: 'Canada', lat: 49.2827, lng: -123.1207 },
    { label: 'Sao Paulo', country: 'Brazil', lat: -23.5558, lng: -46.6396 },
    { label: 'Rio de Janeiro', country: 'Brazil', lat: -22.9068, lng: -43.1729 }
  ];

  const currencyByCountry = {
    Portugal: 'EUR',
    Spain: 'EUR',
    France: 'EUR',
    Germany: 'EUR',
    Italy: 'EUR',
    Netherlands: 'EUR',
    Belgium: 'EUR',
    Ireland: 'EUR',
    'United Kingdom': 'GBP',
    Switzerland: 'CHF',
    'United States': 'USD',
    Canada: 'CAD',
    Brazil: 'BRL'
  };

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

  function optionScore(label, query) {
    const haystack = normalize(label);
    const needle = normalize(query);
    if (!needle) return 1;
    if (haystack === needle) return 0;
    if (haystack.startsWith(needle)) return 1;
    if (haystack.includes(needle)) return 2;
    return 99;
  }

  function compareOptions(query) {
    return (first, second) => {
      const firstScore = optionScore(`${first.label}, ${first.meta || ''}`, query);
      const secondScore = optionScore(`${second.label}, ${second.meta || ''}`, query);
      if (firstScore !== secondScore) return firstScore - secondScore;
      return normalize(first.label).localeCompare(normalize(second.label));
    };
  }

  function selectedScope() {
    return scopeInputs.find((input) => input.checked)?.value || 'Country';
  }

  function radiusKm() {
    const parsed = Number.parseInt(radiusInput?.value || '35', 10);
    return Number.isFinite(parsed) ? Math.max(1, Math.min(parsed, 500)) : 35;
  }

  function numberValue(input) {
    const parsed = Number.parseFloat(input?.value || '');
    return Number.isFinite(parsed) ? parsed : null;
  }

  function resolveCountry(value) {
    const key = normalize(value);
    return countries.find((country) => normalize(country.label) === key) ||
      countries.find((country) => normalize(country.label).startsWith(key)) ||
      countries[0];
  }

  function selectedCountry() {
    return resolveCountry(countryInput?.value || 'Portugal');
  }

  function selectedPlace() {
    const place = normalize(placeInput?.value || '');
    const country = selectedCountry().label;
    return places.find((entry) => normalize(entry.label) === place && entry.country === country) ||
      places.find((entry) => normalize(entry.label) === place);
  }

  function locationLabel() {
    const scope = selectedScope();
    if (scope === 'World') return 'Worldwide';
    if (scope === 'Country') return countryInput?.value.trim() || 'Country not selected';
    const parts = [placeInput?.value.trim(), countryInput?.value.trim()].filter(Boolean);
    const place = parts.length ? parts.join(', ') : 'Selected area';
    return `${place} within ${radiusKm()} km`;
  }

  function suggestedCurrency() {
    const scope = selectedScope();
    if (scope === 'World') return 'USD';
    return currencyByCountry[selectedCountry().label] || 'USD';
  }

  function notifyBudgetCurrency() {
    document.dispatchEvent(new CustomEvent('workvert:location-currency-suggestion', {
      detail: {
        currency: suggestedCurrency(),
        country: selectedScope() === 'World' ? 'World' : selectedCountry().label,
        scope: selectedScope()
      }
    }));
  }

  function updateSummary() {
    const label = locationLabel();
    if (locationValue) locationValue.value = label;
    if (summary) summary.textContent = label;
    if (radiusLabel) radiusLabel.textContent = `${radiusKm()} km`;
    if (mapRadiusLabel) mapRadiusLabel.textContent = `${radiusKm()} km radius`;

    const lat = numberValue(latInput);
    const lng = numberValue(lngInput);
    if (coordinates) {
      coordinates.textContent = lat !== null && lng !== null
        ? `${lat.toFixed(5)}, ${lng.toFixed(5)}`
        : 'No map point selected';
    }
  }

  function renderMenu(menu, items, onPick) {
    if (!menu) return;
    menu.replaceChildren();
    items.forEach((item) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'service-location-autocomplete-option';
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
    [countryMenu, placeMenu].forEach((menu) => {
      if (menu) menu.hidden = true;
    });
  }

  function countrySuggestions(query) {
    return countries
      .filter((country) => optionScore(country.label, query) < 99)
      .map((country) => ({ label: country.label, value: country.label, meta: 'Country', country }))
      .sort(compareOptions(query));
  }

  function placeSuggestions(query) {
    const country = selectedCountry().label;
    return places
      .filter((entry) => entry.country === country || !countryInput?.value.trim())
      .filter((entry) => optionScore(`${entry.label}, ${entry.country}`, query) < 99)
      .map((entry) => ({ label: entry.label, value: entry.label, meta: entry.country, entry }))
      .sort(compareOptions(query));
  }

  function bindAutocomplete(input, menu, suggestions, onPick) {
    if (!input) return;
    const render = () => renderMenu(menu, suggestions(input.value), onPick);
    input.addEventListener('focus', render);
    input.addEventListener('input', () => {
      render();
      updateSummary();
    });
    input.addEventListener('blur', () => setTimeout(closeMenus, 120));
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

  function distanceKm(firstLat, firstLng, secondLat, secondLng) {
    const toRadians = (value) => value * Math.PI / 180;
    const earthRadiusKm = 6371;
    const deltaLat = toRadians(secondLat - firstLat);
    const deltaLng = toRadians(secondLng - firstLng);
    const a = Math.sin(deltaLat / 2) ** 2 +
      Math.cos(toRadians(firstLat)) * Math.cos(toRadians(secondLat)) *
      Math.sin(deltaLng / 2) ** 2;
    return 2 * earthRadiusKm * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }

  function nearestPlace(lat, lng) {
    let best = null;
    let bestDistance = Number.POSITIVE_INFINITY;
    places.forEach((entry) => {
      const distance = distanceKm(entry.lat, entry.lng, lat, lng);
      if (distance < bestDistance) {
        best = entry;
        bestDistance = distance;
      }
    });
    return best ? { ...best, distanceKm: bestDistance } : null;
  }

  function setLocationStatus(message, state) {
    if (!locationStatus) return;
    locationStatus.textContent = message;
    locationStatus.dataset.state = state || 'info';
    locationStatus.hidden = false;
  }

  function setScope(scope) {
    const input = scopeInputs.find((item) => item.value === scope);
    if (input && !input.checked) {
      input.checked = true;
      setPanels();
    }
  }

  function useCurrentLocation() {
    if (!navigator.geolocation) {
      setLocationStatus('Current location is not available in this browser.', 'error');
      return;
    }

    setScope('Region');
    ensureMap();
    if (locateButton) locateButton.disabled = true;
    setLocationStatus('Waiting for browser location permission...', 'info');

    navigator.geolocation.getCurrentPosition((position) => {
      const lat = position.coords.latitude;
      const lng = position.coords.longitude;
      const picked = nearestPlace(lat, lng);

      if (picked) {
        if (countryInput) countryInput.value = picked.country;
        if (placeInput) placeInput.value = picked.label;
        setLocationStatus(`Suggested ${picked.label} from your current location.`, 'success');
      } else {
        setLocationStatus('Location found. Choose the nearest city manually if needed.', 'success');
      }

      setPoint(lat, lng, true);
      if (map) map.setView([lat, lng], Math.max(map.getZoom(), 12));
      notifyBudgetCurrency();
      if (locateButton) locateButton.disabled = false;
    }, (error) => {
      const messages = {
        1: 'Location permission was blocked. You can still type the city manually.',
        2: 'The browser could not detect your current location.',
        3: 'Location detection timed out. Try again or type the city manually.'
      };
      setLocationStatus(messages[error.code] || 'The current location could not be detected.', 'error');
      if (locateButton) locateButton.disabled = false;
    }, {
      enableHighAccuracy: true,
      maximumAge: 300000,
      timeout: 12000
    });
  }

  function ensureMap() {
    if (map || !mapEl) return;
    if (!window.L) {
      mapEl.classList.add('service-location-map-unavailable');
      mapEl.textContent = 'Map unavailable. You can still type a city or region and radius.';
      return;
    }

    const place = selectedPlace();
    const country = selectedCountry();
    const lat = numberValue(latInput) ?? place?.lat ?? country.lat;
    const lng = numberValue(lngInput) ?? place?.lng ?? country.lng;
    const zoom = place ? 10 : country.zoom;

    map = L.map(mapEl, { scrollWheelZoom: false }).setView([lat, lng], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);
    map.on('click', (event) => {
      const picked = nearestPlace(event.latlng.lat, event.latlng.lng);
      if (picked) {
        if (countryInput) countryInput.value = picked.country;
        if (placeInput) placeInput.value = picked.label;
      }
      setPoint(event.latlng.lat, event.latlng.lng, false);
      notifyBudgetCurrency();
    });

    setPoint(lat, lng, false);
  }

  function syncPlaceToMap() {
    const place = selectedPlace();
    if (place) {
      if (countryInput) countryInput.value = place.country;
      setPoint(place.lat, place.lng, true);
    } else {
      updateSummary();
    }
    notifyBudgetCurrency();
  }

  function syncCountryToMap() {
    const country = selectedCountry();
    if (map) map.setView([country.lat, country.lng], country.zoom);
    updateSummary();
    notifyBudgetCurrency();
  }

  function setPanels() {
    const scope = selectedScope();
    root.dataset.scope = scope.toLowerCase();
    if (countryPanel) countryPanel.hidden = scope === 'World';
    regionPanels.forEach((panel) => {
      panel.hidden = scope !== 'Region';
    });
    if (mapPanel) mapPanel.hidden = scope !== 'Region';

    if (scope === 'World') {
      closeMenus();
    }

    if (scope !== 'Region' && locationStatus) {
      locationStatus.hidden = true;
    }

    if (scope === 'Region') {
      ensureMap();
      syncPlaceToMap();
      setTimeout(() => {
        if (map) map.invalidateSize();
      }, 80);
    }

    updateSummary();
    notifyBudgetCurrency();
  }

  bindAutocomplete(countryInput, countryMenu, countrySuggestions, (item) => {
    countryInput.value = item.value;
    if (selectedScope() !== 'Region' && placeInput) placeInput.value = '';
    syncCountryToMap();
  });

  bindAutocomplete(placeInput, placeMenu, placeSuggestions, (item) => {
    placeInput.value = item.value;
    if (countryInput) countryInput.value = item.entry.country;
    syncPlaceToMap();
  });

  scopeInputs.forEach((input) => input.addEventListener('change', setPanels));
  countryInput?.addEventListener('change', syncCountryToMap);
  placeInput?.addEventListener('change', syncPlaceToMap);
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
  locateButton?.addEventListener('click', useCurrentLocation);
  document.addEventListener('click', (event) => {
    if (!root.contains(event.target)) closeMenus();
  });

  setPanels();
})();
