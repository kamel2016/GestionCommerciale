/* ~/css/supplier-form.css */

:root {
    --primary - color: #2563eb;
    --primary - dark: #1e40af;
    --primary - light: #dbeafe;
    --success - color: #10b981;
    --danger - color: #ef4444;
    --warning - color: #f59e0b;
    --gray - 50: #f9fafb;
    --gray - 100: #f3f4f6;
    --gray - 200: #e5e7eb;
    --gray - 300: #d1d5db;
    --gray - 400: #9ca3af;
    --gray - 500: #6b7280;
    --gray - 600: #4b5563;
    --gray - 700: #374151;
    --gray - 800: #1f2937;
    --gray - 900: #111827;
    --border - radius: 0.5rem;
    --box - shadow - sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
    --box - shadow - md: 0 4px 6px - 1px rgba(0, 0, 0, 0.1);
    --box - shadow - lg: 0 10px 15px - 3px rgba(0, 0, 0, 0.1);
    --transition: all 0.3s ease;
}

/* ============================================
   CONTENEUR PRINCIPAL
   ============================================ */

.suppliers - container {
    max - width: 1000px;
    margin: 0 auto;
    padding: 1.5rem;
}

/* ============================================
   EN-TÊTE
   ============================================ */

.suppliers - header {
    display: flex;
    justify - content: space - between;
    align - items: center;
    margin - bottom: 2rem;
    padding: 1.5rem;
    background: linear - gradient(135deg, var(--primary - color) 0 %, var(--primary - dark) 100 %);
    border - radius: var(--border - radius);
    color: white;
    box - shadow: var(--box - shadow - md);
}

.suppliers - header h1 {
    margin: 0;
    font - size: 1.875rem;
    font - weight: 700;
    display: flex;
    align - items: center;
    gap: 0.75rem;
    color: white;
}

.suppliers - header h1 i {
    font - size: 2rem;
}

.header - info {
    font - size: 0.875rem;
    opacity: 0.9;
    margin - top: 0.5rem;
    color: rgba(255, 255, 255, 0.9);
}

/* ============================================
   MESSAGES D'ALERTE
   ============================================ */

.alert {
    padding: 1.25rem;
    margin - bottom: 2rem;
    border - radius: var(--border - radius);
    display: flex;
    gap: 1rem;
    box - shadow: var(--box - shadow - md);
    border - left: 4px solid;
    animation: slideInDown 0.3s ease;
}

.alert - danger {
    background - color: #fef2f2;
    border - left - color: var(--danger - color);
    color: var(--danger - color);
}

.alert - success {
    background - color: #ecfdf5;
    border - left - color: var(--success - color);
    color: var(--success - color);
}

.alert i {
    font - size: 1.5rem;
    flex - shrink: 0;
    margin - top: 0.125rem;
}

.alert strong {
    display: block;
    margin - bottom: 0.5rem;
    font - weight: 700;
}

.alert ul {
    list - style: none;
    padding: 0;
    margin: 0;
}

.alert ul li {
    padding: 0.25rem 0;
    font - size: 0.875rem;
}

.alert ul li:before {
    content: "• ";
    margin - right: 0.5rem;
}

@keyframes slideInDown {
    from {
        transform: translateY(-20px);
        opacity: 0;
    }

    to {
        transform: translateY(0);
        opacity: 1;
    }
}

/* ============================================
   CONTENEUR DE FORMULAIRE
   ============================================ */

.form - container {
    background: white;
    border - radius: var(--border - radius);
    box - shadow: var(--box - shadow - lg);
    overflow: hidden;
    border: 1px solid var(--gray - 200);
}

.supplier - form {
    padding: 2rem;
}

/* ============================================
   SECTIONS DE FORMULAIRE
   ============================================ */

.form - section {
    margin - bottom: 2.5rem;
    padding - bottom: 2rem;
    border - bottom: 2px solid var(--gray - 200);
}

.form - section: last - of - type {
    border - bottom: none;
    margin - bottom: 2rem;
    padding - bottom: 0;
}

.section - header {
    display: flex;
    align - items: center;
    gap: 1rem;
    margin - bottom: 1.5rem;
    padding - bottom: 1rem;
    border - bottom: 3px solid var(--primary - light);
}

.section - header i {
    font - size: 1.5rem;
    color: var(--primary - color);
    flex - shrink: 0;
}

.section - header h2 {
    margin: 0;
    font - size: 1.25rem;
    font - weight: 700;
    color: var(--gray - 900);
}

/* ============================================
   GROUPES DE FORMULAIRE
   ============================================ */

.form - row {
    display: grid;
    grid - template - columns: repeat(auto - fit, minmax(280px, 1fr));
    gap: 1.5rem;
    margin - bottom: 1.5rem;
}

