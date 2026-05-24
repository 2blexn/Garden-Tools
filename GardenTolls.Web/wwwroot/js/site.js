(function () {
    document.querySelectorAll('.gt-burger[data-bs-toggle="collapse"]').forEach(function (btn) {
        var targetSel = btn.getAttribute('data-bs-target');
        if (!targetSel) return;
        var target = document.querySelector(targetSel);
        if (!target) return;
        target.addEventListener('shown.bs.collapse', function () { btn.setAttribute('aria-expanded', 'true'); });
        target.addEventListener('hidden.bs.collapse', function () { btn.setAttribute('aria-expanded', 'false'); });
    });

    function dismissToast(el) {
        if (!el || el.classList.contains('gt-toast--hide')) return;
        el.classList.add('gt-toast--hide');
        setTimeout(function () { el.remove(); }, 320);
    }

    function bindSearchSuggestionClicks(panel, input, form) {
        if (!panel || !input || !form) return;
        panel.querySelectorAll('[data-query]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                input.value = btn.getAttribute('data-query') || '';
                panel.hidden = true;
                form.submit();
            });
        });
    }

    function panelHasItems(panel) {
        return panel && panel.querySelector('.gt-search-dropdown-item');
    }

    function initSearchDropdown(inputId, panelId, formId, suggestionsUrl) {
        var searchInput = document.getElementById(inputId);
        var searchPanel = document.getElementById(panelId);
        var searchForm = document.getElementById(formId);
        var searchWrap = searchInput && searchInput.closest('.gt-search-wrap');
        if (!searchInput || !searchPanel || !searchForm) return;

        var debounceTimer;

        function showPanel() {
            if (panelHasItems(searchPanel)) searchPanel.hidden = false;
        }

        function hidePanel() { searchPanel.hidden = true; }

        function loadSuggestions() {
            var q = searchInput.value.trim();
            var url = suggestionsUrl + (suggestionsUrl.indexOf('?') >= 0 ? '&' : '?') + 'q=' + encodeURIComponent(q);
            fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    searchPanel.innerHTML = html;
                    bindSearchSuggestionClicks(searchPanel, searchInput, searchForm);
                    if (panelHasItems(searchPanel)) showPanel();
                    else hidePanel();
                })
                .catch(function () { hidePanel(); });
        }

        bindSearchSuggestionClicks(searchPanel, searchInput, searchForm);

        searchInput.addEventListener('focus', function () {
            loadSuggestions();
        });

        searchInput.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(loadSuggestions, 220);
        });

        searchInput.addEventListener('click', function (e) {
            e.stopPropagation();
            loadSuggestions();
        });

        document.addEventListener('click', function (e) {
            var inSearch = searchWrap && searchWrap.contains(e.target);
            if (!inSearch) hidePanel();
        });
    }

    initSearchDropdown('gtSearchInput', 'gtSearchPanel', 'gtSearchForm', '/Home/SearchSuggestions');

    var pageInput = document.getElementById('searchPageInput');
    var pagePanel = document.getElementById('searchPageSuggestions');
    var pageForm = document.getElementById('searchPageForm');
    if (pageInput && pagePanel && pageForm) {
        initSearchDropdown('searchPageInput', 'searchPageSuggestions', 'searchPageForm', '/Home/SearchSuggestions');
    }

    document.querySelectorAll('.gt-toast').forEach(function (toast) {
        var closeBtn = toast.querySelector('.gt-toast-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function () { dismissToast(toast); });
        }
        var ms = parseInt(toast.getAttribute('data-auto-dismiss') || '0', 10);
        if (ms > 0) {
            setTimeout(function () { dismissToast(toast); }, ms);
        }
    });
})();
