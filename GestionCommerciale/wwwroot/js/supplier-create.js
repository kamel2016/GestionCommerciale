// supplier-create.js - Script pour la page de création de fournisseur

document.addEventListener('DOMContentLoaded', function () {
    let currentStep = 1;
    const totalSteps = 8;
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');
    const nextBtn = document.getElementById('nextBtn');
    const prevBtn = document.getElementById('prevBtn');
    const createBtn = document.getElementById('createBtn');
    const progressBar = document.getElementById('progressBar');
    const stepIndicator = document.getElementById('stepIndicator');

    // Initialiser la page
    updateProgressBar();
    updateNavigationButtons();
    updateStepIndicator();

    // Gestion des onglets
    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            const step = parseInt(this.getAttribute('data-step'));
            const tabId = this.getAttribute('data-tab');

            if (step > currentStep && !validateCurrentStep()) {
                return;
            }

            switchToStep(step, tabId);
        });
    });

    // Bouton Suivant
    nextBtn.addEventListener('click', function () {
        if (validateCurrentStep()) {
            if (currentStep < totalSteps) {
                const nextTab = document.querySelector(`[data-step="${currentStep + 1}"]`);
                switchToStep(currentStep + 1, nextTab.getAttribute('data-tab'));
            }
        } else {
            showValidationErrors();
        }
    });

    // Bouton Précédent
    prevBtn.addEventListener('click', function () {
        if (currentStep > 1) {
            const prevTab = document.querySelector(`[data-step="${currentStep - 1}"]`);
            switchToStep(currentStep - 1, prevTab.getAttribute('data-tab'));
        }
    });

    // Fonction pour changer d'étape
    function switchToStep(step, tabId) {
        tabButtons.forEach(btn => btn.classList.remove('active'));
        tabContents.forEach(content => content.classList.remove('active'));

        const activeButton = document.querySelector(`[data-step="${step}"]`);
        const activeContent = document.getElementById(tabId);

        if (activeButton && activeContent) {
            activeButton.classList.add('active');
            activeContent.classList.add('active');
            currentStep = step;

            updateProgressBar();
            updateNavigationButtons();
            updateStepIndicator();

            if (tabId === 'summary') {
                updateSummary();
            }

            sessionStorage.setItem('currentSupplierStep', step);

            document.querySelector('.supplier-container').scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    }

    // Validation de l'étape courante
    function validateCurrentStep() {
        const currentTab = document.querySelector('.tab-content.active');
        if (!currentTab) return true;

        const requiredInputs = currentTab.querySelectorAll('input[required], select[required]');
        let isValid = true;

        requiredInputs.forEach(input => {
            const value = input.type === 'checkbox' ? input.checked : input.value.trim();
            if (!value) {
                input.classList.add('input-validation-error');
                showFieldError(input, 'Ce champ est obligatoire');
                isValid = false;
            } else {
                input.classList.remove('input-validation-error');
                hideFieldError(input);
            }
        });

        if (currentStep === 1) {
            isValid = validateGeneralTab() && isValid;
        } else if (currentStep === 2) {
            isValid = validateContactTab() && isValid;
        } else if (currentStep === 3) {
            isValid = validateAddressTab() && isValid;
        }

        const currentTabButton = document.querySelector(`[data-step="${currentStep}"]`);
        if (!isValid) {
            currentTabButton.classList.add('has-error');
        } else {
            currentTabButton.classList.remove('has-error');
        }

        return isValid;
    }

    // Validation onglet général
    function validateGeneralTab() {
        let isValid = true;
        const codeInput = document.querySelector('[name="Supplier.CodeFournisseur"]');
        const raisonInput = document.querySelector('[name="Supplier.RaisonSociale"]');

        if (codeInput && codeInput.value.length < 3) {
            showFieldError(codeInput, 'Le code doit contenir au moins 3 caractères');
            isValid = false;
        }

        if (raisonInput && raisonInput.value.length < 2) {
            showFieldError(raisonInput, 'La raison sociale doit contenir au moins 2 caractères');
            isValid = false;
        }

        return isValid;
    }

    // Validation onglet contact
    function validateContactTab() {
        let isValid = true;
        const emailInput = document.querySelector('[name="Supplier.Email"]');

        if (emailInput && emailInput.value && !isValidEmail(emailInput.value)) {
            showFieldError(emailInput, 'Format d\'email invalide');
            isValid = false;
        }

        return isValid;
    }

    // Validation onglet adresse
    function validateAddressTab() {
        let isValid = true;
        const postalInput = document.querySelector('[name="Supplier.CodePostal"]');

        if (postalInput && postalInput.value && !/^\d{5}$/.test(postalInput.value)) {
            showFieldError(postalInput, 'Le code postal doit contenir 5 chiffres');
            isValid = false;
        }

        return isValid;
    }

    // Afficher erreur sur un champ
    function showFieldError(input, message) {
        input.classList.add('input-validation-error');
        let errorSpan = input.parentElement.querySelector('.field-validation-error');
        if (errorSpan) {
            errorSpan.textContent = message;
            errorSpan.style.display = 'block';
        }
    }

    // Masquer erreur sur un champ
    function hideFieldError(input) {
        input.classList.remove('input-validation-error');
        let errorSpan = input.parentElement.querySelector('.field-validation-error');
        if (errorSpan) {
            errorSpan.textContent = '';
            errorSpan.style.display = 'none';
        }
    }

    // Afficher toutes les erreurs de validation
    function showValidationErrors() {
        const errorInputs = document.querySelectorAll('.input-validation-error');
        if (errorInputs.length > 0) {
            errorInputs[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
            errorInputs[0].focus();
        }
    }

    // Mise à jour de la barre de progression
    function updateProgressBar() {
        const progress = (currentStep / totalSteps) * 100;
        progressBar.style.width = progress + '%';
    }

    // Mise à jour de l'indicateur d'étape
    function updateStepIndicator() {
        stepIndicator.textContent = `Étape ${currentStep} sur ${totalSteps}`;
    }

    // Mise à jour des boutons de navigation
    function updateNavigationButtons() {
        if (currentStep === 1) {
            prevBtn.style.display = 'none';
        } else {
            prevBtn.style.display = 'inline-flex';
        }

        if (currentStep === totalSteps) {
            nextBtn.style.display = 'none';
            createBtn.style.display = 'inline-flex';
        } else {
            nextBtn.style.display = 'inline-flex';
            createBtn.style.display = 'none';
        }
    }

    // Mise à jour du résumé
    function updateSummary() {
        document.getElementById('summary-code').textContent =
            document.querySelector('[name="Supplier.CodeFournisseur"]').value || '-';
        document.getElementById('summary-company').textContent =
            document.querySelector('[name="Supplier.RaisonSociale"]').value || '-';
        document.getElementById('summary-contact').textContent =
            document.querySelector('[name="Supplier.ContactPrincipal"]').value || '-';

        const typeSelect = document.querySelector('[name="Supplier.TypeFournisseur"]');
        document.getElementById('summary-type').textContent =
            typeSelect.selectedOptions[0]?.text || '-';

        document.getElementById('summary-email').textContent =
            document.querySelector('[name="Supplier.Email"]').value || '-';
        document.getElementById('summary-phone').textContent =
            document.querySelector('[name="Supplier.Telephone"]').value || '-';

        const address = document.querySelector('[name="Supplier.Adresse"]').value;
        const city = document.querySelector('[name="Supplier.Ville"]').value;
        const postalCode = document.querySelector('[name="Supplier.CodePostal"]').value;
        const fullAddress = [address, postalCode, city].filter(x => x).join(', ');
        document.getElementById('summary-address').textContent = fullAddress || '-';

        const paymentSelect = document.querySelector('[name="Supplier.ConditionPaiement"]');
        document.getElementById('summary-payment').textContent =
            paymentSelect.selectedOptions[0]?.text || '-';

        const delivery = document.querySelector('[name="Supplier.DelaiLivraison"]').value;
        document.getElementById('summary-delivery').textContent =
            delivery ? delivery + ' jours' : '-';

        const discount = document.querySelector('[name="Supplier.RemiseNegociee"]').value;
        document.getElementById('summary-discount').textContent =
            discount ? discount + '%' : '-';
    }

    // Validation en temps réel du code fournisseur
    const codeSupplierInput = document.querySelector('[data-validation-url*="CheckCodeSupplier"]');
    if (codeSupplierInput) {
        let timeout;
        codeSupplierInput.addEventListener('input', function () {
            clearTimeout(timeout);
            const input = this;
            const value = input.value.trim();

            if (value.length >= 3) {
                timeout = setTimeout(() => {
                    checkCodeSupplier(input, value);
                }, 500);
            }
        });
    }

    // Validation en temps réel de l'email
    const emailInput = document.querySelector('input[data-validation-url*="CheckEmail"][type="email"]');
    if (emailInput) {
        let emailTimeout;
        emailInput.addEventListener('input', function () {
            clearTimeout(emailTimeout);
            const input = this;
            const value = input.value.trim();

            if (value && isValidEmail(value)) {
                emailTimeout = setTimeout(() => {
                    checkEmail(input, value);
                }, 500);
            }
        });
    }

    // Suggestions de ville basées sur le code postal
    const postalCodeInput = document.querySelector('[data-city-suggestions]');
    if (postalCodeInput) {
        postalCodeInput.addEventListener('input', function () {
            const postalCode = this.value.trim();
            if (postalCode.length === 5) {
                getCitySuggestions(postalCode);
            }
        });
    }

    // Fermeture des alertes
    document.querySelectorAll('.alert .close').forEach(closeBtn => {
        closeBtn.addEventListener('click', function () {
            this.parentElement.style.display = 'none';
        });
    });

    // Bouton d'aide flottant
    const helpBtn = document.getElementById('helpBtn');
    const helpTooltip = document.getElementById('helpTooltip');
    let helpActive = false;

    if (helpBtn) {
        helpBtn.addEventListener('click', function () {
            helpActive = !helpActive;
            this.classList.toggle('active', helpActive);

            if (helpActive) {
                showContextualHelp();
            } else {
                hideContextualHelp();
            }
        });
    }

    // Affichage de l'aide contextuelle
    function showContextualHelp() {
        const currentTab = document.querySelector('.tab-content.active');
        const helpTexts = {
            'general': 'Remplissez les informations de base. Le code fournisseur doit être unique.',
            'contact': 'Ajoutez les coordonnées de contact du fournisseur.',
            'address': 'Précisez l\'adresse complète pour les livraisons.',
            'commercial': 'Définissez les conditions commerciales négociées.',
            'fiscal': 'Renseignez les identifiants fiscaux pour la comptabilité.',
            'financial': 'Ajoutez les coordonnées bancaires pour les paiements.',
            'notes': 'Notez toute information complémentaire utile.',
            'summary': 'Vérifiez toutes les informations avant de créer le fournisseur.'
        };

        helpTooltip.textContent = helpTexts[currentTab.id] || 'Aide contextuelle';
        helpTooltip.classList.add('show');
    }

    function hideContextualHelp() {
        helpTooltip.classList.remove('show');
    }

    // Restaurer l'étape sauvegardée
    const savedStep = sessionStorage.getItem('currentSupplierStep');
    if (savedStep && parseInt(savedStep) <= totalSteps) {
        const savedTabButton = document.querySelector(`[data-step="${savedStep}"]`);
        if (savedTabButton) {
            switchToStep(parseInt(savedStep), savedTabButton.getAttribute('data-tab'));
        }
    }

    // Prévenir la perte de données
    let formChanged = false;
    let formSubmitted = false;

    document.querySelectorAll('input, select, textarea').forEach(field => {
        field.addEventListener('change', () => { formChanged = true; });
        field.addEventListener('input', () => { formChanged = true; });
    });

    document.getElementById('createSupplierForm').addEventListener('submit', function () {
        formSubmitted = true;
    });

    window.addEventListener('beforeunload', function (e) {
        if (formChanged && !formSubmitted) {
            e.preventDefault();
            e.returnValue = 'Vous avez des modifications non sauvegardées. Voulez-vous vraiment quitter ?';
        }
    });

    // Raccourcis clavier
    document.addEventListener('keydown', function (e) {
        if (e.ctrlKey || e.metaKey) {
            if (e.key === 's') {
                e.preventDefault();
                const currentTab = document.querySelector('.tab-content.active');
                if (currentTab.id === 'summary') {
                    document.getElementById('createBtn').click();
                }
            }
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                document.getElementById('nextBtn')?.click();
            }
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                document.getElementById('prevBtn')?.click();
            }
        }
    });

    // Nettoyage après succès
    if (window.location.search.includes('success')) {
        sessionStorage.removeItem('currentSupplierStep');
        formChanged = false;
    }
});

