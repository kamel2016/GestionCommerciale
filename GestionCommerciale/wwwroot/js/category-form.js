//// ============================================
//// GESTION DE LA PRÉVISUALISATION EN TEMPS RÉEL
//// ============================================

//document.addEventListener('DOMContentLoaded', function () {
//    // Écouter les changements sur les champs
//    const codeInput = document.getElementById('Category_Code');
//    const nameInput = document.getElementById('Category_Name');
//    const iconInput = document.getElementById('iconInput');
//    const colorPicker = document.getElementById('colorPicker');

//    if (codeInput) {
//        codeInput.addEventListener('input', updatePreview);
//    }
//    if (nameInput) {
//        nameInput.addEventListener('input', updatePreview);
//    }
//    if (iconInput) {
//        iconInput.addEventListener('input', updateIconPreview);
//    }
//    if (colorPicker) {
//        colorPicker.addEventListener('input', updateColorPreview);
//    }

//    // Initialiser la prévisualisation
//    updatePreview();
//});

//function updatePreview() {
//    const code = document.getElementById('Category_Code')?.value || 'CODE';
//    const name = document.getElementById('Category_Name')?.value || 'Nom de la catégorie';

//    const previewCode = document.getElementById('previewCode');
//    const previewName = document.getElementById('previewName');

//    if (previewCode) previewCode.textContent = code;
//    if (previewName) previewName.textContent = name;
//}

//function updateIconPreview() {
//    const iconValue = document.getElementById('iconInput')?.value || 'fas fa-box';
//    const iconPreview = document.getElementById('iconPreview');
//    const previewIcon = document.getElementById('previewIcon');

//    if (iconPreview) {
//        iconPreview.innerHTML = `<i class="${iconValue}"></i>`;
//    }
//    if (previewIcon) {
//        previewIcon.innerHTML = `<i class="${iconValue}"></i>`;
//    }
//}

//function updateColorPreview() {
//    const color = document.getElementById('colorPicker')?.value || '#2196F3';
//    const colorText = document.getElementById('colorText');
//    const previewIcon = document.getElementById('previewIcon');

//    if (colorText) {
//        colorText.value = color;
//    }
//    if (previewIcon) {
//        previewIcon.style.backgroundColor = color;
//    }
//}

//function updateColorFromText() {
//    const colorText = document.getElementById('colorText')?.value;
//    const colorPicker = document.getElementById('colorPicker');
//    const previewIcon = document.getElementById('previewIcon');

//    // Valider le format hexadécimal
//    if (colorText && /^#[0-9A-F]{6}$/i.test(colorText)) {
//        if (colorPicker) {
//            colorPicker.value = colorText;
//        }
//        if (previewIcon) {
//            previewIcon.style.backgroundColor = colorText;
//        }
//    }
//}

// category-form.js - Formulaire de catégorie (Create / Edit)

function updateIconPreview() {
    const input = document.getElementById('iconInput');
    const preview = document.getElementById('iconPreview');
    const previewIcon = document.querySelector('#previewIcon i');
    const value = (input?.value || '').trim() || 'fas fa-box';

    if (preview) preview.innerHTML = '<i class="' + escapeAttr(value) + '"></i>';
    if (previewIcon) previewIcon.className = value;
}

function updateColorPreview() {
    const picker = document.getElementById('colorPicker');
    const text = document.getElementById('colorText');
    if (!picker) return;

    if (text) text.value = picker.value.toUpperCase();
    applyColor(picker.value);
}

function updateColorFromText() {
    const picker = document.getElementById('colorPicker');
    const text = document.getElementById('colorText');
    if (!picker || !text) return;

    if (/^#[0-9A-Fa-f]{6}$/.test(text.value)) {
        picker.value = text.value;
        applyColor(text.value);
    } else {
        text.value = picker.value.toUpperCase(); // valeur invalide : on restaure
    }
}

function applyColor(color) {
    const previewIcon = document.getElementById('previewIcon');
    if (previewIcon) previewIcon.style.backgroundColor = color;
}

function escapeAttr(s) {
    return s.replace(/[^a-zA-Z0-9 _-]/g, ''); // une classe CSS n'a besoin que de ces caractères
}

document.addEventListener('DOMContentLoaded', function () {
    const nameInput = document.querySelector('[name="Category.Name"]');
    const codeInput = document.querySelector('[name="Category.Code"]');
    const previewName = document.getElementById('previewName');
    const previewCode = document.getElementById('previewCode');
    const picker = document.getElementById('colorPicker');
    const text = document.getElementById('colorText');

    // Aperçu en direct
    function syncPreview() {
        if (previewName) previewName.textContent = nameInput?.value.trim() || 'Nom de la catégorie';
        if (previewCode) previewCode.textContent = codeInput?.value.trim().toUpperCase() || 'CODE';
    }

    nameInput?.addEventListener('input', syncPreview);
    codeInput?.addEventListener('input', function () {
        this.value = this.value.toUpperCase();
        syncPreview();
    });

    // Initialisation (important pour la page Edit : valeurs déjà remplies)
    if (picker && text) text.value = picker.value.toUpperCase();
    if (picker) applyColor(picker.value);
    updateIconPreview();
    syncPreview();

    // Alertes
    document.querySelectorAll('.alert').forEach(alert => {
        const hide = () => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        };
        alert.querySelector('.close')?.addEventListener('click', hide);
        setTimeout(hide, 5000);
    });

    // Protection contre la perte de données
    const form = document.querySelector('.category-form');
    let changed = false;
    if (form) {
        form.addEventListener('change', () => changed = true);
        form.addEventListener('submit', () => changed = false);
        window.addEventListener('beforeunload', e => {
            if (changed) { e.preventDefault(); e.returnValue = ''; }
        });
    }
});