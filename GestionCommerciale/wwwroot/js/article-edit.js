// article-edit.js - Gestion de l'édition d'article

document.addEventListener('DOMContentLoaded', function () {
    // Initialiser les onglets
    initializeTabs();

    // Initialiser les événements
    initializeEvents();

    // Initialiser les alertes
    initializeAlerts();

    // Restaurer l'onglet sauvegardé
    restoreSavedTab();
});

/**
 * Initialiser la navigation par onglets
 */
function initializeTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            const tabId = this.getAttribute('data-tab');

            // Désactiver tous les onglets
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabContents.forEach(content => content.classList.remove('active'));

            // Activer l'onglet sélectionné
            this.classList.add('active');
            document.getElementById(tabId).classList.add('active');

            // Sauvegarder l'onglet actif
            sessionStorage.setItem('activeArticleTab', tabId);
        });
    });
}

/**
 * Initialiser les événements
 */
function initializeEvents() {
    // Ajouter fournisseur
    const confirmAddSupplierBtn = document.getElementById('confirmAddSupplier');
    if (confirmAddSupplierBtn) {
        confirmAddSupplierBtn.addEventListener('click', function () {
            const supplierSelect = document.getElementById('supplierSelect');
            if (!supplierSelect || !supplierSelect.value) {
                alert('Veuillez sélectionner un fournisseur');
                return;
            }

            const form = document.getElementById('articleForm');
            const formData = new FormData(form);
            formData.append('handler', 'AddSupplier');
            formData.append('supplierId', supplierSelect.value);

            // Soumettre le formulaire
            form.submit();
        });
    }

    // Retirer fournisseur
    const removeButtons = document.querySelectorAll('.btn-remove');
    removeButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const supplierId = this.getAttribute('data-supplier-id');

            if (confirm('Êtes-vous sûr de vouloir retirer ce fournisseur ?')) {
                // Créer et soumettre un formulaire caché
                const form = document.createElement('form');
                form.method = 'POST';
                form.innerHTML = `
                    <input type="hidden" name="handler" value="RemoveSupplier" />
                    <input type="hidden" name="supplierId" value="${supplierId}" />
                `;
                document.body.appendChild(form);
                form.submit();
            }
        });
    });

    // Validation du formulaire
    const articleForm = document.getElementById('articleForm');
    if (articleForm) {
        articleForm.addEventListener('submit', function (e) {
            if (!validateArticleForm()) {
                e.preventDefault();
                return false;
            }
        });
    }
}

/**
 * Initialiser la fermeture des alertes
 */
function initializeAlerts() {
    document.querySelectorAll('.alert .close').forEach(closeBtn => {
        closeBtn.addEventListener('click', function () {
            this.parentElement.style.display = 'none';
        });
    });

    // Auto-dismiss des alertes après 5 secondes
    document.querySelectorAll('.alert').forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => {
                alert.style.display = 'none';
            }, 300);
        }, 5000);
    });
}

/**
 * Restaurer l'onglet sauvegardé
 */
function restoreSavedTab() {
    const savedTab = sessionStorage.getItem('activeArticleTab');
    if (savedTab) {
        const tabButton = document.querySelector(`[data-tab="${savedTab}"]`);
        if (tabButton) {
            tabButton.click();
        }
    }
}

/**
 * Valider le formulaire d'article
 */
function validateArticleForm() {
    const reference = document.querySelector('[name="Article.Reference"]');
    const barcode = document.querySelector('[name="Article.Barcode"]');
    const name = document.querySelector('[name="Article.Name"]');
    const unit = document.querySelector('[name="Article.Unit"]');
    const vatRate = document.querySelector('[name="Article.VatRate"]');

    // Validation de la référence
    if (!reference || !reference.value.trim()) {
        showError('La référence est obligatoire');
        reference?.focus();
        return false;
    }

    if (reference.value.trim().length < 3) {
        showError('La référence doit contenir au moins 3 caractères');
        reference.focus();
        return false;
    }

    // Validation du code-barre
    if (!barcode || !barcode.value.trim()) {
        showError('Le code-barre est obligatoire');
        barcode?.focus();
        return false;
    }

    // Validation du nom
    if (!name || !name.value.trim()) {
        showError('La désignation est obligatoire');
        name?.focus();
        return false;
    }

    // Validation de l'unité
    if (!unit || !unit.value) {
        showError('Veuillez sélectionner une unité');
        unit?.focus();
        return false;
    }

    // Validation du taux TVA
    if (!vatRate || parseFloat(vatRate.value) < 0 || parseFloat(vatRate.value) > 100) {
        showError('Le taux TVA doit être entre 0 et 100%');
        vatRate?.focus();
        return false;
    }

    return true;
}

/**
 * Afficher un message d'erreur
 */
