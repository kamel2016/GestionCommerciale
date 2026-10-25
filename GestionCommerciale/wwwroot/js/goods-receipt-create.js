(() => {
    'use strict';

    console.log('[GR] script démarré');

    const fmt = new Intl.NumberFormat('fr-TN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    const lignes = [];
    let articles = [];

    const body = document.getElementById('lignesBody');
    const hidden = document.getElementById('lignesJson');
    const form = document.getElementById('grForm');
    const modalEl = document.getElementById('modalLigne');
    const modal = new bootstrap.Modal(modalEl);
    const selFournisseur = document.getElementById('selFournisseur');
    const selCommande = document.getElementById('selCommande');
    const selArticle = document.getElementById('selArticle');
    const inpQte = document.getElementById('inpQte');
    const inpPU = document.getElementById('inpPU');
    const erreur = document.getElementById('modalErreur');
    const btnAjouter = document.getElementById('btnAjouterLigne');
    const btnValider = document.getElementById('btnValiderLigne');
    const totalHTEl = document.getElementById('totalHT');
    const totalTVAEl = document.getElementById('totalTVA');
    const totalTTCEl = document.getElementById('totalTTC');

    function pick(obj, ...noms) {
        for (const n of noms) {
            if (obj[n] !== undefined && obj[n] !== null) return obj[n];
        }
        return undefined;
    }

    function escapeHtml(s) {
        return String(s ?? '').replace(/[&<>"']/g, function (c) {
            switch (c) {
                case '&': return '&';
                case '<': return '<';
                case '>': return '>';
                case '"': return '"';
                case "'": return "'";
            }
        });
    }

    function afficherErreurChargement(zone, msg) {
        zone.innerHTML = `<option value="">— ${escapeHtml(msg)} —</option>`;
    }

    // ---------- Chargement ----------

    async function chargerCommandes(fournisseurId) {
        selCommande.innerHTML = '<option value="">— Chargement… —</option>';

        if (!fournisseurId) {
            selCommande.innerHTML = '<option value="">— Sélectionner un fournisseur d\'abord —</option>';
            return;
        }

        try {
            const url = `/GoodsReceipt/Create?handler=Commandes&fournisseurId=${encodeURIComponent(fournisseurId)}`;
            console.log('[GR] GET', url);
            const response = await fetch(url);
            console.log('[GR] statut commandes', response.status);

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const commandes = await response.json();
            console.log('[GR] commandes reçues', commandes);

            selCommande.innerHTML = '<option value="">— Sélectionner une commande —</option>';

            if (!commandes || commandes.length === 0) {
                afficherErreurChargement(selCommande, 'Aucune commande ouverte');
                return;
            }

            commandes.forEach(c => {
                const id = pick(c, 'id', 'Id');
                const libelle = pick(c, 'libelle', 'Libelle', 'label', 'Label') ?? `Commande ${id}`;
                selCommande.add(new Option(libelle, id));
            });
        } catch (err) {
            console.error('[GR] erreur chargement commandes', err);
            afficherErreurChargement(selCommande, 'Erreur de chargement : ' + err.message);
        }
    }

    async function chargerArticles(fournisseurId) {
        try {
            console.log('chargerArticles appelée avec :', fournisseurId);
            const res = await fetch(`/GoodsReceipt/Create?handler=Articles&fournisseurId=${fournisseurId}`);

            //const res = await fetch('/GoodsReceipt/Create?handler=Articles');
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const articles = await res.json();
            const corps = document.getElementById('lignesBody');

            if (articles.length === 0) {
                corps.innerHTML = `<tr class="gr-empty-row"><td colspan="7">
        <i class="fas fa-box-open"></i> Aucun article pour ce fournisseur</td></tr>`;
                return;
            }

            corps.innerHTML = articles.map(a => `
                <tr data-article-id="${a.id}">
                    <td>${escapeHtml(a.reference ?? '')}</td>
                    <td>${escapeHtml(a.libelle ?? '')}</td>
                    <td class="text-end">${a.qteCommandee ?? 0}</td>
                    <td class="text-end"><input type="number" class="gr-input" min="0"></td>
                    <td class="text-end">${a.prixUnitaireHt ?? 0}</td>
                    <td class="text-end">${a.tva ?? 0}</td>
                    <td class="text-end">0</td>
                </tr>
        `).join('');

            document.getElementById('btnAjouterLigne').disabled = false;
        } catch (err) {
            console.error('[GR] erreur chargement articles', err);
        }
    }

    body.addEventListener('change', e => {
        const input = e.target;
        console.log('Change Body');

        if (!input.matches('input[type="number"]')) {
            console.log('On ne trouve pas le type number');

            return;
        }
        console.log('On trouve le type number');

        const tr = input.closest('tr');
        console.log('On trouve le tr');

        const articleId = +tr.dataset.articleId;
        console.log('On trouve le articleId:' + articleId);
        console.log('dataset :', tr.dataset.articleId, typeof tr.dataset.articleId);

        console.log('lignes :', JSON.stringify(lignes));
        const ligne = lignes.find(l => String(l.articleId) === tr.dataset.articleId);

        if (!ligne) {
            console.log('La ligne non présente');

            return;
        }
        ligne.qteRecue = Math.max(0, +input.value || 0);
        console.log('On est avant le render');

        render();
    });
    // ---------- Modal ----------

    function ouvrirModal() {
        selArticle.value = '';
        inpQte.value = 1;
        inpPU.value = '';
        erreur.classList.add('d-none');
        modal.show();
    }

    function onArticleChange() {
        const a = articles.find(x => String(pick(x, 'id', 'Id')) === selArticle.value);
        inpPU.value = a ? (pick(a, 'prixUnitaireHt', 'PrixUnitaireHt') ?? '') : '';
    }

    function afficherErreur(msg) {
        erreur.textContent = msg;
        erreur.classList.remove('d-none');
    }

    function ajouterLigne() {
        console.log('ajouterLigne appelée');
        console.log('selArticle.value =', selArticle.value);
        console.log('article trouvé =', articles.find(a => String(pick(a, 'id', 'Id')) === selArticle.value));

        const article = articles.find(a => String(pick(a, 'id', 'Id')) === selArticle.value);
        const qte = parseInt(inpQte.value, 10);
        const pu = parseFloat(inpPU.value);

        if (!article) return afficherErreur('Sélectionnez un article.');
        if (!qte || qte <= 0) return afficherErreur('La quantité doit être supérieure à zéro.');
        if (isNaN(pu) || pu < 0) return afficherErreur('Prix unitaire invalide.');

        const articleId = pick(article, 'id', 'Id');
        if (lignes.some(l => l.articleId == articleId)) return afficherErreur('Cet article est déjà dans le bon.');

        lignes.push({
            articleId,
            reference: pick(article, 'reference', 'Reference') ?? '',
            libelle: pick(article, 'libelle', 'Libelle') ?? '',
            quantite: qte,
            prixUnitaire: pu,
            tauxTVA: pick(article, 'tva', 'Tva', 'tauxTVA', 'TauxTVA') ?? 0
        });

        modal.hide();
        render();
    }

    function supprimerLigne(idx) {
        lignes.splice(idx, 1);
        render();
    }

    // ---------- Rendu ----------

    //async function chargerArticles(fournisseurId) {
    //    try {
    //        console.log('chargerArticles appelée avec :', fournisseurId);
    //        const res = await fetch(`/GoodsReceipt/Create?handler=Articles&fournisseurId=${fournisseurId}`);

    //        //const res = await fetch('/GoodsReceipt/Create?handler=Articles');
    //        if (!res.ok) throw new Error(`HTTP ${res.status}`);
    //        const articles = await res.json();
    //        const corps = document.getElementById('lignesBody');

    //        if (articles.length === 0) {
    //            corps.innerHTML = `<tr class="gr-empty-row"><td colspan="7">
    //    <i class="fas fa-box-open"></i> Aucun article pour ce fournisseur</td></tr>`;
    //            return;
    //        }

    //        corps.innerHTML = articles.map(a => `
    //            <tr data-article-id="${a.id}">
    //                <td>${escapeHtml(a.reference ?? '')}</td>
    //                <td>${escapeHtml(a.libelle ?? '')}</td>
    //                <td class="text-end">${a.qteCommandee ?? 0}</td>
    //                <td class="text-end"><input type="number" class="gr-input" min="0" value="0"></td>
    //                <td class="text-end">${a.prixUnitaireHt ?? 0}</td>
    //                <td class="text-end">${a.tva ?? 0}</td>
    //                <td class="text-end">0</td>
    //            </tr>
    //    `).join('');

    //        document.getElementById('btnAjouterLigne').disabled = false;

    //    } catch (err) {
    //        console.error('[GR] erreur chargement articles', err);
    //    }
    //}

    async function ouvrirModal() {
        const fournisseurId = selFournisseur.value;
        if (!fournisseurId) {
            alert('Sélectionnez d\'abord un fournisseur.');
            return;
        }

        erreur.classList.add('d-none');
        selArticle.innerHTML = '<option value="">— Chargement… —</option>';
        selArticle.disabled = true;
        inpQte.value = 1;
        inpPU.value = '';
        modal.show();

        try {
            const url = `/GoodsReceipt/Create?handler=Articles&fournisseurId=${encodeURIComponent(fournisseurId)}`;
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            articles = await res.json();
            console.log('[GR] articles reçus', articles);

            selArticle.innerHTML = '<option value="">— Sélectionner —</option>';
            articles.forEach(a => {
                const id = pick(a, 'id', 'Id');
                const ref = pick(a, 'reference', 'Reference') ?? '';
                const lib = pick(a, 'libelle', 'Libelle') ?? '';
                selArticle.add(new Option(`${ref} - ${lib}`, id));
            });
        } catch (err) {
            console.error('[GR] erreur chargement articles', err);
            selArticle.innerHTML = '<option value="">— Erreur de chargement —</option>';
            afficherErreur('Impossible de charger les articles.');
        } finally {
            selArticle.disabled = false;
        }
    }

    function render() {
        if (lignes.length === 0) {
            body.innerHTML = `<tr class="gr-empty-row"><td colspan="7"><i class="fas fa-box-open"></i> Aucun article ajouté</td></tr>`;
        } else {
            body.innerHTML = lignes.map((l, i) => `
                <tr data-article-id="${l.id}">
                    <td><span class="gr-code">${escapeHtml(l.reference)}</span></td>
                    <td>${escapeHtml(l.libelle)}</td>
                    <td class="text-end">
                        <input type="number" min="1" value="${l.qteCommandee}" data-idx="${i}"
                               class="gr-input gr-input--sm text-end" style="width:80px" />
                    </td>
                    <td class="text-end">${fmt.format(l.prixUnitaireHt)}</td>
                    <td class="text-end">${l.tva} %</td>
                    <td class="text-end">${fmt.format(l.qteCommandee * l.prixUnitaireHt)}</td>
                    <td class="text-end">
                        <button type="button" class="gr-icon-btn gr-icon-btn--danger" data-del="${i}" title="Retirer">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>
                </tr>`).join('');
        }

        let totalHTGlobal = 0, totalTVA = 0;
        lignes.forEach(l => {
            const ht = l.qteCommandee * l.prixUnitaireHt;
            totalHTGlobal += ht;
            totalTVA += ht * l.tva / 100;
        });

        totalHTEl.textContent = fmt.format(totalHTGlobal) + ' €';
        totalTVAEl.textContent = fmt.format(totalTVA) + ' €';
        totalTTCEl.textContent = fmt.format(totalHTGlobal + totalTVA) + ' €';

        hidden.value = JSON.stringify(serialiserLignes());
    }

    function serialiserLignes() {
        return lignes.map(l => ({
            ArticleId: l.articleId,
            Quantite: l.quantite,
            PrixUnitaire: l.prixUnitaire,
            TauxTVA: l.tauxTVA
        }));
    }

    // ---------- Événements ----------

    body.addEventListener('input', e => {
        const input = e.target;
        if (input.dataset.idx === undefined) return;
        lignes[Number(input.dataset.idx)].quantite = Number(input.value) || 0;
        render();
    });

    body.addEventListener('click', e => {
        const btn = e.target.closest('[data-del]');
        if (btn) supprimerLigne(Number(btn.dataset.del));
    });

    selFournisseur.addEventListener('change', () => {
        const fournisseurId = selFournisseur.value;
        chargerCommandes(fournisseurId);
        chargerArticles(fournisseurId);
        lignes.length = 0;
        render();
    });

    selArticle.addEventListener('change', onArticleChange);
    btnAjouter.addEventListener('click', ouvrirModal);
    btnValider.addEventListener('click', ajouterLigne);
    document.getElementById('btnEnregistrer').addEventListener('click', ajouterLigne);

    form.addEventListener('submit', e => {
        if (lignes.length === 0) {
            e.preventDefault();
            alert('Ajoutez au moins un article au bon de réception.');
            return;
        }
        hidden.value = JSON.stringify(serialiserLignes());
    });

    render();
})();