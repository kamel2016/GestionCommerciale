// Gestion des alertes auto-dismiss
document.addEventListener('DOMContentLoaded', function () {
    // Auto-fermeture des alertes après 5 secondes
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });

    // Fermeture manuelle des alertes
    document.querySelectorAll('.alert .close').forEach(button => {
        button.addEventListener('click', function () {
            const alert = this.closest('.alert');
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        });
    });

    // Gestion du modal de suppression
    const deleteModal = document.getElementById('deleteModal');
    if (deleteModal) {
        deleteModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const categoryId = button.getAttribute('data-category-id');
            const categoryName = button.getAttribute('data-category-name');

            document.getElementById('categoryIdInput').value = categoryId;
            document.getElementById('categoryName').textContent = categoryName;
        });
    }

    // Confirmation avant changement de statut
    document.querySelectorAll('form[asp-page-handler="ToggleStatus"]').forEach(form => {
        form.addEventListener('submit', function (e) {
            const button = this.querySelector('button');
            const isActive = button.querySelector('i').classList.contains('fa-toggle-on');
            const action = isActive ? 'désactiver' : 'activer';

            if (!confirm(`Voulez-vous vraiment ${action} cette catégorie ?`)) {
                e.preventDefault();
            }
        });
    });

    // Animation au survol des lignes du tableau
    const rows = document.querySelectorAll('.categories-table tbody tr');
    rows.forEach(row => {
        row.addEventListener('mouseenter', function () {
            this.style.transform = 'scale(1.01)';
        });

        row.addEventListener('mouseleave', function () {
            this.style.transform = 'scale(1)';
        });
    });
});

// Fonction pour réinitialiser les filtres
function resetFilters() {
    window.location.href = window.location.pathname;
}

// Raccourcis clavier
document.addEventListener('keydown', function (e) {
    // Ctrl/Cmd + K pour focus sur la recherche
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        document.querySelector('.search-input').focus();
    }

    // Ctrl/Cmd + N pour nouvelle catégorie
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        const newButton = document.querySelector('a[href*="Edit"]');
        if (newButton) {
            window.location.href = newButton.href;
        }
    }
});