function showError(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-danger';
    alert.innerHTML = `
        <i class="fas fa-exclamation-circle"></i>
        ${message}
        <button type="button" class="close">&times;</button>
    `;

    const container = document.querySelector('.article-container');
    if (container) {
        // Retirer les anciennes alertes d'erreur
        const oldErrors = container.querySelectorAll('.alert-danger');
        oldErrors.forEach(err => err.remove());

        container.insertBefore(alert, container.firstChild);

        // Ajouter l'événement de fermeture
        alert.querySelector('.close').addEventListener('click', function () {
            alert.style.display = 'none';
        });

        // Faire défiler vers l'alerte
        alert.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

/**
 * Afficher un message de succès
 */
function showSuccess(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-success';
    alert.innerHTML = `
        <i class="fas fa-check-circle"></i>
        ${message}
        <button type="button" class="close">&times;</button>
    `;

    const container = document.querySelector('.article-container');
    if (container) {
        container.insertBefore(alert, container.firstChild);

        alert.querySelector('.close').addEventListener('click', function () {
            alert.style.display = 'none';
        });

        setTimeout(() => {
            alert.style.display = 'none';
        }, 5000);
    }
}

/**
 * Activer/Désactiver les champs basés sur le statut
 */
function toggleArticleFields(isActive) {
    const fields = document.querySelectorAll('.form-control, .form-check-input');
    fields.forEach(field => {
        if (field.id !== 'isActiveCheck') {
            field.disabled = !isActive;
            field.style.opacity = isActive ? '1' : '0.6';
        }
    });
}

/**
 * Calculer le stock disponible
 */
function calculateAvailableStock() {
    const currentStock = parseFloat(document.querySelector('[name="Article.CurrentStock"]')?.value || 0);
    const reservedStock = parseFloat(document.querySelector('[name="Article.ReservedStock"]')?.value || 0);
    const onOrderStock = parseFloat(document.querySelector('[name="Article.OnOrderStock"]')?.value || 0);

    return {
        current: currentStock,
        available: currentStock - reservedStock,
        reserved: reservedStock,
        onOrder: onOrderStock
    };
}

/**
 * Formater un nombre en devise
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('fr-FR', {
        style: 'currency',
        currency: 'EUR'
    }).format(amount);
}

/**
 * Copier un texte dans le presse-papiers
 */
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showSuccess('Texte copié !');
    }).catch(() => {
        showError('Erreur lors de la copie');
    });
}

/**
 * Exporter les données d'article
 */
function exportArticleData() {
    const reference = document.querySelector('[name="Article.Reference"]')?.value || '';
    const barcode = document.querySelector('[name="Article.Barcode"]')?.value || '';
    const name = document.querySelector('[name="Article.Name"]')?.value || '';

    const data = {
        reference,
        barcode,
        name,
        exportedAt: new Date().toISOString()
    };

    const json = JSON.stringify(data, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `article_${reference}.json`;
    a.click();
    URL.revokeObjectURL(url);
}

/**
 * Imprimer la fiche article
 */
function printArticle() {
    window.print();
}

/**
 * Gérer le changement de catégorie
 */
function onCategoryChange(categorySelect) {
    console.log('Catégorie sélectionnée:', categorySelect.value);
    // À adapter selon les besoins
}

/**
 * Avertissement de stock minimum
 */
function checkStockWarnings() {
    const currentStock = parseFloat(document.querySelector('[name="Article.CurrentStock"]')?.value || 0);
    const stockMinimum = parseFloat(document.querySelector('[name="Article.StockMinimum"]')?.value || 0);

    if (currentStock <= stockMinimum && stockMinimum > 0) {
        showWarning(`⚠️ Stock faible: ${currentStock} ${document.querySelector('[name="Article.Unit"]')?.value || 'pcs'}`);
    }
}

/**
 * Afficher un avertissement
 */
function showWarning(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-warning';
    alert.innerHTML = `
        <i class="fas fa-exclamation-triangle"></i>
        ${message}
        <button type="button" class="close">&times;</button>
    `;

    const container = document.querySelector('.article-container');
    if (container) {
        container.insertBefore(alert, container.firstChild);

        alert.querySelector('.close').addEventListener('click', function () {
            alert.style.display = 'none';
        });
    }
}

/**
 * Événements de clavier
 */
document.addEventListener('keydown', function (e) {
    // Ctrl+S pour sauvegarder
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        const form = document.getElementById('articleForm');
        if (form && validateArticleForm()) {
            form.submit();
        }
    }

    // Ctrl+P pour imprimer
    if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
        e.preventDefault();
        printArticle();
    }
});

/**
 * Prévention de perte de données
 */
let formChanged = false;
const originalFormState = {};

document.addEventListener('DOMContentLoaded', function () {
    // Sauvegarder l'état initial du formulaire
    document.querySelectorAll('input, select, textarea').forEach(field => {
        originalFormState[field.name] = field.value;

        field.addEventListener('change', () => {
            formChanged = true;
        });
    });
});

window.addEventListener('beforeunload', function (e) {
    if (formChanged) {
        e.preventDefault();
        e.returnValue = 'Vous avez des modifications non sauvegardées. Voulez-vous vraiment quitter ?';
    }
});

// Réinitialiser le flag après soumission
const form = document.getElementById('articleForm');
if (form) {
    form.addEventListener('submit', function () {
        formChanged = false;
    });
}