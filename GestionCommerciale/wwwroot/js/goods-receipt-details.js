// ============================================
// GOODS RECEIPT DETAILS JAVASCRIPT
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    initializeDetails();
});

/**
 * Initialise la page de détails
 */
function initializeDetails() {
    // Masquer les boutons d'action si on est dans une modal
    if (window.opener || window.frameElement) {
        hideActionButtons();
    }

    // Ajouter les écouteurs pour l'impression
    if (window.matchMedia) {
        window.matchMedia('print').addListener(function (mq) {
            if (!mq.matches) {
                onPrintFinished();
            }
        });
    }
}

/**
 * Masque les boutons d'action (utilisé en modal)
 */
function hideActionButtons() {
    const actions = document.getElementById('receiptActions');
    if (actions) {
        actions.style.display = 'none';
    }
}

/**
 * Appelé après l'impression
 */
function onPrintFinished() {
    // Vous pouvez ajouter du code ici après l'impression
    console.log('Impression terminée');
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
        day: '2-digit'
    }).format(date);
}

/**
 * Exporte le bon en PDF
 */
function exportToPDF() {
    const element = document.querySelector('.receipt-container');
    const opt = {
        margin: 10,
        filename: 'bon-reception-' + getReceiptNumber() + '.pdf',
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 2 },
        jsPDF: { orientation: 'portrait', unit: 'mm', format: 'a4' }
    };

    // Vérifier si html2pdf est disponible
    if (typeof html2pdf !== 'undefined') {
        html2pdf().set(opt).from(element).save();
    } else {
        console.error('html2pdf n\'est pas disponible. Utilisez window.print() à la place.');
        window.print();
    }
}

/**
 * Obtient le numéro du bon de réception
 */
function getReceiptNumber() {
    const container = document.querySelector('.receipt-container');
    if (container && container.getAttribute('data-receipt-id')) {
        return container.getAttribute('data-receipt-id');
    }
    return 'bon-reception';
}

/**
 * Copie le numéro du bon dans le presse-papiers
 */
function copyReceiptNumber() {
    const number = document.querySelector('.receipt-number').textContent;
    navigator.clipboard.writeText(number).then(() => {
        showToast('Numéro copié: ' + number);
    }).catch(err => {
        console.error('Erreur lors de la copie:', err);
    });
}

/**
 * Affiche une notification toast
 */
function showToast(message, duration = 3000) {
    const toast = document.createElement('div');
    toast.className = 'toast-message';
    toast.textContent = message;
    toast.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        background-color: #333;
        color: white;
        padding: 12px 20px;
        border-radius: 4px;
        z-index: 9999;
        animation: slideInRight 0.3s ease;
    `;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, duration);
}

/**
 * Valide les données du bon
 */
function validateReceipt() {
    const receipt = document.querySelector('.receipt-container');
    if (!receipt) {
        return false;
    }

    const amountTTC = parseFloat(
        document.querySelector('.amount-value').textContent
    );

    if (isNaN(amountTTC) || amountTTC <= 0) {
        console.error('Montant invalide');
        return false;
    }

    return true;
}

/**
 * Génère un résumé du bon
 */
function generateSummary() {
    const summary = {
        number: document.querySelector('.receipt-number').textContent,
        status: document.querySelector('.status-badge').textContent,
        supplier: Array.from(document.querySelectorAll('.info-row span'))
            .find(el => el.parentElement.querySelector('label').textContent.includes('Fournisseur'))?.textContent,
        date: Array.from(document.querySelectorAll('.info-row span'))
            .find(el => el.parentElement.querySelector('label').textContent.includes('Saisie'))?.textContent,
        totalAmount: document.querySelectorAll('.amount-value')[0].textContent
    };

    return summary;
}

/**
 * Partage le bon (si les APIs de partage sont disponibles)
 */
function shareReceipt() {
    if (navigator.share) {
        navigator.share({
            title: 'Bon de Réception',
            text: 'Bon de Réception ' + getReceiptNumber(),
            url: window.location.href
        }).catch(err => {
            if (err.name !== 'AbortError') {
                console.error('Erreur lors du partage:', err);
            }
        });
    } else {
        showToast('Le partage n\'est pas supporté par votre navigateur');
    }
}

/**
 * Affiche les statistiques du bon
 */
function showStatistics() {
    const lines = document.querySelectorAll('.receipt-lines-table tbody tr');
    const stats = {
        totalLines: lines.length,
        totalQuantity: 0,
        totalAmount: 0
    };

    lines.forEach(line => {
        const cells = line.querySelectorAll('td');
        if (cells.length > 0) {
            stats.totalQuantity += parseInt(cells[3].textContent);
            const amount = parseFloat(cells[7].textContent.replace(' €', '').replace(',', '.'));
            stats.totalAmount += amount;
        }
    });

    console.log('Statistiques du bon:', stats);
    return stats;
}

/**
 * Prépare le bon pour l'impression
 */
function prepareForPrint() {
    // Masquer les boutons d'action
    const actions = document.getElementById('receiptActions');
    if (actions) {
        actions.style.display = 'none';
    }

    // Appliquer les styles d'impression
    document.querySelector('.receipt-container').classList.add('print-mode');
}

/**
 * Restaure le bon après l'impression
 */
function restoreAfterPrint() {
    const actions = document.getElementById('receiptActions');
    if (actions) {
        actions.style.display = '';
    }

    document.querySelector('.receipt-container').classList.remove('print-mode');
}

/**
 * Styles pour l'impression
 */
if (typeof document !== 'undefined') {
    const style = document.createElement('style');
    style.textContent = `
        @media print {
            body {
                margin: 0;
                padding: 0;
            }

            .receipt-container {
                max-width: 100%;
                padding: 20px;
            }

            #receiptActions {
                display: none !important;
            }

            .receipt-section {
                page-break-inside: avoid;
            }

            a {
                color: inherit;
                text-decoration: none;
            }
        }

        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }

        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
}

// Exposer les fonctions globalement pour les appels HTML
window.formatCurrency = formatCurrency;
window.formatDate = formatDate;
window.exportToPDF = exportToPDF;
window.copyReceiptNumber = copyReceiptNumber;
window.showToast = showToast;
window.validateReceipt = validateReceipt;
window.generateSummary = generateSummary;
window.shareReceipt = shareReceipt;
window.showStatistics = showStatistics;
window.prepareForPrint = prepareForPrint;
window.restoreAfterPrint = restoreAfterPrint;