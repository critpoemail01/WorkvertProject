(() => {
  const root = document.querySelector('[data-service-photo-uploader]');
  if (!root) return;

  const input = root.querySelector('[data-service-photo-input]');
  const preview = root.querySelector('[data-service-photo-preview]');
  const countLabel = root.querySelector('[data-service-photo-count]');
  const maxPhotos = 5;

  function formatBytes(bytes) {
    if (!Number.isFinite(bytes)) return '';
    if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  function renderEmpty(message) {
    if (!preview) return;
    preview.replaceChildren();
    const empty = document.createElement('div');
    empty.className = 'service-photo-empty';
    const icon = document.createElement('i');
    icon.className = 'bi bi-card-image';
    const text = document.createElement('span');
    text.textContent = message || 'No photos selected yet. You can upload up to 5 images.';
    empty.append(icon, text);
    preview.appendChild(empty);
  }

  function render() {
    if (!input || !preview) return;
    const files = Array.from(input.files || []);
    const shownFiles = files.slice(0, maxPhotos);

    if (countLabel) {
      const suffix = files.length > maxPhotos ? `, first ${maxPhotos} shown` : '';
      countLabel.textContent = `${files.length} photo${files.length === 1 ? '' : 's'} selected${suffix}`;
    }

    if (!files.length) {
      renderEmpty();
      return;
    }

    preview.replaceChildren();
    shownFiles.forEach((file) => {
      const card = document.createElement('article');
      card.className = 'service-photo-card';

      const image = document.createElement('img');
      image.alt = file.name;
      image.src = URL.createObjectURL(file);
      image.addEventListener('load', () => URL.revokeObjectURL(image.src), { once: true });

      const caption = document.createElement('div');
      const name = document.createElement('strong');
      name.textContent = file.name;
      const size = document.createElement('small');
      size.textContent = formatBytes(file.size);
      caption.append(name, size);

      card.append(image, caption);
      preview.appendChild(card);
    });
  }

  input?.addEventListener('change', render);
  renderEmpty();
})();
