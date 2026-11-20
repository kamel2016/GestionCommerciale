# ROADMAP – GestionCommerciale

Rapport d'audit en lecture seule du 2026-11-15 (branche `claude-audit`), suivi du découpage en sessions de travail.
Effort : **S** = quelques heures, **M** = environ une session, **G** = plusieurs sessions.
Règle CLAUDE.md : une session = un module, une branche dédiée, un commit par étape, `dotnet build` puis `dotnet test` avant de rendre la main, jamais de `dotnet ef database update` sans montrer le script et obtenir l'accord.

## Décisions de l'utilisateur (2026-11-15)
1. **Stock client** : le stock baisse uniquement à la validation du BL. La commande client ne touche plus au stock.
2. **Pages/Supplier/** : à supprimer au profit de `Pages/Fournisseurs/`. La suppression se fait en S5, avec confirmation explicite à ce moment-là.
3. **Devis** : le module est commité tel quel et conservé.

Point encore ouvert : l'avis du comptable (FODEC, timbre fiscal, retenue à la source, numérotation, exercice) est nécessaire avant S10.

## Rapport d'audit

### État global
- Le build échoue (1 erreur) : `Pages/Clients/Create.cshtml:46`, `@using GestionCommerciale.Enums` manquant.
- `Client.Code` (et son index unique) et `Client.ModeFacturation` ne figurent dans aucune migration ni dans le snapshot.
- `InitialCreate` a été modifiée à la main. La migration `AjoutDevis` n'est pas commitée. 22 fichiers sont modifiés ou non suivis.
- Aucune authentification, aucun rôle, aucun journal d'actions.

### Par module
| Module | Réel (enregistre en base) | Factice ou incomplet | Bugs et règles manquantes |
|---|---|---|---|
| Clients | Create, Index (activer, supprimer) | Edit (5 clients en dur, faux succès), ClientDetails, Delete, Details | Pas d'unicité email/matricule, pas de format matricule tunisien, code postal à 5 chiffres, plafond de crédit et délai jamais contrôlés, code client non protégé contre la concurrence |
| Articles | Create, Edit, suppression via Index | Details (historique de stock inventé), Delete | Signe inversé des entrées de stock (`Articles/Edit.cshtml.cs:223`), CodeBarre non contrôlé, filtres faux, affichage en € |
| Fournisseurs | Create, Edit, Index | Details, Delete (copies de pages Client), dossier doublon `Supplier/` | Include de collections entières dans Index, `supplier-form.js` inexistant, matricule non validé |
| Commandes client | Create, Edit, suppression via Index | Page Delete | Double sortie de stock avec le BL, `TauxTVA` et `MontantLigne` jamais renseignés, remise globale jamais calculée, Edit calcule avec `PrixAchatHT` (`Edit.cshtml.cs:213`), Edit perd `QuantiteLivree`, plafond et délai ignorés, numéro sans verrou |
| Commandes fournisseur | Create, Edit, Delete | – | TVA codée à 20 %, montants forgeables, aucun statut « livrée », pas de reliquat |
| Bons de réception | Create (brouillon) | Details (données en dur) | Aucune validation donc aucune entrée de stock, prix pris sur l'article, injection de `RetenueSourceService` concret (`GoodsReceipt/Create.cshtml.cs:21`) qui peut faire planter la page |
| Bons de livraison | Create, Valider, Annuler (transactions, sorties de stock) | Pas d'édition ni de suppression de brouillon | Statut de la commande jamais mis à jour, prix forgeable, annulation avec le mauvais type de mouvement, pas de concurrence |
| Factures | Création depuis un BL, validation, annulation, règlements, PDF | Pas de facture directe, pas de facture mensuelle, pas d'avoirs, pas d'échéancier | Retenue à la source déduite plusieurs fois, numérotation fragile, pas de « 1 BL = 1 facture », FODEC et timbre systématiques, facture modifiable après validation |
| Stock | Mouvements via BL et Edit article | `StockService` jamais enregistré ni utilisé | Signes incohérents, aucune entrée à la réception, pas de protection de concurrence |

### Points transverses
- **Migrations** : migration manquante, précisions decimal (18,2) contre 3 décimales utilisées, double configuration de `TauxTVA`, index manquants (dont `Facture.BonLivraisonId`), cascade Règlement vers Facture dangereuse, pas de `Migrate()` au démarrage.
- **Validation** : peu de bornes, aucune règle tunisienne, entités complètes reçues du navigateur, quantités et prix non contrôlés côté serveur.
- **Erreurs** : pas de gestion globale, messages d'exception affichés tels quels, concurrence jamais gérée, presque aucun log.
- **Performance** : listes sans pagination (BL, commandes fournisseur), `Include` inutiles, `AsNoTracking` rare.
- **Sécurité** : aucune authentification ni rôle, paramètres fiscaux et PDF accessibles à tous, seed factice à chaque démarrage, Dockerfile inexploitable.
- **Nettoyage** : doublons (`Supplier/`, `Categorie`/`Category`), ViewModels et scripts orphelins, code commenté, encodage mixte, symbole € au lieu de DT (fait dans les vues et scripts : commit `cfed4f0`).
- **PDF** : en-tête en dur, paramètres société non lus, mentions légales incomplètes, PDF non figé.

### Tableau priorisé
**Bloquant**
| Module | Problème | Fichier | Effort |
|---|---|---|---|
| Build | Erreur de compilation | `Pages/Clients/Create.cshtml:46` | S |
| Migrations | `Client.Code`, `ModeFacturation`, index unique absents ; `InitialCreate` modifiée | `Migrations/`, `Models/Client.cs` | M |
| Clients | Edit, Delete, Details factices | `Pages/Clients/*` | M |
| Stock et commandes | Double sortie de stock (corrigée en S7, commit `88e62fb`) | `Commande_Client/Create:145`, `BonLivraisonService:55` | M |
| Stock | Signe inversé des entrées | `Articles/Edit.cshtml.cs:223` | S |
| Réception | Aucune validation de BR, pas d'entrée de stock, Details factice | `Pages/GoodsReceipt/*` | G |
| Réception | Service de retenue non injectable | `GoodsReceipt/Create:21` | S |
| Commandes client | Edit avec `PrixAchatHT`, TVA historique non copiée | `Commande_Client/Edit:213`, `Create:137` | M |
| Factures | Retenue à la source déduite plusieurs fois | `ReglementService:35,87-91`, `Facture.cs:46` | M |
| Factures | Numérotation fragile, pas de « 1 BL = 1 facture » | `FactureService`, `SequenceService` | M |
| Sécurité | Aucune authentification ni rôle | `Program.cs` | G |
| Fiscalité | FODEC, timbre, retenue, numérotation non validés | `FactureService` | M plus avis du comptable |

**Important**
| Module | Problème | Fichier | Effort |
|---|---|---|---|
| Fournisseurs | Doublon `Supplier/`, Details et Delete factices | `Pages/Supplier`, `Pages/Fournisseurs` | M |
| Articles | Details et Delete factices, filtres faux | `Pages/Articles/*` | M |
| Commandes | Statut jamais mis à jour, transitions libres | `BonLivraisonService:71`, `Commande_Client/Edit` | M |
| Commandes client | Plafond de crédit, délai de paiement, remise globale | `Commande_Client`, `Client.cs` | M |
| Commandes fournisseur | TVA en dur, montants forgeables, pas de reliquat | `Commande_Fournisseur/Create:64` | M |
| Factures | Facture directe, mensuelle, brouillon modifiable | `FactureService`, `Pages/Factures` | G |
| Factures | Immuabilité après validation, règlement sur brouillon | `FactureService`, `ReglementService` | S |
| Migrations | Précisions decimal, index, cascades | `ApplicationDbContext` | M |
| PDF | Paramètres société, mentions légales, TVA par taux | `FacturePdfService` | M |
| Stock | Convention de signe unique, `StockService` partout | `StockService`, BL, Articles | M |
| Erreurs et logs | Gestion globale, concurrence, transactions | `Services/`, `Pages/` | M |
| Validation | Règles tunisiennes, anti sur-binding, quantités | `Models/`, `Pages/` | M |

**Souhaitable**
| Module | Problème | Fichier | Effort |
|---|---|---|---|
| Règlements | Échéancier, relances, multi-factures, trop-perçu | `ReglementService` | G |
| Avoirs | Inexistants | – | G |
| Audit | Journal des actions sensibles | – | M |
| Import Excel | Articles, clients, fournisseurs | – | G |
| Reporting | Balance âgée, ventes, stock, TVA, retenues | – | G |
| Performance | Pagination BL et commandes fournisseur, projections | Pages Index | S à M |
| Nettoyage | Doublons, code mort, encodage (€ vers DT fait : `cfed4f0`) | divers | M |
| Exploitation | Dockerfile, secrets, configuration par environnement | `Dockerfile` | M |

---

## Sessions de travail

### S1 – Remise en état (réalisée ; découpage de l'historique abandonné)
- **Objectif** : un build vert et un dépôt propre.

**Réalisé** (preuve entre parenthèses, vérifiée le 2026-11-17)
- **Erreur de build corrigée** : `@using GestionCommerciale.Enums` est en ligne 3 de `Pages/Clients/Create.cshtml` (commit `5ed700a` « Build corrigé »).
- **Projet de tests créé avec un premier test** : `GestionCommerciale.Tests` et `ClientCodeTests.cs` (commit `3819ba1`). La solution compte au 2026-11-19 **375 tests réussis et 18 ignorés** (tests SQL Server, lancés seulement avec `GC_TESTS_SQLSERVER=1`), 0 échec (`dotnet test`) ; 89 au 2026-11-17.
- **SQL fautif retiré de `InitialCreate`** (5 lignes, avec votre accord) : commit `639c304`, fichier `Migrations/20261010173403_InitialCreate.cs`.
- **État de travail commité** : oui, mais en **un seul commit** (`5ed700a`, 22 fichiers, +3013 / −52).
- **`dotnet build` et `dotnet test` passent** : 0 erreur de compilation, 89 tests réussis (exécutés le 2026-11-17).
- **L'application démarre** : démarrage local vérifié le 2026-11-17, pages clients répondant 200. Aucune trace dans le dépôt.

**Reste à faire** : rien.

**Abandonné**
- **Découper l'état commité en étapes lisibles** (Devis, ModeFacturation, numérotation) : **abandonné (décision : historique conservé)**, décision du 2026-11-17. Tout reste dans le commit `5ed700a`, qui mélange les lots ; l'historique n'est pas réécrit.

- **Effort** : S à M.

### S2 – Migration Client et précisions decimal (partielle)
- **Objectif** : aligner la base sur le modèle.

**Réalisé** (preuve entre parenthèses, vérifiée le 2026-11-17)
- **Migration du code client et du mode de facturation** : `Migrations/20261115223951_AjoutCodeClientEtModeFacturation.cs` (+ `.Designer.cs` et snapshot), commit `639c304`. Elle ajoute `Clients.Code` (nvarchar(20)) et l'index unique `IX_Clients_Code`, remplit les codes **avant** l'index, et ajoute `Clients.ModeFacturation`.
- **Contenu supplémentaire de la même migration** : `LignesCommandeClient.TauxTVA`, la remise globale de `CommandesClient` (`TauxRemiseGlobale`, `MontantHTBrut`, `MontantRemiseGlobale`), `NumeroCommande` en nvarchar(20) avec index unique, `ClientId` non nul, `Commentaire` nullable.
- **Relation `CommandeClient`–`Client` corrigée** (`.WithMany(c => c.CommandesClient)` dans `Data/ApplicationDbContext.cs`), ce qui évite la colonne parasite `ClientId1` : commit `639c304`.
- **Script SQL montré avant application** : oui, le 2026-11-15, en session. Il n'est pas conservé dans le dépôt.
- **Garde-fou** : sauvegarde de la base de dev déclarée faite par vous avant application ; aucun `dotnet ef database update` sans votre accord explicite.
- **Appliquée à la base de dev** : le 2026-11-15 (`dotnet ef database update` terminé sans erreur, inscription dans `__EFMigrationsHistory`). L'application lit ensuite `Clients.Code` en base : la liste affiche `CLI-00001`.
- **Modèle et migrations alignés** : `dotnet ef migrations has-pending-model-changes` répond « No changes have been made to the model since the last migration » (2026-11-17).

**Reste à faire**
- **Précisions decimal : non faites, migration à part, à planifier** (aucune session n'est encore fixée). Dans le snapshot, ces colonnes sont toujours en `decimal(18,2)`, c'est-à-dire sans précision explicite :
  - `Facture` : `TotalHt`, `TotalTva`, `TotalTtc`, `TimbreFiscal`, `MontantFodec`, `NetAPayer`, `MontantRetenueSource` ;
  - `LigneFacture` : `PrixUnitaireHt`, `RemisePourcent`, `TauxTva` ;
  - `ParametresSociete` : `TauxFodec`, `TauxRetenueSource` ;
  - `BonReception` : `TauxRetenueSource` et `MontantRetenueSource`.
  Cette liste est à confirmer, propriété par propriété, avant d'écrire la migration. Les arrondis du code sont à 3 décimales alors que ces colonnes n'en gardent que 2.
- **État réel au 2026-11-19 [vérifié par requête en lecture seule sur la base de dev]** : les 9 colonnes d'en-tête (`MontantHT`, `MontantTVA`, `MontantTTC` de `CommandesClient`, `CommandesFournisseur`, `BonsReception`) sont en `decimal(18,3)`. Elles n'ont pas été modifiées à la main : la base de dev a été recréée depuis la chaîne de migrations (décision du 2026-11-19, voir `docs/DECISION-PRECISIONS.md`, section 7, document non suivi), qui les définit déjà en `(18,3)` ; **aucune migration n'est donc à écrire pour ces 9 colonnes**. Les autres colonnes de la liste ci-dessus restent en `decimal(18,2)` [vérifié : par exemple `Factures.TotalHt`, `LignesFacture.PrixUnitaireHt`, `ParametresSociete.TauxFodec`, `BonsReception.MontantRetenueSource`] : la migration correspondante reste à écrire, après relecture comptable de la précision cible.
- **Double configuration de `TauxTVA` : déplacée vers S4** (décision du 2026-11-17), car elle concerne les Articles. Faite en S4 (commit `01faca4`, aucune migration nécessaire).
- **Test à la main : « créer un client et vérifier son code `CLI-00001` »** : non prouvé dans l'historique. La génération du code est couverte par le test `ClientServiceTests.Les_codes_sont_generes_dans_l_ordre` (sous SQLite). Le client existant de la base de dev porte `CLI-00001` parce que la migration l'a rempli. La création d'un nouveau client sur la base de dev est à confirmer par vous.

- **Effort** : M.

### S3 – Clients (terminée le 2026-11-17, branche `s3-clients`, non fusionnée)
- **Objectif** : un CRUD client qui enregistre vraiment.
- **Résultat** : `dotnet build` 0 erreur (45 avertissements, aucun nouveau) ; `dotnet test` **89 tests réussis**, sous SQLite en mémoire. Branche à 11 commits de code après `claude-audit` (la mise à jour de cette feuille de route vient en plus), rien de poussé.
- **Décisions prises pendant la session** : matricule fiscal validé de façon souple (chiffres, lettres, `/`, 20 caractères maximum, sans contrôle de la clé) ; code postal à 4 chiffres ; unicité de l'email et du matricule contrôlée par l'application seulement, sans index en base ; nettoyage des champs par un `Trim()` complet avant validation ; mode de facturation verrouillé dès que le client a un document (commande, devis, bon de livraison ou facture) ; Edit redirige vers la liste avec le message vert ; suppression physique seulement sans aucun document.

**Réalisé, étape par étape**

| Étape | Contenu | Commit |
|---|---|---|
| 1 | Projet de tests `GestionCommerciale.Tests` (xUnit + SQLite en mémoire) | `3819ba1` |
| 2 | `ClientValidation` : matricule, code postal, remise, plafond, délai | `b284959` |
| 2 bis | Regex stricts (`[0-9]`, `\z`), tests chiffres arabes et retour à la ligne | `80938ec` |
| 3 | Création via `ClientService` : validation, unicité, code `CLI-00001` en un seul endroit, 3 essais maximum, seulement sur l'index `IX_Clients_Code` | `d2ba1c4` |
| 4 | Modification réelle (`EditModel`, `ModifierAsync`), code et date de création intouchables, verrou du mode de facturation | `9f1adc6` |
| 5 | Fiche client réelle (`ClientDetails`, `ObtenirFicheAsync`, 5 dernières commandes) | `076eb7b` |
| 5 bis | Edit redirige vers la liste avec « Client CLI-… modifié avec succès. » + tests de la page | `b6bf026` |
| 6 | Suppression sûre : page de confirmation, contrôle refait dans le service, filet de la clé étrangère | `0d4b76e` |
| 7 | Liste : liens Détails et Modifier, code affiché | `3ffd73a` |
| 8 | Suppression des pages et ViewModels factices (`Details.*`, 6 ViewModels, exclusions du `.csproj`) | `591dac5` |
| 8 bis | Suppression de `PaginationViewModel` (inutilisé) | `16c97f4` |
| 9 | Compte rendu final (aucun fichier modifié) | – |

La migration `AjoutCodeClientEtModeFacturation` (`639c304`, commitée avant S3) est appliquée à la base de dev.

**Ce qui reste du module Clients**
1. **Formulaire de création incomplet** : il n'a ni mobile, ni ville, ni code postal, ni retenue à la source, ni plafond de crédit, ni délai de paiement, ni remise. Ces champs ne se saisissent que dans l'Edit.
2. **Plafond de crédit et délai de paiement non appliqués** : enregistrés, validés et affichés, mais aucune règle ne les applique aux commandes. Prévu en **S7**.
3. **Encours et solde client** : absents, faute de règle de calcul validée.
4. **Liste non migrée vers le service** : l'activation et la désactivation (`ToggleStatus`) écrivent toujours directement dans la base. `Index.cshtml.cs` est resté en Windows-1252 et son champ `_logger` n'est plus utilisé.
5. **Symbole € dans les autres modules** (Articles, commandes, réceptions, fournisseurs) : remplacé par DT dans les vues et scripts (commit `cfed4f0`, 2026-11-19). Reste `Pages/Fournisseurs/Details.cshtml:35` (« Adresse € », texte fautif sans rapport avec un montant), à traiter en S5.

**Risques connus**
- Tout est testé sous SQLite. Les détections propres à SQL Server n'ont jamais été exécutées : doublon de code (erreurs 2601 et 2627 sur `IX_Clients_Code`) et refus de suppression par clé étrangère (erreur 547).
- Clients existants dont le code postal n'a pas 4 chiffres : refusés à la modification tant qu'ils ne sont pas corrigés (requête de contrôle : `SELECT Id, CodePostal FROM Clients WHERE CodePostal IS NOT NULL AND LEN(CodePostal) <> 4`).
- Matricule fiscal très souple : à resserrer avec l'avis du comptable.
- Le verrou du mode de facturation couvre 4 types de documents : un nouveau type de document devra être ajouté à `ADesDocumentsAsync`.
- Encodages mixtes dans le projet (UTF-8 avec BOM, sans BOM, Windows-1252) et avertissements de fins de ligne LF/CRLF de Git.

- **Effort réalisé** : M à G.
- **Test à la main** : créer un client sans email puis avec le même email en changeant la casse (refus) ; saisir le matricule `12-34` (refus) ; modifier un client, vérifier le bandeau vert et recharger ; essayer le code postal `75001` (refus) ; sur un client qui a un document, vérifier le mode de facturation grisé avec cadenas ; supprimer un client d'essai (message vert) et essayer d'en supprimer un qui a une commande (refus sur la page) ; ouvrir la fiche d'un client avec et sans commande.

### S4 – Articles
- **Objectif** : fiches article fiables et premier correctif de stock.
- **Fait** (preuves vérifiées le 2026-11-19 par `git log`, `dotnet test` et `dotnet ef migrations`) : signe des mouvements de stock corrigé (`8f69627`) ; suppression sûre d'un article et page Delete réelle (`1250db4`) ; création et modification réelles, unicité de la référence et du code-barres contrôlée par l'application (`112bcc4`, `980c33a`) ; index unique filtré sur `CodeBarre` (`f179c7a`) ; fiche Détails réelle (`103e7dc`) et suppression de son code mort (`2436c13`) ; liste des articles : catégories réelles, filtres rupture, stock bas et stock élevé distincts, tri stable, nombre de fournisseurs réel (`d167ef4`) ; double configuration de `TauxTVA` supprimée, `decimal(5,2)` conservé, aucune migration (`01faca4` ; `dotnet ef migrations has-pending-model-changes` : aucun changement) ; symbole € remplacé par DT dans 13 vues et 3 scripts (`cfed4f0`).
- **Reste à faire (S4)** :
  1. **Test manuel dans un navigateur**, jamais fait : Articles (liste, filtres, pagination, Create, Edit, Détails) et Détails d'un bon de réception (le script `goods-receipt-details.js` relit les montants affichés avec le suffixe « DT » : vérifier que le total ne s'affiche pas « NaN »).
  2. **Stock réservé, en commande et disponible** : affichés « N/A » sur la fiche article, décision métier en attente (définitions candidates : `docs/DECISION-PRECISIONS.md`, section 8, document non suivi).
  3. **Erreurs de la fenêtre « Nouveau mouvement de stock »** : affichées en haut de la page Edit et non dans la fenêtre (voir « Limite connue » ci-dessous).
  4. **`catch (DbUpdateConcurrencyException)` de `Articles/Edit.OnPostAddStockMovementAsync`** : aucun test (voir S6).
  5. **Tests de suppression avec clés étrangères désactivées** : le repli sur la violation de clé étrangère de SQL Server n'est pas testé (voir « Limite de test (étape 5) »).
- **Effort** : M.
- **Test à la main** : ajouter une entrée de stock manuelle, la quantité augmente ; un retour fournisseur ou un ajustement négatif la diminue ; la fiche affiche le vrai historique.
- **Limite connue (étape 2, mouvements manuels)** : les erreurs de saisie de la fenêtre « Nouveau mouvement de stock » (motif manquant, stock insuffisant, quantité nulle…) ne sont pas affichées dans la fenêtre elle-même : elles passent par le message d'erreur en haut de la page Edit, après rechargement. À améliorer plus tard (affichage dans la fenêtre).
- **Suppression sûre d'un article (étape 5)** : un article n'est supprimable que s'il n'apparaît dans aucune des 8 tables qui le référencent (`MouvementsStock`, `LignesBonLivraison`, `LignesFacture`, `LignesBonReception`, `LignesCommandeFournisseur`, `LignesCommandeClient`, `LigneDevis`, `ArticleFournisseurs`) **et** si son stock est à 0 (message : « Stock non nul (N unités) : ajustez le stock à 0 ou désactivez l'article »). Le contrôle est dans `ArticleService.SupprimerAsync`, refait à la suppression ; Index, Edit et la page Delete passent par lui.
- **Limite de test (étape 5)** : les tests de suppression tournent sur SQLite avec les clés étrangères désactivées (`Foreign Keys=False`) pour créer une ligne qui référence l'article sans fabriquer tout le document parent. Le contrôle applicatif est donc validé, mais le repli sur une violation de clé étrangère de SQL Server (ligne créée entre le contrôle et la suppression) n'est pas testé.
- **Dette (étape 5)** : `ArticleService` réutilise `ClientService.EstViolationCleEtrangere` ; à déplacer dans un helper commun (par ex. `Helpers/`) lors d'une session qui touche aussi le module Clients.
- **Création et modification réelles via `ArticleService` (étape 3)** : `CreerAsync` et `ModifierAsync` portent les règles N1 à N8 (nettoyage des textes, unicité de la référence et du code-barres insensible à la casse, TVA 0 à 100 avec 2 décimales, stocks, catégorie facultative mais existante, valeurs imposées à la création, courses doublon/catégorie sans exception). Les pages Create et Edit passent par le service ; Create reçoit les prix d'achat HT, de vente HT et TTC (décision D) ; le TTC est libre, jamais calculé ni vérifié (décision C, règle FODEC non validée).
- **Limites de l'étape 3** : (1) tests sur SQLite ; la détection SQL Server d'un doublon simultané (erreurs 2601/2627 avec le nom d'index) et d'une clé étrangère (547) n'est pas exercée ; seules les requêtes d'unicité et de catégorie ont été vérifiées sur la base SQL Server de dev (refus uniquement, aucune écriture). (2) Rien n'a été vérifié dans un navigateur (affichage des erreurs par champ, champs de prix de Create). (3) Corrigé après l'étape 3 : le libellé « Catégorie » du formulaire de création n'a plus la classe `required` (commit `dd7dfe9`), et la fiche Edit affiche maintenant les erreurs du code-barres, de la marque, du modèle, de la couleur et de la taille (emplacements `asp-validation-for` rétablis) ; rendu toujours non vérifié dans un navigateur. (4) [Levé] L'index unique filtré sur le code-barres a été créé (étape 4, `f179c7a`) et appliqué à la base de dev (voir « Migration `UniciteCodeBarre` » ci-dessous).
- **BLOQUANT avant toute base neuve ou tout déploiement : la chaîne de migrations ne reconstruisait pas la base (constaté à l'étape 4)** : 2 migrations appliquées sur la base de dev (`20261109184448_AjoutParametres`, `20261111082521_AjoutDevis1`) n'avaient jamais été commitées et étaient absentes du dépôt (`Migrate()` sur base vide échouait à `AjoutParametres1`). Retrouvées dans la corbeille Windows et réintégrées sans modification (commit `3ddb9d5`) : `Migrate()` sur base vide réussit désormais avec les 19 migrations. **Écart levé le 2026-11-19 (base de dev)** : la base de dev a été recréée depuis la chaîne de migrations ; les 9 colonnes de montants (`BonsReception`, `CommandesClient`, `CommandesFournisseur` : `MontantHT`, `MontantTVA`, `MontantTTC`) y sont en `decimal(18,3)` [vérifié par requête, 9 sur 9], comme dans la chaîne et le snapshot (voir S2). **La production n'a pas été examinée** : sa chaîne et son schéma sont inconnus et doivent être vérifiés avant toute application.
- **Prérequis écrits de l'étape 4 (index unique filtré sur `CodeBarre`)** : (1) rejouer sur CHAQUE base cible, avant l'application, les 4 requêtes de contrôle (doublons après nettoyage, chaînes vides ou blanches, espaces au début ou à la fin, tabulation / saut de ligne / retour chariot / espace insécable) ; (2) une sauvegarde de la base cible est un prérequis, vérifiée avant toute application ; (3) la production n'a pas été examinée ; (4) la migration ne s'applique sur aucune base sans accord explicite après affichage du script.
- **RÈGLE : toute écriture de code-barres passe par `ArticleService`** (`CreerAsync` / `ModifierAsync`). L'index unique filtré (`WHERE [CodeBarre] IS NOT NULL`) accepte plusieurs NULL mais **refuse deux chaînes vides** (constaté sur SQL Server, test S6f) : seul le service convertit une valeur vide ou blanche en NULL et traduit un doublon en erreur sur le champ. Aucune écriture directe dans `Articles.CodeBarre` (SQL, autre service, script). **L'import Excel (S15) doit s'y conformer** (voir S15).
- **Migration `UniciteCodeBarre` générée (étape 4) et APPLIQUÉE à la base de dev** : `20261118231853_UniciteCodeBarre` remplace l'index non unique `IX_Articles_CodeBarre` par un index unique filtré (`WHERE [CodeBarre] IS NOT NULL`) ; elle ne touche à aucune précision decimal (vérifié : le script ne contient ni `decimal` ni `ALTER COLUMN`). `Down` rétablit l'index non unique. **État constaté le 2026-11-19 sur la base de dev (requêtes en lecture seule)** : `dbo.__EFMigrationsHistory` contient 20 lignes et la dernière est `20261118231853_UniciteCodeBarre` ; `IX_Articles_CodeBarre` a `is_unique = 1` et le filtre `([CodeBarre] IS NOT NULL)` ; `dotnet ef migrations list` n'affiche aucune migration en attente. Appliquée sur la base de dev recréée, après sauvegarde `COPY_ONLY` et rejeu des 4 requêtes de contrôle (vides) : voir `docs/DECISION-PRECISIONS.md`, section 7.2 (document non suivi). Sur toute autre base cible, les prérequis ci-dessus restent à rejouer (sauvegarde, 4 requêtes de contrôle, accord explicite) ; la production n'existe pas à ce jour. La détection d'un doublon de code-barres simultané (2601 sur `IX_Articles_CodeBarre`) est écrite et testée sur SQLite et sur SQL Server 13.0.4001 (base jetable).
- **Tests sur un vrai SQL Server (étape 4)** : base jetable LocalDB créée par `Migrate()` et supprimée à la fin (garde-fous : LocalDB obligatoire, nom de base `GestionCommerciale_Test_*`), exécutés seulement avec `GC_TESTS_SQLSERVER=1` (sinon « ignorés »). Numéros réels relevés sur SQL Server 13.0.4001 : doublon de référence = **2601** (message contenant `IX_Articles_Reference`, aussi pour une casse différente) ; violation de clé primaire = **2627** (`PK_Articles`, non confondue avec un doublon de référence) ; clé étrangère = **547** (`FK_Articles_Categories_CategoryId`). Valable pour cette version seulement.

### S5 – Fournisseurs
- **Objectif** : un seul module fournisseur.
- **À faire** : supprimer `Pages/Supplier/` (décision 2, confirmation explicite au moment de la session) en reportant la requête allégée de son Index ; Details et Delete réels ; unicité du nom, de l'email et du matricule ; liste unique de gouvernorats ; script JS manquant ; pagination bornée. [vérifié : `Pages/Supplier/` existe encore à côté de `Pages/Fournisseurs/` ; le reste est déduit de l'audit du 2026-11-15, non revérifié dans le code]
- **Effort** : M.
- **Test à la main** : créer, modifier, supprimer un fournisseur ; refuser la suppression d'un fournisseur ayant des articles.

### S6 – Stock (historique des mouvements)
- **Objectif** : une seule règle pour tous les mouvements.
- **Fait (branche `s6-stock`, version « socle » réduite, vérifié le 2026-11-19 par `git log`, `dotnet test` et un test sur SQL Server LocalDB)** :
  1. **Convention unique** (`f7a8fef`) : quantité d'un mouvement **toujours positive**, sens porté par le type (`TypeMouvementStockExtensions.Sens()`, défini une seule fois). Types ajoutés : `AjustementPositif`, `AjustementNegatif`, `EntreeAnnulationLivraison`. L'enum est stocké en entier : **aucune migration** (`has-pending-model-changes` : aucun changement). L'ancien `AjustementInventaire` (valeur 2) est conservé pour lire d'anciennes lignes mais refusé pour tout nouveau mouvement.
  2. **Point d'entrée unique** (`f7a8fef`) : `StockService.AppliquerMouvement` crée tous les mouvements ; il refuse toute quantité <= 0, tout type sans sens et tout stock négatif, et renseigne `QuantiteAvant`, `QuantiteApres`, `Reference`, `SourceType`, `SourceId`. `MouvementStock.QuantiteSignee` (calculée, non enregistrée) sert aux trois écrans qui lisaient le signe : fiche article (`Articles/Details`), historique de `Articles/Edit` et `Stock/Mouvements`.
  3. **Annulation d'un BL** (`9c7f4a9`) : mouvements inverses de type `EntreeAnnulationLivraison` (le mouvement d'origine n'est jamais supprimé), idempotente (statut et garde-fou sur l'existence d'un mouvement d'annulation), refusée s'il existe une facture non annulée liée au BL. `BonLivraisonService` passe maintenant par `StockService`.
  4. **Concurrence** (`9c7f4a9`) : on garde le jeton `Article.RowVersion` existant (colonne `timestamp` confirmée sur la base de dev) ; `BonsLivraison/Details.cshtml.cs` attrape `DbUpdateConcurrencyException` (message clair, rien d'enregistré). Prouvé sur un vrai SQL Server : deux validations simultanées, la seconde échoue, stock 10 - 8 = 2, jamais négatif (`SqlServerStockConcurrenceTests`, avec `GC_TESTS_SQLSERVER=1`).
- **Reste à faire (S6)** :
  1. **Statut `Facture` du BL jamais posé** [vérifié] : `FactureService` ne passe jamais le BL au statut `Facture` (il ne fait que renseigner `Facture.BonLivraisonId`), donc le test `bl.Statut == Facture` de `BonLivraisonService.AnnulerAsync` ne se déclenche jamais ; le refus repose sur la facture liée non annulée. À traiter en **S10** (facturation).
  2. **Commandes client** [vérifié] : ~~`Commande_Client/Create.cshtml.cs:145` et `Edit.cshtml.cs:174` et `:224` modifiaient `Article.Quantite` sans aucun mouvement (double sortie avec le BL)~~ : **corrigé en S7** (commit `88e62fb`, voir S7).
  3. **Entrée de stock à la réception fournisseur** [déduit de l'audit : aucune page de validation de BR dans `Pages/GoodsReceipt/`] : S8.
  4. **Page d'historique réelle par article** [déduit, non revérifié].
  5. **`CoutUnitaire` non renseigné** [vérifié] : `Article.CMUP` n'est calculé nulle part, il n'y a donc pas de source fiable.
  6. **Données existantes** [vérifié sur la base de dev] : un seul mouvement (`SortieLivraison`, +10, `QuantiteAvant` = 0), conforme à la convention ; non modifié. Sur toute autre base, contrôler avant déploiement qu'aucune quantité de `MouvementsStock` n'est négative ou nulle.
  7. **Tests** : sous SQLite, qui ne reproduit pas le rowversion, la course de la page est simulée ; le `catch` de `Articles/Edit.OnPostAddStockMovementAsync` n'a toujours aucun test dédié (le mécanisme rowversion, lui, est prouvé par le test SQL Server) ; les écrans (formulaire de mouvement, historique) ne sont vérifiés que par lecture du texte des vues, pas dans un navigateur ; test SQL Server fait sur une seule version de LocalDB.
  8. **Dette** [vérifié] : `Services/Interface/IBonLivraisonService.cs` ne correspond plus au service (la classe `BonLivraisonService` ne l'implémente pas et ses signatures diffèrent).
- **Contrat de `StockService` (historique)** : enregistré dans `Program.cs` depuis S4 étape 2. Les mentions de S4 étape 2 (quantité signée, `NotSupportedException` pour `AjustementInventaire`) sont remplacées par la convention de S6 ci-dessus : quantité positive, sens par le type, saisie manuelle `EnregistrerMouvementManuelAsync` avec motif obligatoire pour les deux ajustements. `BonLivraisonService` utilise maintenant `StockService` (plus de modification directe de `Article.Quantite`, plus d'annulation en `EntreeReception`, `QuantiteAvant` renseignée).
- **Limite de test (S4 étape 2, reprise)** : voir le point 7 de « Reste à faire (S6) ».
- **Effort** : M.
- **Test à la main** : valider puis annuler un BL, le stock revient et l'historique montre deux mouvements cohérents.

### S7 – Commandes client
- **Objectif** : commandes justes, sans toucher au stock (décision 1).
- **Fait (version « stock » réduite, branche `s6-stock`, vérifié le 2026-11-19 par `dotnet test` : 438 réussis, 20 ignorés SQL Server, 0 échec)** : commit `88e62fb`.
  1. **La commande ne touche plus au stock** [vérifié] : plus aucune écriture sur `Article.Quantite` hors `StockService` (recherche sur tout le code). Supprimées : `Create.cshtml.cs:145`, `Edit.cshtml.cs:174` et `:224`. Créer, modifier, supprimer ou passer une commande au statut Annulée laisse le stock inchangé (tests). **Le risque de double sortie (commande puis BL) est levé.**
  2. **Règle A (survente), en lecture seule** [vérifié] : Create et Edit refusent une ligne dont la quantité dépasse le **stock physique actuel**, sans aucune réservation (le message le dit). Dans Edit, la vérification ne s'applique que si les lignes changent. Les libellés « Stock disponible » et les alertes JavaScript des pages de commande disent « Stock physique actuel ».
  3. **Commande déjà liée à un BL** [vérifié] : Edit refuse de modifier ses lignes (message clair, lien `LigneBonLivraison` -> ligne de commande et `QuantiteLivree` conservés) ; l'en-tête reste modifiable, et si les lignes ne changent pas, elles et les montants ne sont pas touchés. Contrôle sur l'existence de n'importe quel BL lié, quel que soit son statut.
  4. **Parcours testé** : commande -> BL validé -> annulation du BL donne le stock 10 -> 10 -> 6 -> 10, avec les mouvements `SortieLivraison` puis `EntreeAnnulationLivraison` (SQLite uniquement).
- **Décision métier ouverte (règle C)** : « stock disponible = stock - réservé » (réservé = lignes de commande confirmées non livrées). Non implémentée : elle dépend des statuts à compter et de la fiabilité de `QuantiteLivree` (voir `docs/DECISION-PRECISIONS.md`, section 8, document non suivi). Tant qu'elle n'est pas décidée, deux commandes peuvent chacune demander tout le stock ; le BL reste la protection finale (`ValiderAsync` refuse un stock insuffisant, le `RowVersion` protège la concurrence).
- **BLOQUANT avant pilote (non corrigé, consigné le 2026-11-19)** [vérifié dans le code] :
  1. **Edit : suppression et recréation des lignes** quand elles changent : `QuantiteLivree` et le lien `LigneBonLivraison.LigneCommandeClientId` seraient perdus. Ce risque est évité seulement pour les commandes qui ont un BL (modification des lignes refusée, point 3 ci-dessus) ; la correction de fond (mettre à jour les lignes existantes au lieu de les recréer) reste à faire.
  2. ~~Edit : calcul du HT avec `PrixAchatHT`~~ : **corrigé en S7bis** (commit `f17d71f`, voir ci-dessous).
  3. ~~Create : `TauxTVA` et `MontantLigne` jamais renseignés~~ : **corrigé en S7bis** (commit `f17d71f`).
- **S7bis : montants de commande (HT / TVA / TTC) corrects et centralisés** (branche `s7bis-montants`, vérifié le 2026-11-19 par `dotnet test` : 482 réussis, 21 ignorés dont les tests SQL Server, 0 échec) :
  1. **Fait** [vérifié] (`f17d71f`) : Create et Edit copient `PrixVenteHT` (`PrixUnitaire`) et `TauxTVA` de l'article sur chaque ligne, puis appellent `commande.RecalculerTotaux()` (`Helpers/CalculTotaux`, qui existait mais n'était appelé nulle part et n'avait aucun test). Règle : `MontantLigne` = `Quantité x PrixUnitaire` arrondi au millime ; TVA **par taux** sur la somme nette des lignes de ce taux, arrondie au millime ; remise globale en % répartie au prorata, le dernier taux reçoit le reste ; **AwayFromZero** partout ; pas de timbre sur une commande. Aucune migration (colonnes déjà en `decimal(18,3)`). Le prix posté par le navigateur est ignoré. **Prix et TVA figés** : modifier l'article ensuite ne change pas une commande ; dans l'Edit, un article déjà présent garde son prix et son taux d'origine, seul un article nouveau prend le prix actuel. L'Edit n'utilise plus `PrixAchatHT`.
  2. **Tests** [vérifié] : `CalculTotauxTests` (32 cas calculés à la main, dont des cas où `ToEven` et `AwayFromZero` divergent), `CommandeMontantsTests` (stockage relu = `CalculTotaux`, aucune valeur à 0, prix figé), `CommandeBlFactureTests` (parcours commande -> BL -> facture à chiffres fixes).
  3. **Affichage JavaScript** (Create et Edit) aligné [vérifié par lecture du texte, **non exécuté** : pas de moteur JavaScript disponible] : calcul en millimes entiers, TVA par taux, 3 décimales. Exact jusqu'à environ 900 millions de DT par taux ; le serveur recalcule tout. Placeholders « 0,000 DT ».
  4. **Points ouverts, décisions métier** : (a) **arrondi : HT par ligne au millime et TVA par taux sur la base nette, à confirmer avec le comptable** (règle fiscale, non validée) ; (b) **remise** : % global uniquement, il n'existe aucun champ de saisie de la remise dans les pages de commande [déduit], pas de remise par ligne (le modèle de ligne de commande n'en a pas : migration) ; (c) **quantités entières** : la **vente au kilo ou au mètre** (quantités décimales) est une décision métier ouverte, elle exigerait une migration sur les lignes de commande et de BL (`LigneFacture.Quantite` est déjà décimale, incohérence).
  5. **BLOQUANT avant pilote (non corrigés, prouvés ou lus le 2026-11-19), à traiter en S9 et S10** :
     - **Résolus en S10** (commits `14e8337` et `0cea48b`) : facture au prix et à la TVA actuels de l'article ; arrondi `ToEven` en facture ; colonnes `Facture` en `decimal(18,2)` (corrigé dans le code et par la migration `S10Facturation`, **non appliquée** à la base de dev) ; prix du BL forgeable. Voir S10.
     - **Devis vers commande** : `DevisService.TransformerEnCommandeAsync` ne copie pas `TauxTVA` (reste à 0 sur les lignes) et perd la remise par ligne du devis (prix net reporté) [vérifié par lecture du code, non testé].
     - **Formatage `"C"` dépendant de la culture** du serveur dans `Commande_Client/Details` et `Index` (le symbole monétaire affiché n'est pas garanti « DT ») [vérifié par lecture].
     - Le total de ligne de `Commande_Client/Details` est recalculé à l'affichage (`Quantite x PrixUnitaire`) au lieu de lire `MontantLigne` [vérifié par lecture] ; équivalent tant que les lignes sont créées par Create et Edit.
- **Données de test de la base de dev à purger (non corrigées)** [vérifié par requêtes en lecture seule le 2026-11-19] : 3 commandes (`CC-202610-0002`, `CC-202610-0003`, `CC-202611-0001`, ids 2, 3, 4), 4 lignes, sans aucun mouvement de stock associé. Ces commandes ont **probablement** fait sortir du stock sans mouvement (jusqu'à 24 unités au total ; [déduit] du code ancien, non prouvable par les données, qui n'ont pas de stock initial) ; la commande 3 a en plus un BL validé (sortie de 10 unités déjà comptée une fois). La commande d'identifiant 1 (`CC-202610-0001`) **n'existe plus** : sa suppression n'avait jamais restitué le stock. La commande 2 est au statut **Livrée** sans BL ni quantité livrée (statut posé à la main). Les stocks des articles concernés (2, 3, 4) ne sont donc pas fiables : purger ou recalculer ces données de test avant tout pilote. **Montants (S7bis, requêtes en lecture seule le 2026-11-19)** : les 4 lignes de commande ont `TauxTVA` = 0 et `MontantLigne` = 0 (100 %) ; comme **tous les prix de tous les articles valent 0** dans cette base, aucun total de commande n'est faux (0 = 0) et il n'y a aucune donnée monétaire réelle ; la facture 1 (Validée) a un HT de 0 et un TTC de 1,00 (le seul timbre). Ces commandes et cette facture sont des données de test à purger, non corrigées.
- **Reste à faire (S7 complet)** [déduit de l'audit, non revérifié dans le code, sauf mention] : champ de saisie de la remise globale (le calcul la gère déjà) ; transitions de statut contrôlées (l'Edit accepte n'importe quel statut [vérifié]) ; plafond de crédit et délai de paiement pour les clients en compte ; numérotation sûre (sans verrou) ; vraie page Delete (`Commande_Client/Delete`, factice) ; suppression d'une commande liée à un BL (non gérée explicitement, retombe sur l'erreur générique) ; test manuel dans un navigateur des pages Create et Edit (non fait).
- **Effort** : M.
- **Test à la main** : créer une commande, le stock ne bouge pas ; demander plus que le stock physique actuel est refusé avec un message clair ; modifier les lignes d'une commande qui a un BL est refusé ; modifier une commande partiellement livrée sans perdre la livraison (non couvert) ; dépasser le plafond de crédit est signalé (non implémenté).

### S8 – Commandes fournisseur et bons de réception
- **Objectif** : la marchandise reçue entre en stock.
- **À faire** : TVA par ligne issue de l'article ; prix recalculés côté serveur ; reliquat par ligne ; validation et annulation du BR avec entrée de stock ; statut de la commande mis à jour ; Details réel ; corriger l'injection de `IRetenueSourceService` ; numérotation sûre. [vérifié : `GoodsReceipt/Create.cshtml.cs:21` injecte `RetenueSourceService` concret alors que seule l'interface est enregistrée (`Program.cs:28`), et `Pages/GoodsReceipt/` n'a aucune page de validation de BR ; le reste est déduit de l'audit, non revérifié dans le code]
- **Effort** : G.
- **Test à la main** : créer puis valider un BR, la quantité de l'article augmente ; recevoir plus que le reliquat est refusé.

### S9 – Bons de livraison
- **Objectif** : BL complets et cohérents avec la commande.
- **À faire** : statut de la commande après livraison (partielle ou totale) ; édition et suppression d'un brouillon ; prix et TVA figés sur la ligne du BL ; contrôle du client et de la commande au POST ; gestion d'erreurs et de concurrence sur Valider et Annuler. [vérifié : la mise à jour du statut de la commande est en commentaire, `BonLivraisonService.cs:76-78` ; le reste est déduit de l'audit, non revérifié dans le code]
- **Effort** : M.
- **Test à la main** : deux livraisons partielles d'une commande, puis annulation de l'une.

### S10 – Facturation (après avis du comptable)
- **Objectif** : factures conformes.
- **À noter (S6)** [vérifié] : `FactureService` ne passe jamais le BL au statut `StatutBonLivraison.Facture` ; il renseigne seulement `Facture.BonLivraisonId`. Ce statut n'est toujours pas posé en S10 : la règle « BL déjà facturé » (facturation et annulation du BL) repose sur l'existence d'une facture non annulée liée au BL, protégée par un index unique filtré.
- **Prérequis** : réponses du comptable sur FODEC, timbre, retenue à la source, numérotation et exercice. Aucune règle fiscale supposée.
- **Fait (branche `s10-facturation`, vérifié le 2026-11-21 par `dotnet test` : 514 réussis, 24 ignorés dont les tests SQL Server, 0 échec ; tests SQL Server exécutés avec `GC_TESTS_SQLSERVER=1` sur une base LocalDB jetable supprimée ensuite)** : commits `14e8337` (CalculTotaux étendu) et `0cea48b` (facturation, migration), plus le test de parcours complet (commit suivant).
  1. **Lignes de facture depuis le BL** [vérifié par test] : article et quantité du BL ; **prix, TVA** de la **ligne de commande liée** (figés), jamais de l'article du jour ; remise globale de la commande reprise. Le prix de la ligne de BL est désormais dérivé côté serveur de la ligne de commande liée (le prix posté par le navigateur est ignoré) et chaque ligne de BL doit être liée à une ligne de commande.
  2. **Un seul mode d'arrondi** [vérifié] : `CalculTotaux` (millime, `AwayFromZero`) pour la commande et la facture ; fin de l'arrondi `ToEven` en facture. Extension par surcharge : les résultats de commande sont identiques avant et après (test sur 14 jeux).
  3. **FODEC, TVA, timbre** [vérifié par test] : ordre remise globale, puis FODEC sur le HT net des **seules lignes marquées** (`Article.Fodec` copié sur la ligne de facture) ; aucun article n'étant marqué dans la base de dev, le FODEC vaut 0 (fin du 1 % appliqué à tout) ; TVA par taux ; la base de TVA inclut ou non le FODEC selon le paramètre société `BaseTvaInclutFodec`, **défaut : exclu (comportement antérieur)** ; timbre de `ParametresSociete`, **un par facture** (donc un par BL facturé).
  4. **Contrôles** [vérifié] : BL validé obligatoire (brouillon et annulé refusés), non déjà facturé, non vide, toutes lignes liées ; **une facture non annulée par BL** (index unique filtré `IX_Factures_BonLivraisonId`, course prouvée sur SQL Server : erreur 2601, traduite en message clair) ; une facture annulée ne bloque pas une nouvelle facture du même BL. La page de création ne propose que les BL validés.
  5. **Règle de calcul versionnée** [vérifié par test et sur SQL Server] : `Facture.RegleCalcul` (1 = factures d'avant S10, 2 = nouvelles) ; une facture de règle 1, ou validée, n'est **jamais recalculée**, même si les paramètres société changent ; la migration marque les factures existantes en règle 1 et conserve leurs montants à l'identique.
  6. **Migration `20261120231803_S10Facturation` générée et relue, NON APPLIQUÉE à la base de dev** [vérifié : historique de dev à 20 migrations, colonne `RegleCalcul` absente] : `Facture` (`TotalHt`, `TotalTva`, `TotalTtc`, `TimbreFiscal`, `MontantFodec`, `NetAPayer`, `MontantRetenueSource`) et `LigneFacture` (`PrixUnitaireHt`, `TotalHt`) de `decimal(18,2)` à `decimal(18,3)` (élargissement : aucune troncature des valeurs existantes ; les factures validées déjà arrondies à 2 décimales restent telles quelles) ; colonnes `Facture.RegleCalcul` (défaut 1, corrigé à la main), `LigneFacture.SoumiseFodec`, `ParametresSociete.BaseTvaInclutFodec` ; index unique filtré. **Avant application sur une base** : sauvegarde, accord explicite, et requête de doublons `SELECT BonLivraisonId, COUNT(*) FROM Factures WHERE BonLivraisonId IS NOT NULL AND Statut <> 2 GROUP BY BonLivraisonId HAVING COUNT(*) > 1` (doit être vide, sinon l'index échoue). Migration testée sur une base jetable, y compris sa méthode `Down` et son application sur une facture existante.
  7. **Test de parcours complet** commande, BL validé, facture validée, annulation du BL refusée (facturé), facture non recalculée si les paramètres changent : mêmes totaux de bout en bout (HT 22,005 ; TVA 4,191 ; TTC 26,196 en commande, 27,196 en facture avec le timbre). Aucun avoir n'existe encore (S13).
- **En attente de validation du comptable (rien supposé, tout paramétré ou isolé)** : quels articles sont soumis au FODEC et à quel taux ; la base de TVA inclut-elle le FODEC (`BaseTvaInclutFodec`) ; le timbre fiscal (une fois par facture, y compris pour des BL partiels successifs) ; arrondi par ligne puis par taux ; retenue à la source ; numérotation et exercice.
- **BLOQUANT avant pilote (S12)** [vérifié par lecture de `FacturePdfService`] : le PDF n'affiche **ni le FODEC ni la remise en montant** (seulement le taux de la remise globale), donc le TTC imprimé ne se déduit pas des lignes affichées.
- **Fusion des branches** [vérifié par `git branch` et `git log`] : `main` est resté à la V1 (`80100cc`) ; `s3-clients`, `s4-articles`, `s6-stock`, `s7bis-montants` et `s10-facturation` sont chaînées **sans fusion**. Prévoir une fusion séquentielle, dans cet ordre, avec la suite de tests complète (et les tests SQL Server) après chaque fusion.
- **Reste à faire (S10)** [déduit de l'audit, non revérifié dans le code, sauf mention] : appliquer la migration (accord et sauvegarde) ; facture directe pour le client passager ; facture mensuelle depuis plusieurs BL ; modification du brouillon ; échéance selon `Client.DelaiPaiement` (aujourd'hui 30 jours fixes [vérifié]) ; une seule numérotation : `FactureService` utilise `ISequenceService` alors que `NumerotationService` existe [vérifié] ; retenue à la source (S11) : dette connue documentée par un test (`ResteAPayer` déduit deux fois la retenue) ; `ClientId` du BL encore posté par le formulaire et non dérivé de la commande [vérifié] ; une même ligne de commande peut apparaître deux fois dans un BL (non contrôlé) [déduit] ; `ParametresSociete.FodecDefaut` n'a plus aucun effet (libellé de la page de paramètres adapté) ; aucun article n'est marqué FODEC (à renseigner selon le comptable) ; devis vers commande : `TauxTVA` à 0 et remise perdue (voir S7bis) ; facture annulée et avoir : S13.
- **Effort** : G.
- **Test à la main** : facturer un BL, tenter de le facturer à nouveau (refusé), générer la facture mensuelle d'un client.

### S11 – Règlements puis échéanciers
- **Objectif** : encaissements justes.
- **À faire** : supprimer la triple déduction de la retenue à la source ; refuser un règlement sur brouillon ou facture annulée ; transaction et concurrence ; remplacer la cascade Règlement vers Facture ; échéancier et suivi des retards. [déduit de l'audit du 2026-11-15, non revérifié dans le code]
- **Effort** : G.
- **Test à la main** : deux règlements partiels puis solde ; un règlement supérieur au reste est refusé.

### S12 – PDF et mentions légales
- **Objectif** : facture imprimable et conforme.
- **À faire** : lire les paramètres de la société (nom, matricule, adresse, logo) ; mentions légales validées par le comptable ; récapitulatif de TVA par taux ; FODEC ; montant en lettres ; « net à payer » correct ; archivage du PDF figé à la validation ; formats de nombres indépendants du serveur. [déduit de l'audit du 2026-11-15, non revérifié dans le code]
- **Effort** : M.
- **Test à la main** : comparer un PDF avec un exemple validé par le comptable.

### S13 – Avoirs
- **Objectif** : corriger une facture validée sans la modifier.
- **À faire** : modèle, numérotation, lien avec la facture d'origine, retour de stock éventuel, impact sur le solde client, PDF. [déduit de l'audit du 2026-11-15, non revérifié dans le code]
- **Prérequis** : règles d'avoir validées par le comptable.
- **Effort** : G.

### S14 – Sécurité
- **Objectif** : accès contrôlé et traçable avant toute vraie donnée.
- **À faire** : authentification, rôles, `[Authorize]` sur toutes les pages, protection des paramètres fiscaux et des PDF ; journal d'audit (qui, quand, quoi) sur validations, annulations, règlements et paramètres ; seed de paramètres factices retiré ; `Migrate()` et configuration par environnement.
- **Effort** : G.

### S15 – Import Excel
- **Objectif** : reprendre les données existantes.
- **À faire** : import des articles, clients et fournisseurs avec contrôle, rapport d'erreurs et mode simulation avant écriture.
- **Contrainte (règle code-barres, voir S4)** : l'import des articles doit écrire par `ArticleService.CreerAsync` / `ModifierAsync` (jamais directement dans `Articles`), pour bénéficier du nettoyage, de la validation et du traitement des doublons et des courses. En particulier, une cellule de code-barres vide ou blanche doit devenir NULL (le service le fait) : l'index unique refuse deux chaînes vides.
- **Effort** : G.

### S16 – Reporting
- **Objectif** : tableaux de bord utiles.
- **À faire** : ventes, balance âgée, stock et valorisation, TVA, retenues à la source ; exports.
- **Effort** : G.

### S17 – Audit final et exploitation
- **Objectif** : produit livrable.
- **À faire** : audit de performance (pagination, projections, `AsNoTracking`), nettoyage du code mort et de l'encodage, `Dockerfile` et configuration, journalisation, revue de sécurité, tests de bout en bout.
- **Rappel** : la sauvegarde SQL Server reste une tâche d'exploitation, hors développement.
- **Effort** : M.