.form - group {
    display: flex;
    flex - direction: column;
    gap: 0.5rem;
}

.form - group.full - width {
    grid - column: 1 / -1;
}

.form - group label {
    font - weight: 600;
    font - size: 0.875rem;
    color: var(--gray - 800);
    text - transform: capitalize;
    letter - spacing: 0.3px;
}

.form - group label::after {
    content: attr(data - required);
    color: var(--danger - color);
    margin - left: 0.25rem;
}

/* ============================================
   INPUTS & SELECTS
   ============================================ */

.form - control,
    select.form - control {
    padding: 0.875rem;
    border: 2px solid var(--gray - 300);
    border - radius: var(--border - radius);
    font - size: 0.95rem;
    font - family: inherit;
    transition: var(--transition);
    background - color: white;
    color: var(--gray - 900);
}

.form - control::placeholder {
    color: var(--gray - 400);
}

.form - control:focus {
    outline: none;
    border - color: var(--primary - color);
    box - shadow: 0 0 0 4px var(--primary - light);
    background - color: white;
}

.form - control: hover: not(: focus) {
    border - color: var(--gray - 400);
}

.form - control.is - invalid {
    border - color: var(--danger - color);
    background - color: #fef2f2;
}

.form - control.is - invalid:focus {
    box - shadow: 0 0 0 4px rgba(239, 68, 68, 0.1);
}

select.form - control {
    cursor: pointer;
    appearance: none;
    background - image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%236b7280' d='M1 4l5 5 5-5'/%3E%3C/svg%3E");
    background - repeat: no - repeat;
    background - position: right 0.75rem center;
    padding - right: 2rem;
}

/* ============================================
   INPUT GROUP
   ============================================ */

.input - group {
    display: flex;
    border: 2px solid var(--gray - 300);
    border - radius: var(--border - radius);
    overflow: hidden;
    transition: var(--transition);
}

.input - group: focus - within {
    border - color: var(--primary - color);
    box - shadow: 0 0 0 4px var(--primary - light);
}

.input - group.form - control {
    flex: 1;
    border: none;
    border - radius: 0;
}

.input - group.form - control:focus {
    box - shadow: none;
    border: none;
}

.input - group - text {
    background - color: var(--gray - 100);
    border: none;
    padding: 0.875rem 1rem;
    font - weight: 600;
    color: var(--gray - 600);
    font - size: 0.875rem;
}

/* ============================================
   TEXTE D'AIDE & VALIDATION
   ============================================ */

.form - text {
    display: block;
    font - size: 0.8rem;
    color: var(--gray - 600);
    margin - top: 0.25rem;
}

.text - danger {
    display: block;
    color: var(--danger - color);
    font - size: 0.8rem;
    margin - top: 0.5rem;
    font - weight: 600;
    animation: shake 0.3s ease;
}

@keyframes shake {
    0 %, 100 % {
        transform: translateX(0);
    }

    25 % {
        transform: translateX(-5px);
    }

    75 % {
        transform: translateX(5px);
    }
}

/* ============================================
   CHECKBOX PERSONNALISÉ
   ============================================ */

.checkbox - label {
    display: flex;
    align - items: center;
    gap: 0.75rem;
    cursor: pointer;
    user - select: none;
    font - weight: 500;
    color: var(--gray - 800);
}

.checkbox - input {
    position: absolute;
    opacity: 0;
    width: 0;
    height: 0;
}

.checkbox - custom {
    display: flex;
    align - items: center;
    justify - content: center;
    width: 1.25rem;
    height: 1.25rem;
    border: 2px solid var(--gray - 400);
    border - radius: 0.375rem;
    background - color: white;
    transition: var(--transition);
    flex - shrink: 0;
}

