// ============================================
// ARTICLE DETAILS JAVASCRIPT
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    initTabSystem();
    initAlertClosing();
});

/**
 * Initialise le système d'onglets
 */
function initTabSystem() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            const tabName = this.getAttribute('data-tab');

            // Désactiver tous les onglets et boutons
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabContents.forEach(content => content.classList.remove('active'));

            // Activer l'onglet sélectionné
            this.classList.add('active');
            document.getElementById(tabName).classList.add('active');

            // Sauvegarder l'onglet actif dans sessionStorage
            sessionStorage.setItem('activeTab', tabName);
        });
    });

    // Restaurer l'onglet actif depuis sessionStorage
    const activeTab = sessionStorage.getItem('activeTab');
    if (activeTab) {
        const button = document.querySelector(`[data-tab="${activeTab}"]`);
        if (button) {
            button.click();
        }
    }
}

/**
 * Initialise la fermeture des alertes
 */
function initAlertClosing() {
    const closeButtons = document.querySelectorAll('.btn-close');

    closeButtons.forEach(button => {
        button.addEventListener('click', function () {
            const alert = this.closest('.alert');
            if (alert) {
                alert.style.animation = 'slideOut 0.3s ease';
                setTimeout(() => {
                    alert.remove();
                }, 300);
            }
        });
    });
}

/**
 * Formate un nombre en devise EUR
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
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(date);
}

/**
 * Exporte le tableau des fournisseurs en CSV
 */
function exportSuppliersToCSV() {
    const table = document.querySelector('.pricing-table');
    if (!table) return;

    let csv = 'Fournisseur,Code,Prix Gros HT,Prix Détail HT,Prix Promo HT,Marge,Remise Max,Dernier Achat\n';

    table.querySelectorAll('tbody tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        const rowData = [];
        cells.forEach(cell => {
            rowData.push('"' + cell.textContent.trim().replace(/"/g, '""') + '"');
        });
        csv += rowData.join(',') + '\n';
    });

    downloadCSV(csv, 'fournisseurs-article.csv');
}

/**
 * Exporte l'historique du stock en CSV
 */
function exportStockHistoryToCSV() {
    const table = document.querySelector('.movements-table');
    if (!table) return;

    let csv = 'Date,Type,Quantité,Référence,Commentaire\n';

    table.querySelectorAll('tbody tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        const rowData = [];
        cells.forEach(cell => {
            rowData.push('"' + cell.textContent.trim().replace(/"/g, '""') + '"');
        });
        csv += rowData.join(',') + '\n';
    });

    downloadCSV(csv, 'historique-stock.csv');
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
 * Copie le texte dans le presse-papiers
 */
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showNotification('Copié dans le presse-papiers', 'success');
    }).catch(err => {
        showNotification('Erreur lors de la copie', 'error');
        console.error(err);
    });
}

/**
 * Affiche une notification
 */
function showNotification(message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.role = 'alert';
    alertDiv.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    const container = document.querySelector('.article-container');
    if (container) {
        container.insertBefore(alertDiv, container.firstChild);

        // Auto-fermer après 3 secondes
        setTimeout(() => {
            const closeBtn = alertDiv.querySelector('.btn-close');
            if (closeBtn) {
                closeBtn.click();
            }
        }, 3000);
    }
}

/**
 * Imprime la page
 */
function printDetails() {
    window.print();
}

/**
 * Valide les tarifs (exemple)
 */
function validatePricings() {
    const table = document.querySelector('.pricing-table');
    if (!table) return;

    let hasErrors = false;
    table.querySelectorAll('tbody tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        // Exemple : vérifier que le prix gros < prix détail
        const wholesale = parseFloat(cells[1].textContent);
        const retail = parseFloat(cells[2].textContent);

        if (wholesale > retail) {
            row.style.backgroundColor = '#ffe6e6';
            hasErrors = true;
        }
    });

    if (hasErrors) {
        showNotification('Attention : Certains tarifs semblent incorrects', 'warning');
    } else {
        showNotification('Tous les tarifs sont valides', 'success');
    }
}

/**
 * Anime les compteurs de stock au chargement
 */
function animateStockCards() {
    const cards = document.querySelectorAll('.stock-card .stock-value');

    cards.forEach(card => {
        const finalValue = parseInt(card.textContent);
        let currentValue = 0;
        const increment = Math.ceil(finalValue / 20);

        const interval = setInterval(() => {
            currentValue += increment;
            if (currentValue >= finalValue) {
                currentValue = finalValue;
                clearInterval(interval);
            }
            card.textContent = currentValue;
        }, 30);
    });
}

/**
 * Filtre l'historique du stock par type
 */
function filterMovementsByType(type) {
    const table = document.querySelector('.movements-table');
    if (!table) return;

    table.querySelectorAll('tbody tr').forEach(row => {
        const typeCell = row.querySelector('[data-label="Type"]');
        if (type === 'all' || typeCell.textContent.toLowerCase().includes(type.toLowerCase())) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

/**
 * Tri les tableaux
 */
function sortTable(tableSelector, columnIndex, order = 'asc') {
    const table = document.querySelector(tableSelector);
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
            ? aValue.localeCompare(bValue)
            : bValue.localeCompare(aValue);
    });

    rows.forEach(row => tbody.appendChild(row));
}

// Animation au démarrage
window.addEventListener('load', function () {
    animateStockCards();
});