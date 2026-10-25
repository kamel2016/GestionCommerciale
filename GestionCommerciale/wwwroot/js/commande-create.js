// wwwroot/js/commande-create.js

(function () {
    const fmt = (n) => n.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' €';

    let commandeArticles = []; // { articleId, nom, prix, tva, quantite, stock }

    const modalEl = document.getElementById('articleModal');
    const modal = new bootstrap.Modal(modalEl);
    const select = document.getElementById('article-select');
    const qteInput = document.getElementById('quantite');
    const prixInput = document.getElementById('prix-unitaire');
    const stockInput = document.getElementById('stock-disponible');
    const previewEl = document.getElementById('montant-ligne-preview');
    const tbody = document.getElementById('articles-tbody');
    const noArticles = document.getElementById('no-articles');
    const form = document.getElementById('commande-form');
    const lignesJsonInput = document.getElementById('lignes-json');

    // Ouverture du modal (appelée par le bouton)
    window.openArticleModal = function () {
        select.value = '';
        qteInput.value = 1;
        qteInput.classList.remove('is-invalid');
        select.classList.remove('is-invalid');
        prixInput.value = '';
        stockInput.value = '';
        previewEl.textContent = fmt(0);
        modal.show();
    };

    // Mise à jour prix/stock/aperçu quand on change d'article
    select.addEventListener('change', updatePreview);
    qteInput.addEventListener('input', updatePreview);

    function updatePreview() {
        const opt = select.selectedOptions[0];
        if (!opt || !opt.value) {
            prixInput.value = '';
            stockInput.value = '';
            previewEl.textContent = fmt(0);
            return;
        }
        const prix = parseFloat(opt.dataset.prix) || 0;
        const stock = parseInt(opt.dataset.stock) || 0;
        const qte = parseInt(qteInput.value) || 0;

        prixInput.value = fmt(prix);
        stockInput.value = stock;
        qteInput.max = stock;
        previewEl.textContent = fmt(prix * qte);
    }

    // Ajouter l'article depuis le modal
    document.getElementById('btn-add-article').addEventListener('click', () => {
        const opt = select.selectedOptions[0];
        let valid = true;

        if (!opt || !opt.value) {
            select.classList.add('is-invalid');
            valid = false;
        } else {
            select.classList.remove('is-invalid');
        }

        const qte = parseInt(qteInput.value) || 0;
        const stock = parseInt(opt?.dataset.stock) || 0;

        if (qte < 1 || (opt && opt.value && qte > stock)) {
            qteInput.classList.add('is-invalid');
            if (opt && opt.value && qte > stock) {
                qteInput.nextElementSibling.textContent = `Stock insuffisant (disponible : ${stock}).`;
            }
            valid = false;
        } else {
            qteInput.classList.remove('is-invalid');
        }

        if (!valid) return;

        const articleId = parseInt(opt.value);
        const existing = commandeArticles.find(a => a.articleId === articleId);

        if (existing) {
            const nouvelleQte = existing.quantite + qte;
            if (nouvelleQte > existing.stock) {
                alert(`Stock insuffisant pour « ${existing.nom} » (disponible : ${existing.stock}).`);
                return;
            }
            existing.quantite = nouvelleQte;
        } else {
            commandeArticles.push({
                articleId,
                nom: opt.dataset.nom,
                prix: parseFloat(opt.dataset.prix),
                tva: parseFloat(opt.dataset.tva),
                quantite: qte,
                stock: stock
            });
        }

        modal.hide();
        renderTable();
    });

    function renderTable() {
        tbody.innerHTML = '';

        commandeArticles.forEach((a, index) => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${escapeHtml(a.nom)}</td>
                <td class="text-end">${fmt(a.prix)}</td>
                <td class="text-center">${a.quantite}</td>
                <td class="text-end">${fmt(a.prix * a.quantite)}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-sm btn-outline-danger" data-index="${index}">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>`;
            tr.querySelector('button').addEventListener('click', () => removeArticle(index));
            tbody.appendChild(tr);
        });

        noArticles.style.display = commandeArticles.length ? 'none' : 'block';
        document.getElementById('articles-container').style.display = commandeArticles.length ? 'block' : 'none';

        updateTotaux();
        lignesJsonInput.value = JSON.stringify(commandeArticles.map(a => ({
            articleId: a.articleId,
            quantite: a.quantite,
            prixUnitaire: a.prix
        })));
    }

    function removeArticle(index) {
        commandeArticles.splice(index, 1);
        renderTable();
    }

    function updateTotaux() {
        let ht = 0, tva = 0;
        commandeArticles.forEach(a => {
            const montant = a.prix * a.quantite;
            ht += montant;
            tva += montant * (a.tva / 100);
        });
        document.getElementById('montant-ht').textContent = fmt(ht);
        document.getElementById('montant-tva').textContent = fmt(tva);
        document.getElementById('montant-ttc').textContent = fmt(ht + tva);
    }

    // Empêche la soumission sans article
    form.addEventListener('submit', (e) => {
        if (commandeArticles.length === 0) {
            e.preventDefault();
            alert('Ajoutez au moins un article à la commande.');
        }
    });

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    renderTable();
})();