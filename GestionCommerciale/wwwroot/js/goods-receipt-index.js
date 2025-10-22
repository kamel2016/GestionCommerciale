// ============================================
// GOODS RECEIPT INDEX JAVASCRIPT
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    initializeEventListeners();
    initializeAlerts();
});

/**
 * Initialise les écouteurs d'événements
 */
function initializeEventListeners() {
    // Fermeture des alertes
    const closeButtons = document.querySelectorAll('.btn-close');
    closeButtons.forEach(button => {
        button.addEventListener('click', function () {
            const alert = this.closest('.alert');
            if (alert) {
                closeAlert(alert);
            }
        });
    });

    // Validation du formulaire de filtres
    const filterForm = document.querySelector('.filters-form');
    if (filterForm) {
        filterForm.addEventListener('submit', function (e) {
            validateFilterForm(e);
        });
    }
}

/**
 * Initialise la fermeture automatique des alertes
 */
function initializeAlerts() {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        // Fermer automatiquement après 5 secondes
        setTimeout(() => {
            closeAlert(alert);
        }, 5000);
    });
}

/**
 * Ferme une alerte avec animation
 */
function closeAlert(alert) {
    alert.style.animation = 'slideOut 0.3s ease';
    setTimeout(() => {
        alert.remove();
    }, 300);
}

/**
 * Ouvre le bon de réception dans une popup
 */
function openReceiptPopup(receiptId) {
    const modal = new bootstrap.Modal(document.getElementById('receiptModal'));
    const modalBody = document.getElementById('receiptModalBody');

    // Afficher un chargement
    modalBody.innerHTML = '<div class="text-center"><i class="fas fa-spinner fa-spin"></i> Chargement...</div>';

    // Charger le contenu via AJAX
    fetch(`/GoodsReceipt/Details/${receiptId}`)
        .then(response => response.text())
        .then(html => {
            modalBody.innerHTML = html;
            modal.show();
        })
        .catch(error => {
            console.error('Erreur:', error);
            modalBody.innerHTML = '<div class="alert alert-danger"><i class="fas fa-exclamation-circle"></i> Erreur lors du chargement du bon.</div>';
        });
}

/**
 * Imprime le bon de réception
 */
function printReceipt(receiptId) {
    // Ouvrir dans une nouvelle fenêtre en mode impression
    const printWindow = window.open(`/GoodsReceipt/Details/${receiptId}?print=true`, '_blank');
    if (printWindow) {
        printWindow.addEventListener('load', function () {
            printWindow.print();
        });
    }
}

/**
 * Imprime depuis la modal
 */
function printReceiptFromModal() {
    const modalBody = document.getElementById('receiptModalBody');
    const receiptId = modalBody.getAttribute('data-receipt-id');

    if (receiptId) {
        printReceipt(receiptId);
    }
}

/**
 * Annule un bon de réception
 */
