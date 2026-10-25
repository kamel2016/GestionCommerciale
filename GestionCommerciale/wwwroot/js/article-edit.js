// article-edit.js - Gestion de l'édition d'article

(function () {
    'use strict';

    let formChanged = false;

    document.addEventListener('DOMContentLoaded', function () {
        initializeTabs();
        initializeSupplierActions();
        initializeFormValidation();
        initializePriceCalculation();
        initializeAlerts();
        initializeUnsavedWarning();
        restoreSavedTab();
        openTabWithError();
        checkStockWarnings();
    });

    // ============================================
    // ONGLETS
    // ============================================
    function activateTab(tabId) {
        const button = document.querySelector('.tab-btn[data-tab="' + tabId + '"]');
        const content = document.getElementById(tabId);
        if (!button || !content) return;

        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

        button.classList.add('active');
        content.classList.add('active');
        sessionStorage.setItem('activeArticleTab', tabId);
    }

    function initializeTabs() {
        document.querySelectorAll('.tab-btn').forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();
                activateTab(this.getAttribute('data-tab'));
            });
        });
    }

    function restoreSavedTab() {
        const saved = sessionStorage.getItem('activeArticleTab');
        if (saved) activateTab(saved);
    }

    // Ouvre l'onglet contenant une erreur de validation serveur
    function openTabWithError() {
        const firstError = document.querySelector('.tab-content .input-validation-error');
        if (firstError) {
            const parent = firstError.closest('.tab-content');
            if (parent) activateTab(parent.id);
        }
    }

    // ============================================
    // FOURNISSEURS (formulaires cachés avec jeton)
    // ============================================
    function getAntiForgeryToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function postHandler(handler, fields) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = window.location.pathname + '?handler=' + encodeURIComponent(handler);

        const all = Object.assign({ __RequestVerificationToken: getAntiForgeryToken() }, fields);
        Object.keys(all).forEach(name => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = name;
            input.value = all[name];
            form.appendChild(input);
        });

        document.body.appendChild(form);
        formChanged = false;
        form.submit();
    }

    function initializeSupplierActions() {
        // Retrait d'un fournisseur
        document.querySelectorAll('.btn-remove').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const tarifId = this.getAttribute('data-tarif-id') || this.getAttribute('data-supplier-id');
                if (!tarifId) return;

                if (confirm('Êtes-vous sûr de vouloir retirer ce fournisseur ?')) {
                    postHandler('RemoveSupplier', { tarifId: tarifId });
                }
            });
        });
    }

    // ============================================
    // VALIDATION (alignée sur les [Required] de l'entité)
    // ============================================
    function initializeFormValidation() {
        const form = document.getElementById('articleForm');
        if (!form) return;

        form.addEventListener('submit', function (e) {
            if (!validateArticleForm()) {
                e.preventDefault();
            }
        });
    }

    function validateArticleForm() {
        const reference = document.querySelector('[name="Article.Reference"]');
        const libelle = document.querySelector('[name="Article.Libelle"]');
        const tva = document.querySelector('[name="Article.TauxTVA"]');
        const stockMin = document.querySelector('[name="Article.StockMin"]');
        const stockMax = document.querySelector('[name="Article.StockMax"]');

        if (!reference || !reference.value.trim()) {
            return fail('La référence est obligatoire', reference);
        }

        if (!libelle || !libelle.value.trim()) {
            return fail('Le libellé est obligatoire', libelle);
        }

        // Le code-barre est optionnel : aucune validation bloquante

        if (tva && tva.value !== '') {
            const v = parseFloat(tva.value);
            if (isNaN(v) || v < 0 || v > 100) {
                return fail('Le taux TVA doit être compris entre 0 et 100 %', tva);
            }
        }

        if (stockMin && stockMax && stockMin.value !== '' && stockMax.value !== '') {
            if (parseInt(stockMax.value, 10) < parseInt(stockMin.value, 10)) {
                return fail('Le stock maximum doit être supérieur ou égal au stock minimum', stockMax);
            }
        }

        return true;
    }

    function fail(message, field) {
        if (field) {
            const tab = field.closest('.tab-content');
            if (tab) activateTab(tab.id);
            field.focus();
        }
        showAlert('danger', message);
        return false;
    }

    // ============================================
    // CALCUL AUTOMATIQUE DU PRIX TTC
    // ============================================
    function initializePriceCalculation() {
        const ht = document.querySelector('[name="Article.PrixVenteHT"]');
        const ttc = document.querySelector('[name="Article.PrixVenteTTC"]');
        const tva = document.querySelector('[name="Article.TauxTVA"]');
        if (!ht || !ttc || !tva) return;

        function recalcTTC() {
            const h = parseFloat(ht.value);
            const t = parseFloat(tva.value);
            if (isNaN(h) || isNaN(t)) return;
            ttc.value = (h * (1 + t / 100)).toFixed(3);
        }

        function recalcHT() {
            const c = parseFloat(ttc.value);
            const t = parseFloat(tva.value);
            if (isNaN(c) || isNaN(t)) return;
            ht.value = (c / (1 + t / 100)).toFixed(3);
        }

        ht.addEventListener('input', recalcTTC);
        tva.addEventListener('input', recalcTTC);
        ttc.addEventListener('input', recalcHT);
    }

    // ============================================
    // ALERTES
    // ============================================
    function showAlert(type, message) {
        const icons = {
            danger: 'fa-exclamation-circle',
            success: 'fa-check-circle',
            warning: 'fa-exclamation-triangle'
        };

        const alert = document.createElement('div');
        alert.className = 'alert alert-' + type;

        const icon = document.createElement('i');
        icon.className = 'fas ' + (icons[type] || icons.danger);
        alert.appendChild(icon);
        alert.appendChild(document.createTextNode(' ' + message + ' '));

        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'close';
        close.innerHTML = '&times;';
        alert.appendChild(close);

        const container = document.querySelector('.article-container') || document.body;
        if (type === 'danger') {
            container.querySelectorAll('.alert-danger').forEach(a => a.remove());
        }
        container.insertBefore(alert, container.firstChild);

        bindAlert(alert, type === 'danger' ? 0 : 5000);
        alert.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function bindAlert(alert, autoDismissMs) {
        const hide = () => {
            alert.style.transition = 'opacity .3s';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        };
        const close = alert.querySelector('.close');
        if (close) close.addEventListener('click', hide);
        if (autoDismissMs > 0) setTimeout(hide, autoDismissMs);
    }

    function initializeAlerts() {
        document.querySelectorAll('.alert').forEach(a => bindAlert(a, 5000));
    }

    // ============================================
    // AVERTISSEMENT DE STOCK
    // ============================================
    function checkStockWarnings() {
        const qte = document.querySelector('[name="Article.Quantite"]');
        const min = document.querySelector('[name="Article.StockMin"]');
        const unite = document.querySelector('[name="Article.Unite"]');

        // Quantite peut être affichée en lecture seule (hors formulaire) : on lit aussi data-quantite
        const qteEl = qte || document.querySelector('[data-quantite]');
        if (!qteEl || !min) return;

        const q = parseFloat(qte ? qte.value : qteEl.getAttribute('data-quantite'));
        const m = parseFloat(min.value);
        if (isNaN(q) || isNaN(m)) return;

        if (m > 0 && q <= m) {
            const u = unite && unite.value ? unite.value : '';
            showAlert('warning', 'Stock faible : ' + q + ' ' + u + ' (minimum : ' + m + ')');
        }
    }

    // ============================================
    // PROTECTION CONTRE LA PERTE DE DONNÉES
    // ============================================
    function initializeUnsavedWarning() {
        const form = document.getElementById('articleForm');
        if (!form) return;

        form.addEventListener('change', () => { formChanged = true; });
        form.addEventListener('submit', () => { formChanged = false; });

        window.addEventListener('beforeunload', function (e) {
            if (formChanged) {
                e.preventDefault();
                e.returnValue = '';
            }
        });
    }

    // ============================================
    // RACCOURCIS CLAVIER
    // ============================================
    document.addEventListener('keydown', function (e) {
        // Ctrl+S : enregistrer
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
            e.preventDefault();
            const form = document.getElementById('articleForm');
            if (form && validateArticleForm()) {
                formChanged = false;
                form.submit();
            }
        }
    });

    // ============================================
    // UTILITAIRES
    // ============================================
    window.articleEdit = {
        formatCurrency: function (amount) {
            return new Intl.NumberFormat('fr-TN', {
                style: 'currency',
                currency: 'TND',
                minimumFractionDigits: 3
            }).format(amount);
        },
        copyToClipboard: function (text) {
            navigator.clipboard.writeText(text)
                .then(() => showAlert('success', 'Texte copié !'))
                .catch(() => showAlert('danger', 'Erreur lors de la copie'));
        }
    };
})();