// ============================================
// GESTION DU FORMULAIRE D'ÉDITION DE CATÉGORIE
// ============================================

document.addEventListener('DOMContentLoaded', function () {

    // ============================================
    // INITIALISATION DES ÉLÉMENTS DU DOM
    // ============================================
    const form = document.querySelector('.category-form');
    const codeInput = document.getElementById('Category_Code');
    const nameInput = document.getElementById('Category_Name');
    const descriptionInput = document.getElementById('Category_Description');
    const iconInput = document.getElementById('Category_Icon');
    const colorInput = document.getElementById('Category_Color');
    const isActiveSwitch = document.getElementById('Category_IsActive');
    const parentSelect = document.getElementById('Category_ParentCategoryId');

    const previewCard = document.querySelector('.preview-card');
    const previewIcon = document.querySelector('.preview-icon');
    const previewName = document.querySelector('.preview-name');
    const previewDescription = document.querySelector('.preview-description');
    const previewCode = document.querySelector('.preview-code');
    const previewStatus = document.querySelector('.preview-status');

    // ============================================
    // INITIALISER LA PRÉVISUALISATION AVEC LES VALEURS ACTUELLES
    // ============================================
    updatePreview();

    // ============================================
    // ÉCOUTEURS D'ÉVÉNEMENTS
    // ============================================
    codeInput.addEventListener('input', handleCodeInput);
    nameInput.addEventListener('input', handleNameInput);
    descriptionInput.addEventListener('input', updatePreview);
    iconInput.addEventListener('input', handleIconInput);
    colorInput.addEventListener('input', handleColorInput);
    isActiveSwitch.addEventListener('change', handleStatusChange);

    // Validation en temps réel
    form.addEventListener('submit', handleFormSubmit);

    // ============================================
    // GESTION DU CODE
    // ============================================
    function handleCodeInput(e) {
        let value = e.target.value;

        // Convertir en majuscules et remplacer espaces par tirets
        value = value.toUpperCase().replace(/\s+/g, '-');

        // Supprimer les caractères non autorisés
        value = value.replace(/[^A-Z0-9-]/g, '');

        // Limiter les tirets consécutifs
        value = value.replace(/-+/g, '-');

        e.target.value = value;
        updatePreview();
        validateCode();
    }

    function validateCode() {
        const value = codeInput.value;
        const feedbackDiv = codeInput.parentElement.querySelector('.validation-feedback') || createFeedbackDiv(codeInput);

        if (value.length < 2) {
            showValidationError(feedbackDiv, 'Le code doit contenir au moins 2 caractères');
            return false;
        } else if (value.length > 20) {
            showValidationError(feedbackDiv, 'Le code ne peut pas dépasser 20 caractères');
            return false;
        } else {
            showValidationSuccess(feedbackDiv, 'Code valide');
            return true;
        }
    }

    // ============================================
    // GESTION DU NOM
    // ============================================
    function handleNameInput(e) {
        updatePreview();
        validateName();
    }

    function validateName() {
        const value = nameInput.value.trim();
        const feedbackDiv = nameInput.parentElement.querySelector('.validation-feedback') || createFeedbackDiv(nameInput);

        if (value.length < 2) {
            showValidationError(feedbackDiv, 'Le nom doit contenir au moins 2 caractères');
            return false;
        } else if (value.length > 100) {
            showValidationError(feedbackDiv, 'Le nom ne peut pas dépasser 100 caractères');
            return false;
        } else {
            showValidationSuccess(feedbackDiv, 'Nom valide');
            return true;
        }
    }

    // ============================================
    // GESTION DE L'ICÔNE
    // ============================================
    function handleIconInput(e) {
        updatePreview();
        validateIcon();
    }

    function validateIcon() {
        const value = iconInput.value.trim();
        const feedbackDiv = iconInput.parentElement.querySelector('.validation-feedback') || createFeedbackDiv(iconInput);

        if (value && !value.startsWith('fas fa-') && !value.startsWith('far fa-') && !value.startsWith('fab fa-')) {
            showValidationError(feedbackDiv, 'L\'icône doit commencer par "fas fa-", "far fa-" ou "fab fa-"');
            return false;
        } else if (value) {
            showValidationSuccess(feedbackDiv, 'Icône valide');
            return true;
        } else {
            feedbackDiv.style.display = 'none';
            return true;
        }
    }

    // ============================================
    // GESTION DE LA COULEUR
    // ============================================
    function handleColorInput(e) {
        updatePreview();
    }

    // ============================================
    // GESTION DU STATUT
    // ============================================
    function handleStatusChange(e) {
        updatePreview();
    }

    // ============================================
    // MISE À JOUR DE LA PRÉVISUALISATION
    // ============================================
    function updatePreview() {
        // Récupérer les valeurs actuelles
        const code = codeInput.value || 'CODE';
        const name = nameInput.value || 'Nom de la catégorie';
        const description = descriptionInput.value || 'Description de la catégorie';
        const icon = iconInput.value || 'fas fa-folder';
        const color = colorInput.value || '#2196F3';
        const isActive = isActiveSwitch.checked;

        // Mettre à jour la carte de prévisualisation
        previewCard.style.borderColor = color;
        previewCard.style.background = `linear-gradient(135deg, ${color}15 0%, ${color}05 100%)`;

        previewIcon.className = icon;
        previewIcon.style.color = color;

        previewName.textContent = name;
        previewName.style.color = color;

        previewDescription.textContent = description;

        previewCode.textContent = code;
        previewCode.style.background = `${color}20`;
        previewCode.style.color = color;

        // Mettre à jour le statut
        if (isActive) {
            previewStatus.innerHTML = '<i class="fas fa-check-circle"></i> Active';
            previewStatus.className = 'preview-status status-active';
        } else {
            previewStatus.innerHTML = '<i class="fas fa-times-circle"></i> Inactive';
            previewStatus.className = 'preview-status status-inactive';
        }
    }

    // ============================================
    // VALIDATION DU FORMULAIRE
    // ============================================
    function handleFormSubmit(e) {
        let isValid = true;

        // Valider tous les champs
        if (!validateCode()) isValid = false;
        if (!validateName()) isValid = false;
        if (!validateIcon()) isValid = false;

        if (!isValid) {
            e.preventDefault();

            // Afficher un message d'erreur général
            showFormError('Veuillez corriger les erreurs avant de soumettre le formulaire.');

            // Scroller vers la première erreur
            const firstError = document.querySelector('.validation-feedback.error');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    }

    // ============================================
    // FONCTIONS UTILITAIRES
    // ============================================
    function createFeedbackDiv(inputElement) {
        const feedbackDiv = document.createElement('div');
        feedbackDiv.className = 'validation-feedback';
        inputElement.parentElement.appendChild(feedbackDiv);
        return feedbackDiv;
    }

    function showValidationError(feedbackDiv, message) {
        feedbackDiv.textContent = message;
        feedbackDiv.className = 'validation-feedback error';
        feedbackDiv.style.display = 'block';
    }

    function showValidationSuccess(feedbackDiv, message) {
        feedbackDiv.textContent = message;
        feedbackDiv.className = 'validation-feedback success';
        feedbackDiv.style.display = 'block';
    }

    function showFormError(message) {
        // Chercher ou créer la div d'erreur générale
        let errorDiv = document.querySelector('.form-error-message');

        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'form-error-message';
            form.insertBefore(errorDiv, form.firstChild);
        }

        errorDiv.innerHTML = `
            <i class="fas fa-exclamation-triangle"></i>
            <span>${message}</span>
        `;
        errorDiv.style.display = 'flex';

        // Masquer après 5 secondes
        setTimeout(() => {
            errorDiv.style.display = 'none';
        }, 5000);
    }

    // ============================================
    // CONFIRMATION DE SUPPRESSION
    // ============================================
    const deleteButton = document.querySelector('.btn-danger');
    if (deleteButton) {
        deleteButton.addEventListener('click', function (e) {
            const categoryName = nameInput.value || 'cette catégorie';
            if (!confirm(`Êtes-vous sûr de vouloir supprimer "${categoryName}" ?\n\nCette action est irréversible.`)) {
                e.preventDefault();
            }
        });
    }

    // ============================================
    // ANIMATION D'ENTRÉE
    // ============================================
    setTimeout(() => {
        form.classList.add('loaded');
    }, 100);
});