function cancelReceipt(receiptId) {
    if (confirm('Êtes-vous sûr de vouloir annuler ce bon de réception ? Cette action est irréversible.')) {
        // Appeler l'API pour annuler le bon
        fetch(`/GoodsReceipt/${receiptId}/Cancel`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(response => {
                if (response.ok) {
                    showNotification('Bon annulé avec succès', 'success');
                    setTimeout(() => {
                        location.reload();
                    }, 1500);
                } else {
                    showNotification('Erreur lors de l\'annulation', 'error');
                }
            })
            .catch(error => {
                console.error('Erreur:', error);
                showNotification('Erreur lors de l\'annulation', 'error');
            });
    }
}

/**
 * Affiche une notification
 */
function showNotification(message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.role = 'alert';

    const icon = type === 'success' ? 'check-circle' : 'exclamation-circle';
    alertDiv.innerHTML = `
        <i class="fas fa-${icon}"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    const container = document.querySelector('.container-fluid');
    if (container) {
        const pageHeader = container.querySelector('.page-header');
        if (pageHeader) {
            pageHeader.insertAdjacentElement('afterend', alertDiv);
        } else {
            container.insertBefore(alertDiv, container.firstChild);
        }

        // Auto-fermer après 5 secondes
        setTimeout(() => {
            closeAlert(alertDiv);
        }, 5000);
    }
}

/**
 * Valide le formulaire de filtres
 */
function validateFilterForm(e) {
    const dateFrom = document.querySelector('input[name="dateFrom"]').value;
    const dateTo = document.querySelector('input[name="dateTo"]').value;

    if (dateFrom && dateTo && new Date(dateFrom) > new Date(dateTo)) {
        e.preventDefault();
        showNotification('La date de début doit être antérieure à la date de fin', 'error');
        return false;
    }

    return true;
}

/**
 * Réinitialise les filtres
 */
function resetFilters() {
    document.querySelector('.filters-form').reset();
    location.href = '/GoodsReceipt';
}

/**
 * Exporte le tableau en CSV
 */
function exportTableToCSV() {
    const table = document.querySelector('.receipt-table');
    if (!table) return;

    let csv = 'N° Bon,Date,Fournisseur,Code Fournisseur,Montant HT,Montant TTC,Statut,Créé par\n';

    table.querySelectorAll('tbody tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        const rowData = [];
        cells.forEach((cell, index) => {
            // Sauter la colonne Actions
            if (index < 8) {
                rowData.push('"' + cell.textContent.trim().replace(/"/g, '""') + '"');
            }
        });
        csv += rowData.join(',') + '\n';
    });

    downloadCSV(csv, 'bons-reception.csv');
}

/**
 * Télécharge un fichier CSV
 */
function downloadCSV(csv, filename) {
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv);
    link.download = filename;
    link.click();
}

/**
 * Filtre le tableau en temps réel
 */
function filterTable(searchTerm) {
    const table = document.querySelector('.receipt-table');
    if (!table) return;

    const rows = table.querySelectorAll('tbody tr');
    const term = searchTerm.toLowerCase();

    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(term) ? '' : 'none';
    });
}

/**
 * Tri le tableau
 */
function sortTableByColumn(columnIndex, order = 'asc') {
    const table = document.querySelector('.receipt-table');
    if (!table) return;

    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr'));

    rows.sort((a, b) => {
        const aValue = a.querySelector(`td:nth-child(${columnIndex + 1})`).textContent.trim();
        const bValue = b.querySelector(`td:nth-child(${columnIndex + 1})`).textContent.trim();

        // Essayer de convertir en nombre
        const aNum = parseFloat(aValue);
        const bNum = parseFloat(bValue);

        if (!isNaN(aNum) && !isNaN(bNum)) {
            return order === 'asc' ? aNum - bNum : bNum - aNum;
        }

        // Sinon, trier comme texte
        return order === 'asc'
            ? aValue.localeCompare(bValue, 'fr')
            : bValue.localeCompare(aValue, 'fr');
    });

    rows.forEach(row => tbody.appendChild(row));
}

/**
 * Formate un montant en devise EUR
 */
function formatCurrency(value) {
    return new Intl.NumberFormat('fr-FR', {
        style: 'currency',
        currency: 'EUR',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value);
}

/**
 * Formate une date
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('fr-FR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
    }).format(date);
}

/**
 * Bascule la sélection multiple des lignes
 */
function toggleRowSelection() {
    const checkboxes = document.querySelectorAll('.receipt-table tbody input[type="checkbox"]');
    const selectAllCheckbox = document.querySelector('.receipt-table thead input[type="checkbox"]');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            checkboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
                checkbox.closest('tr').classList.toggle('selected', this.checked);
            });
        });

        checkboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function () {
                this.closest('tr').classList.toggle('selected', this.checked);
            });
        });
    }
}

/**
 * Effectue une action en masse
 */
function bulkAction(action) {
    const selectedCheckboxes = document.querySelectorAll('.receipt-table tbody input[type="checkbox"]:checked');

    if (selectedCheckboxes.length === 0) {
        showNotification('Veuillez sélectionner au moins un bon', 'warning');
        return;
    }

    const ids = Array.from(selectedCheckboxes).map(checkbox => checkbox.value);

    if (confirm(`Êtes-vous sûr de vouloir ${action} ${ids.length} bon(s) ?`)) {
        fetch('/GoodsReceipt/BulkAction', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                action: action,
                ids: ids
            })
        })
            .then(response => {
                if (response.ok) {
                    showNotification(`${ids.length} bon(s) ${action}(s) avec succès`, 'success');
                    setTimeout(() => {
                        location.reload();
                    }, 1500);
                } else {
                    showNotification('Erreur lors de l\'opération', 'error');
                }
            })
            .catch(error => {
                console.error('Erreur:', error);
                showNotification('Erreur lors de l\'opération', 'error');
            });
    }
}