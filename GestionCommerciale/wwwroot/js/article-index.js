// article-index.js - Gestion de la page de recherche d'articles

document.addEventListener('DOMContentLoaded', function () {
    // Initialiser les alertes
    initializeAlerts();

    // Initialiser les événements de suppression
    initializeDeleteButtons();

    // Initialiser les filtres
    initializeFilters();

    // Initialiser les raccourcis clavier
    initializeKeyboardShortcuts();
});

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
 * Initialiser les boutons de suppression
 */
function initializeDeleteButtons() {
    const deleteButtons = document.querySelectorAll('.btn-delete');

    deleteButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const articleId = this.getAttribute('data-article-id');
            const articleName = this.getAttribute('data-article-name');

            showDeleteModal(articleId, articleName);
        });
    });
}

/**
 * Afficher le modal de suppression
 */
function showDeleteModal(articleId, articleName) {
    const modal = document.getElementById('deleteModal');
    const articleNameEl = document.getElementById('articleName');
    const articleIdInput = document.getElementById('articleIdInput');

    if (modal && articleNameEl && articleIdInput) {
        articleNameEl.textContent = articleName;
        articleIdInput.value = articleId;

        // Afficher le modal avec Bootstrap
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    }
}

/**
 * Initialiser les filtres
 */
function initializeFilters() {
    const searchForm = document.getElementById('searchForm');
    const searchInput = document.querySelector('.search-input');

    if (searchForm) {
        // Mettre en évidence les filtres actifs
        highlightActiveFilters();
    }

    if (searchInput) {
        // Focus sur le champ de recherche
        searchInput.addEventListener('focus', function () {
            this.style.borderColor = '#27ae60';
        });

        searchInput.addEventListener('blur', function () {
            this.style.borderColor = '#bdc3c7';
        });

        // Suggestion lors de la saisie
        let searchTimeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const query = this.value.trim();

            if (query.length >= 2) {
                // Afficher des suggestions (à implémenter)
                showSearchSuggestions(query);
            }
        });
    }
}

/**
 * Mettre en évidence les filtres actifs
 */
function highlightActiveFilters() {
    const filterSelects = document.querySelectorAll('.filter-select');

    filterSelects.forEach(select => {
        if (select.value) {
            select.style.borderColor = '#27ae60';
            select.style.fontWeight = '600';
        }
    });
}

/**
 * Afficher les suggestions de recherche
 */
function showSearchSuggestions(query) {
    // À implémenter avec AJAX si nécessaire
    console.log('Recherche:', query);
}

/**
 * Initialiser les raccourcis clavier
 */
function initializeKeyboardShortcuts() {
    document.addEventListener('keydown', function (e) {
        // Ctrl+N pour créer un nouvel article
        if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
            e.preventDefault();
            window.location.href = '/Articles/Edit';
        }

        // Ctrl+F pour le focus sur la recherche
        if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
            e.preventDefault();
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.focus();
                searchInput.select();
            }
        }
    });
}

/**
 * Filtrer les articles en temps réel (côté client)
 */
function filterArticlesClient(searchTerm) {
    const rows = document.querySelectorAll('.articles-table tbody tr');
    let visibleCount = 0;

    const search = searchTerm.toLowerCase();

    rows.forEach(row => {
        const reference = row.querySelector('td[data-label="Référence"]')?.textContent.toLowerCase() || '';
        const name = row.querySelector('td[data-label="Désignation"]')?.textContent.toLowerCase() || '';
        const barcode = row.querySelector('.barcode')?.textContent.toLowerCase() || '';

        if (reference.includes(search) || name.includes(search) || barcode.includes(search)) {
            row.style.display = '';
            visibleCount++;
        } else {
            row.style.display = 'none';
        }
    });

    // Afficher le message d'absence de résultat si nécessaire
    if (visibleCount === 0) {
        showEmptyResultsMessage();
    }
}

/**
 * Afficher le message d'absence de résultats
 */
