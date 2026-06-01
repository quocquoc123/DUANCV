/**
 * Food Fast — realtime search suggestions via existing /SanPhams/Search API (HTML).
 */
(function () {
  'use strict';

  var DEBOUNCE_MS = 350;
  var MIN_CHARS = 1;
  var MAX_SUGGESTIONS = 8;

  var form = document.querySelector('.ff-search-form');
  if (!form) return;

  var input = form.querySelector('#searchTerm');
  var suggestionsEl = document.getElementById('ff-search-suggestions');
  if (!input || !suggestionsEl) return;

  var searchUrl = form.getAttribute('data-ff-search-url') || '/SanPhams/Search';
  var debounceTimer = null;
  var abortController = null;
  var activeIndex = -1;

  function hideSuggestions() {
    suggestionsEl.hidden = true;
    suggestionsEl.innerHTML = '';
    activeIndex = -1;
  }

  function showLoading() {
    suggestionsEl.hidden = false;
    suggestionsEl.innerHTML =
      '<div class="ff-search-suggestions-loading">Đang tìm kiếm...</div>';
  }

  function formatPrice(text) {
    return text ? text.trim() : '';
  }

  function parseProductsFromHtml(html) {
    var doc = new DOMParser().parseFromString(html, 'text/html');
    var cards = doc.querySelectorAll(
      '#products-section .product-card, .products-grid .product-card'
    );
    var items = [];

    cards.forEach(function (card) {
      var link =
        card.querySelector('.btn-view-detail') ||
        card.querySelector('.btn-buy') ||
        card.querySelector('a[href*="ChiTietSanPham"]');
      var img = card.querySelector('.product-image');
      var nameEl = card.querySelector('.product-name');
      var priceEl = card.querySelector('.product-price');

      if (!link || !nameEl) return;

      items.push({
        url: link.getAttribute('href'),
        image: img ? img.getAttribute('src') : '',
        name: nameEl.textContent.trim(),
        price: priceEl ? formatPrice(priceEl.textContent) : '',
      });
    });

    return items;
  }

  function clientFilter(items, term) {
    var q = term.toLowerCase().trim();
    if (!q) return items;

    return items.filter(function (item) {
      var haystack = (item.name + ' ' + (item.description || '') + ' ' + (item.category || '')).toLowerCase();
      return haystack.indexOf(q) !== -1;
    });
  }

  function enrichFromPage(term) {
    var dataEl = document.getElementById('ff-products-data');
    if (!dataEl) return [];

    try {
      var all = JSON.parse(dataEl.textContent);
      var q = term.toLowerCase().trim();
      return all
        .filter(function (p) {
          var haystack = (
            (p.tenSp || '') +
            ' ' +
            (p.thanhPhan || '') +
            ' ' +
            (p.chitietSp || '') +
            ' ' +
            (p.tenDm || '')
          ).toLowerCase();
          return haystack.indexOf(q) !== -1;
        })
        .slice(0, MAX_SUGGESTIONS)
        .map(function (p) {
          return {
            url: p.detailUrl,
            image: p.hinhAnh1,
            name: p.tenSp,
            price: formatVnd(p.giaTien),
          };
        });
    } catch (e) {
      return [];
    }
  }

  function formatVnd(value) {
    try {
      return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
      }).format(value);
    } catch (e) {
      return value + ' VND';
    }
  }

  function renderSuggestions(items, term) {
    if (!items.length) {
      suggestionsEl.hidden = false;
      suggestionsEl.innerHTML =
        '<div class="ff-search-suggestions-empty">Không tìm thấy sản phẩm phù hợp</div>';
      return;
    }

    var html = items
      .slice(0, MAX_SUGGESTIONS)
      .map(function (item, i) {
        var img = item.image
          ? '<img class="ff-search-suggestion-img" src="' +
            escapeAttr(item.image) +
            '" alt="">'
          : '<div class="ff-search-suggestion-img"></div>';
        return (
          '<a class="ff-search-suggestion" href="' +
          escapeAttr(item.url) +
          '" data-index="' +
          i +
          '">' +
          img +
          '<div class="ff-search-suggestion-body">' +
          '<div class="ff-search-suggestion-name">' +
          escapeHtml(item.name) +
          '</div>' +
          (item.price
            ? '<div class="ff-search-suggestion-price">' +
              escapeHtml(item.price) +
              '</div>'
            : '') +
          '</div></a>'
        );
      })
      .join('');

    html +=
      '<a class="ff-search-view-all" href="' +
      escapeAttr(
        searchUrl + '?searchTerm=' + encodeURIComponent(term)
      ) +
      '">Xem tất cả kết quả</a>';

    suggestionsEl.innerHTML = html;
    suggestionsEl.hidden = false;
  }

  function escapeHtml(str) {
    var d = document.createElement('div');
    d.textContent = str;
    return d.innerHTML;
  }

  function escapeAttr(str) {
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;')
      .replace(/</g, '&lt;');
  }

  function fetchSuggestions(term) {
    if (abortController) abortController.abort();
    abortController = new AbortController();
    showLoading();

    var url = searchUrl + '?searchTerm=' + encodeURIComponent(term);

    fetch(url, {
      signal: abortController.signal,
      headers: { 'X-Requested-With': 'XMLHttpRequest' },
    })
      .then(function (res) {
        return res.text();
      })
      .then(function (html) {
        var items = parseProductsFromHtml(html);

        if (items.length < MAX_SUGGESTIONS) {
          var pageItems = enrichFromPage(term);
          var seen = {};
          items.forEach(function (it) {
            seen[it.url] = true;
          });
          pageItems.forEach(function (it) {
            if (!seen[it.url] && items.length < MAX_SUGGESTIONS) {
              items.push(it);
              seen[it.url] = true;
            }
          });
        }

        renderSuggestions(items, term);
      })
      .catch(function (err) {
        if (err.name === 'AbortError') return;
        var fallback = enrichFromPage(term);
        if (fallback.length) {
          renderSuggestions(fallback, term);
        } else {
          suggestionsEl.hidden = false;
          suggestionsEl.innerHTML =
            '<div class="ff-search-suggestions-empty">Không thể tải gợi ý. Thử nhấn Tìm kiếm.</div>';
        }
      });
  }

  function onInput() {
    var term = input.value.trim();
    clearTimeout(debounceTimer);

    if (term.length < MIN_CHARS) {
      hideSuggestions();
      return;
    }

    debounceTimer = setTimeout(function () {
      fetchSuggestions(term);
    }, DEBOUNCE_MS);
  }

  input.addEventListener('input', onInput);

  input.addEventListener('focus', function () {
    if (input.value.trim().length >= MIN_CHARS && suggestionsEl.innerHTML) {
      suggestionsEl.hidden = false;
    }
  });

  input.addEventListener('keydown', function (e) {
    var links = suggestionsEl.querySelectorAll('.ff-search-suggestion');
    if (!links.length || suggestionsEl.hidden) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      activeIndex = Math.min(activeIndex + 1, links.length - 1);
      updateActive(links);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      activeIndex = Math.max(activeIndex - 1, 0);
      updateActive(links);
    } else if (e.key === 'Enter' && activeIndex >= 0) {
      e.preventDefault();
      links[activeIndex].click();
    } else if (e.key === 'Escape') {
      hideSuggestions();
    }
  });

  function updateActive(links) {
    links.forEach(function (link, i) {
      link.style.background = i === activeIndex ? '#fff8ef' : '';
    });
    if (links[activeIndex]) {
      links[activeIndex].scrollIntoView({ block: 'nearest' });
    }
  }

  document.addEventListener('click', function (e) {
    if (!form.contains(e.target)) {
      hideSuggestions();
    }
  });

  form.addEventListener('submit', function () {
    hideSuggestions();
  });
})();