.checkbox - input: checked + .checkbox - custom {
    background: linear - gradient(135deg, var(--success - color) 0 %, #059669 100 %);
    border - color: var(--success - color);
    box - shadow: 0 2px 4px rgba(16, 185, 129, 0.2);
}

.checkbox - input: checked + .checkbox - custom::after {
    content: "✓";
    color: white;
    font - size: 0.875rem;
    font - weight: 700;
}

.checkbox - input: focus + .checkbox - custom {
    box - shadow: 0 0 0 4px rgba(16, 185, 129, 0.1);
}

.checkbox - text {
    font - size: 0.95rem;
}

/* ============================================
   BOUTONS D'ACTION
   ============================================ */

.form - actions {
    display: flex;
    gap: 1rem;
    margin - top: 2rem;
    padding - top: 2rem;
    border - top: 2px solid var(--gray - 200);
    flex - wrap: wrap;
}

.btn {
    padding: 0.875rem 1.75rem;
    border - radius: var(--border - radius);
    font - weight: 600;
    font - size: 0.95rem;
    cursor: pointer;
    transition: var(--transition);
    display: inline - flex;
    align - items: center;
    justify - content: center;
    gap: 0.75rem;
    border: none;
    text - decoration: none;
    white - space: nowrap;
}

.btn i {
    font - size: 1rem;
}

.btn - primary {
    background: linear - gradient(135deg, var(--primary - color) 0 %, var(--primary - dark) 100 %);
    color: white;
    box - shadow: var(--box - shadow - md);
}

.btn - primary:hover {
    transform: translateY(-2px);
    box - shadow: var(--box - shadow - lg);
}

.btn - primary:active {
    transform: translateY(0);
}

.btn - secondary {
    background - color: var(--gray - 300);
    color: var(--gray - 800);
    border: 2px solid var(--gray - 400);
}

.btn - secondary:hover {
    background - color: var(--gray - 400);
    border - color: var(--gray - 500);
    transform: translateY(-2px);
}

/* ============================================
   CARTES D'INFORMATION
   ============================================ */

.info - cards {
    display: grid;
    grid - template - columns: repeat(auto - fit, minmax(200px, 1fr));
    gap: 1.5rem;
    margin - top: 2rem;
    padding - top: 2rem;
    border - top: 2px solid var(--gray - 200);
}

.info - card {
    background: linear - gradient(135deg, var(--gray - 50) 0 %, var(--gray - 100) 100 %);
    border: 2px solid var(--gray - 200);
    border - radius: var(--border - radius);
    padding: 1.5rem;
    transition: var(--transition);
}

.info - card:hover {
    box - shadow: var(--box - shadow - lg);
    transform: translateY(-4px);
    border - color: var(--primary - color);
}

.info - header {
    display: flex;
    align - items: center;
    gap: 0.75rem;
    margin - bottom: 1rem;
    padding - bottom: 1rem;
    border - bottom: 2px solid var(--gray - 300);
}

.info - header i {
    font - size: 1.5rem;
    color: var(--primary - color);
}

.info - header h3 {
    margin: 0;
    font - size: 0.95rem;
    font - weight: 700;
    color: var(--gray - 800);
}

.info - body {
    text - align: center;
}

.info - value {
    margin: 0;
    font - size: 2rem;
    font - weight: 700;
    color: var(--primary - color);
    line - height: 1;
}

.info - label {
    margin: 0.75rem 0 0 0;
    font - size: 0.8rem;
    color: var(--gray - 600);
    font - weight: 500;
}

/* ============================================
   RESPONSIVE
   ============================================ */

@media(max - width: 768px) {
    .suppliers - container {
        padding: 1rem;
    }

    .suppliers - header {
        padding: 1.25rem;
    }

    .suppliers - header h1 {
        font - size: 1.5rem;
    }

    .suppliers - header h1 i {
        font - size: 1.5rem;
    }

    .supplier - form {
        padding: 1.5rem;
    }

    .form - row {
        grid - template - columns: 1fr;
        gap: 1rem;
    }

    .form - section {
        margin - bottom: 2rem;
        padding - bottom: 1.5rem;
    }

    .section - header {
        margin - bottom: 1rem;
        padding - bottom: 0.75rem;
    }

    .section - header h2 {
        font - size: 1.1rem;
    }

    .info - cards {
        grid - template - columns: 1fr;
        gap: 1rem;
    }

    .form - actions {
        flex - direction: column;
    }

    .btn {
        width: 100 %;
    }
}

@media(max - width: 480px) {
    .suppliers - container {
        padding: 0.75rem;
    }

    .suppliers - header {
        padding: 1rem;
    }

    .suppliers - header h1 {
        font - size: 1.25rem;
    }

    .suppliers - header h1 i {
        font - size: 1.25rem;
    }

    .supplier - form {
        padding: 1rem;
    }

    .form - row {
        gap: 0.75rem;
    }

    .form - group label {
        font - size: 0.8rem;
    }

    .form - control,
        select.form - control {
        font - size: 1rem; /* Évite le zoom sur iPhone */
        padding: 0.75rem;
    }

    .form - section {
        margin - bottom: 1.5rem;
        padding - bottom: 1rem;
    }

    .section - header {
        gap: 0.5rem;
    }

    .section - header i {
        font - size: 1.25rem;
    }

    .section - header h2 {
        font - size: 1rem;
    }

    .form - actions {
        gap: 0.75rem;
    }

    .btn {
        padding: 0.75rem 1rem;
        font - size: 0.85rem;
    }

    .info - cards {
        gap: 0.75rem;
    }

    .info - value {
        font - size: 1.5rem;
    }
}