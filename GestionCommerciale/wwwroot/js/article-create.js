(function () {
    'use strict';

    function init() {
        debugger;
        var form = document.getElementById('createArticleForm');
        if (!form) return;

        // Ce script ne bloque jamais le submit : Razor Pages doit recevoir le POST.
        var stockMin = form.querySelector('[name="Article.StockMin"]');
        var stockMax = form.querySelector('[name="Article.StockMax"]');
        if (stockMin && stockMax) {
            stockMin.addEventListener('input', function () {
                stockMax.min = stockMin.value || '0';
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
}());