// Fonctions de validation AJAX
function checkCodeSupplier(input, codeSupplier) {
    const url = input.getAttribute('data-validation-url');

    fetch(`${url}?codeSupplier=${encodeURIComponent(codeSupplier)}`)
        .then(response => response.json())
        .then(data => {
            const validationSpan = input.parentElement.querySelector('.field-validation-error');
            if (data.exists) {
                input.classList.add('input-validation-error');
                if (validationSpan) {
                    validationSpan.textContent = 'Ce code fournisseur existe déjà.';
                    validationSpan.style.display = 'block';
                }
            } else {
                input.classList.remove('input-validation-error');
                if (validationSpan) {
                    validationSpan.textContent = '';
                    validationSpan.style.display = 'none';
                }
            }
        })
        .catch(error => {
            console.error('Erreur lors de la validation du code fournisseur:', error);
        });
}

function checkEmail(input, email) {
    const url = input.getAttribute('data-validation-url');

    fetch(`${url}?email=${encodeURIComponent(email)}`)
        .then(response => response.json())
        .then(data => {
            const validationSpan = input.parentElement.querySelector('.field-validation-error');
            if (data.exists) {
                input.classList.add('input-validation-error');
                if (validationSpan) {
                    validationSpan.textContent = 'Cette adresse email est déjà utilisée.';
                    validationSpan.style.display = 'block';
                }
            } else {
                input.classList.remove('input-validation-error');
                if (validationSpan) {
                    validationSpan.textContent = '';
                    validationSpan.style.display = 'none';
                }
            }
        })
        .catch(error => {
            console.error('Erreur lors de la validation de l\'email:', error);
        });
}

function getCitySuggestions(postalCode) {
    const cityInput = document.querySelector('input[name="Supplier.Ville"]');
    if (!cityInput) return;

    const url = document.querySelector('[data-city-suggestions]').getAttribute('data-city-suggestions');

    fetch(`${url}?postalCode=${postalCode}`)
        .then(response => response.json())
        .then(data => {
            if (data.suggestions && data.suggestions.length > 0) {
                cityInput.value = data.suggestions[0];
                cityInput.dispatchEvent(new Event('change'));
            }
        })
        .catch(error => {
            console.error('Erreur lors de la récupération des suggestions de ville:', error);
        });
}

function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}