function showEmptyResultsMessage() {
    const table = document.querySelector('.articles-table tbody');
    if (table && table.querySelectorAll('tr:not([style*="display:none"])').length === 0) {
        const emptyRow = document.createElement('tr');
        emptyRow.innerHTML = '<td colspan="9" style="text-align: center; padding: 40px;">Aucun article ne correspond à votre recherche</td>';
        table.appendChild(emptyRow);
    }
}

/**
 * Exporter la liste des articles
 */
function exportArticles(format = 'csv') {
    const rows = document.querySelectorAll('.articles-table tbody tr');

    if (format === 'csv') {
        let csv = 'Référence,Désignation,Catégorie,Marque,Stock,Statut,Fournisseurs\n';

        rows.forEach(row => {
            if (row.style.display !== 'none') {
                const cells = row.querySelectorAll('td');
                const data = Array.from(cells).map((cell, index) => {
                    if (index !== cells.length - 1) { // Exclure la colonne Actions
                        return '"' + cell.textContent.trim().replace(/"/g, '""') + '"';
                    }
                    return '';
                }).join(',');
                csv += data + '\n';
            }
        });

        downloadFile(csv, 'articles.csv', 'text/csv');
    }
}

/**
 * Télécharger un fichier
 */
function downloadFile(content, filename, type) {
    const blob = new Blob([content], { type });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
}

/**
 * Sélectionner/Désélectionner tous les articles
 */
function toggleSelectAll(checkbox) {
    const rows = document.querySelectorAll('.articles-table tbody tr');
    rows.forEach(row => {
        const rowCheckbox = row.querySelector('input[type="checkbox"]');
        if (rowCheckbox) {
            rowCheckbox.checked = checkbox.checked;
        }
    });
}

/**
 * Obtenir les articles sélectionnés
 */
function getSelectedArticles() {
    const rows = document.querySelectorAll('.articles-table tbody tr');
    const selected = [];

    rows.forEach(row => {
        const checkbox = row.querySelector('input[type="checkbox"]');
        if (checkbox && checkbox.checked) {
            const idCell = row.querySelector('td');
            const id = idCell?.getAttribute('data-id');
            if (id) {
                selected.push(parseInt(id));
            }
        }
    });

    return selected;
}

/**
 * Appliquer une action en masse aux articles sélectionnés
 */
function applyBulkAction(action) {
    const selected = getSelectedArticles();

    if (selected.length === 0) {
        alert('Veuillez sélectionner au moins un article.');
        return;
    }

    switch (action) {
        case 'delete':
            if (confirm(`Êtes-vous sûr de vouloir supprimer ${selected.length} article(s) ?`)) {
                bulkDelete(selected);
            }
            break;
        case 'export':
            exportArticles('csv');
            break;
        default:
            console.log('Action inconnue:', action);
    }
}

/**
 * Supprimer plusieurs articles
 */
function bulkDelete(articleIds) {
    console.log('Suppression en masse des articles:', articleIds);
    // À implémenter avec une requête AJAX
}

/**
 * Copier le code-barre dans le presse-papiers
 */
function copyBarcode(event, barcode) {
    event.preventDefault();
    navigator.clipboard.writeText(barcode).then(() => {
        showSuccess('Code-barre copié !');
    }).catch(() => {
        showError('Erreur lors de la copie');
    });
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

    const container = document.querySelector('.articles-container');
    if (container) {
        container.insertBefore(alert, container.firstChild);

        alert.querySelector('.close').addEventListener('click', function () {
            alert.style.display = 'none';
        });

        setTimeout(() => {
            alert.style.display = 'none';
        }, 3000);
    }
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

    const container = document.querySelector('.articles-container');
    if (container) {
        container.insertBefore(alert, container.firstChild);

        alert.querySelector('.close').addEventListener('click', function () {
            alert.style.display = 'none';
        });
    }
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
 * Imprimer la liste des articles
 */
function printArticlesList() {
    window.print();
}