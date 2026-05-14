(() => {
  const root = document.querySelector('[data-location-targeting]');
  if (!root) return;

  const scopeInputs = Array.from(root.querySelectorAll('input[name="Input.AudienceLocationScope"]'));
  const countryInput = root.querySelector('[data-location-country]');
  const cityInput = root.querySelector('[data-location-city]');
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

  function selectedScope() {
    const checked = scopeInputs.find((input) => input.checked);
    return checked ? checked.value : 'World';
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
      coordinates.textContent = 'No point selected yet.';
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
  [countryInput, cityInput].forEach((input) => {
    if (input) input.addEventListener('input', updateSummary);
  });
  if (radiusInput) radiusInput.addEventListener('input', syncRadiusFromInput);
  if (radiusRange) radiusRange.addEventListener('input', syncRadiusFromRange);

  if (!window.L && mapEl) {
    mapEl.classList.add('planner-map-unavailable');
    mapEl.textContent = 'Map unavailable. You can still type a city and radius.';
  }

  setPanels();
})();
