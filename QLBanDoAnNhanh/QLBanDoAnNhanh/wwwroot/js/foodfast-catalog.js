/**
 * Food Fast — client-side filter & sort on product grids (no API changes).
 */
(function () {
  'use strict';

  function initCatalog() {
  var catalog = document.querySelector('[data-ff-catalog]');
  if (!catalog) return;

  var grid = catalog.querySelector('.products-grid');
  if (!grid) return;

  var cards = Array.from(grid.querySelectorAll('.product-card'));
  if (!cards.length) return;

  var emptyEl = catalog.querySelector('.ff-catalog-empty');
  var resultsEl = catalog.querySelector('.ff-catalog-results');
  var searchTerm = catalog.getAttribute('data-ff-search-term') || '';

  var state = {
    sort: 'default',
    priceRanges: [],
    categories: [],
    priceMin: null,
    priceMax: null,
  };

  var sortSelects = catalog.querySelectorAll('[data-ff-sort]');
  var clearBtns = catalog.querySelectorAll('[data-ff-clear-filters]');
  var priceChips = catalog.querySelectorAll('[data-ff-price]');
  var categoryChips = catalog.querySelectorAll('[data-ff-category]');
  var priceMinInput = catalog.querySelector('[data-ff-price-min]');
  var priceMaxInput = catalog.querySelector('[data-ff-price-max]');
  var drawer = document.getElementById('ff-filter-drawer');
  var backdrop = document.getElementById('ff-filter-drawer-backdrop');
  var openDrawerBtn = catalog.querySelector('[data-ff-open-filters]');
  var closeDrawerBtn = catalog.querySelector('[data-ff-close-filters]');

  function getCardData(card) {
    return {
      el: card,
      maSp: parseInt(card.getAttribute('data-ma-sp'), 10) || 0,
      price: parseFloat(card.getAttribute('data-price')) || 0,
      name: (card.getAttribute('data-name') || '').toLowerCase(),
      sales: parseInt(card.getAttribute('data-sales'), 10) || 0,
      discount: parseInt(card.getAttribute('data-discount'), 10) || 0,
      category: (card.getAttribute('data-category') || '').toUpperCase(),
    };
  }

  var cardData = cards.map(getCardData);

  function toggleChip(list, value, attrName) {
    var idx = list.indexOf(value);
    if (idx === -1) list.push(value);
    else list.splice(idx, 1);
    syncChipsByAttr(attrName, list);
  }

  function syncChipsByAttr(attrName, list) {
    catalog.querySelectorAll('[' + attrName + ']').forEach(function (chip) {
      var val = chip.getAttribute(attrName);
      chip.classList.toggle('is-active', list.indexOf(val) !== -1);
    });
  }

  function matchesPrice(price) {
    if (state.priceRanges.length) {
      var ok = state.priceRanges.some(function (range) {
        if (range === 'under-50') return price < 50000;
        if (range === '50-100') return price >= 50000 && price <= 100000;
        if (range === '100-200') return price > 100000 && price <= 200000;
        if (range === 'over-200') return price > 200000;
        return true;
      });
      if (!ok) return false;
    }

    if (state.priceMin != null && price < state.priceMin) return false;
    if (state.priceMax != null && price > state.priceMax) return false;

    return true;
  }

  function matchesCategory(category) {
    if (!state.categories.length) return true;
    return state.categories.indexOf(category) !== -1;
  }

  function sortCards(list) {
    var sorted = list.slice();

    switch (state.sort) {
      case 'price-asc':
        sorted.sort(function (a, b) {
          return a.price - b.price;
        });
        break;
      case 'price-desc':
        sorted.sort(function (a, b) {
          return b.price - a.price;
        });
        break;
      case 'name-asc':
        sorted.sort(function (a, b) {
          return a.name.localeCompare(b.name, 'vi');
        });
        break;
      case 'name-desc':
        sorted.sort(function (a, b) {
          return b.name.localeCompare(a.name, 'vi');
        });
        break;
      case 'newest':
        sorted.sort(function (a, b) {
          return b.maSp - a.maSp;
        });
        break;
      case 'bestseller':
        sorted.sort(function (a, b) {
          return b.sales - a.sales;
        });
        break;
      case 'discount':
        sorted.sort(function (a, b) {
          return b.discount - a.discount;
        });
        break;
      default:
        sorted.sort(function (a, b) {
          return a.maSp - b.maSp;
        });
    }

    return sorted;
  }

  function applyFilters() {
    var visible = cardData.filter(function (item) {
      return matchesPrice(item.price) && matchesCategory(item.category);
    });

    visible = sortCards(visible);

    cardData.forEach(function (item) {
      item.el.classList.add('ff-card-hidden');
    });

    visible.forEach(function (item) {
      item.el.classList.remove('ff-card-hidden');
      grid.appendChild(item.el);
    });

    grid.classList.add('ff-grid-updating');
    setTimeout(function () {
      grid.classList.remove('ff-grid-updating');
    }, 400);

    var count = visible.length;

    if (resultsEl) {
      if (searchTerm) {
        resultsEl.innerHTML =
          'Kết quả tìm kiếm cho: <strong>' +
          escapeHtml(searchTerm) +
          '</strong> — Tìm thấy <strong>' +
          count +
          '</strong> sản phẩm';
        resultsEl.hidden = false;
      } else if (
        state.sort !== 'default' ||
        state.priceRanges.length ||
        state.categories.length ||
        state.priceMin != null ||
        state.priceMax != null
      ) {
        resultsEl.innerHTML =
          'Tìm thấy <strong>' + count + '</strong> sản phẩm';
        resultsEl.hidden = false;
      } else {
        resultsEl.hidden = true;
      }
    }

    if (emptyEl) {
      emptyEl.classList.toggle('is-visible', count === 0);
      grid.style.display = count === 0 ? 'none' : '';
    }
  }

  function escapeHtml(str) {
    var d = document.createElement('div');
    d.textContent = str;
    return d.innerHTML;
  }

  function clearFilters() {
    state.sort = 'default';
    state.priceRanges = [];
    state.categories = [];
    state.priceMin = null;
    state.priceMax = null;

    sortSelects.forEach(function (sel) {
      sel.value = 'default';
    });
    syncChipsByAttr('data-ff-price', []);
    syncChipsByAttr('data-ff-category', []);
    if (priceMinInput) priceMinInput.value = '';
    if (priceMaxInput) priceMaxInput.value = '';

    applyFilters();
  }

  sortSelects.forEach(function (sel) {
    sel.addEventListener('change', function () {
      state.sort = sel.value;
      sortSelects.forEach(function (s) {
        s.value = sel.value;
      });
      applyFilters();
    });
  });

  priceChips.forEach(function (chip) {
    chip.addEventListener('click', function () {
      var val = chip.getAttribute('data-ff-price');
      toggleChip(state.priceRanges, val, 'data-ff-price');
      applyFilters();
    });
  });

  categoryChips.forEach(function (chip) {
    chip.addEventListener('click', function () {
      var val = chip.getAttribute('data-ff-category');
      toggleChip(state.categories, val, 'data-ff-category');
      applyFilters();
    });
  });

  function onRangeChange() {
    var minVal = priceMinInput ? parseInt(priceMinInput.value, 10) : null;
    var maxVal = priceMaxInput ? parseInt(priceMaxInput.value, 10) : null;
    state.priceMin = minVal > 0 ? minVal : null;
    state.priceMax = maxVal && maxVal < 500000 ? maxVal : null;
    applyFilters();
  }

  if (priceMinInput) priceMinInput.addEventListener('change', onRangeChange);
  if (priceMaxInput) priceMaxInput.addEventListener('change', onRangeChange);

  clearBtns.forEach(function (btn) {
    btn.addEventListener('click', clearFilters);
  });

  function openDrawer() {
    if (drawer) drawer.classList.add('is-open');
    if (backdrop) backdrop.classList.add('is-open');
    document.body.style.overflow = 'hidden';
  }

  function closeDrawer() {
    if (drawer) drawer.classList.remove('is-open');
    if (backdrop) backdrop.classList.remove('is-open');
    document.body.style.overflow = '';
  }

  if (openDrawerBtn) openDrawerBtn.addEventListener('click', openDrawer);
  if (closeDrawerBtn) closeDrawerBtn.addEventListener('click', closeDrawer);
  if (backdrop) backdrop.addEventListener('click', closeDrawer);

  if (searchTerm && resultsEl) {
    resultsEl.innerHTML =
      'Kết quả tìm kiếm cho: <strong>' +
      escapeHtml(searchTerm) +
      '</strong> — Tìm thấy <strong>' +
      cardData.length +
      '</strong> sản phẩm';
    resultsEl.hidden = false;
  }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initCatalog);
  } else {
    initCatalog();
  }
})();
