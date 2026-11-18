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
- Il n'y a aucun projet de tests.
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
- **Nettoyage** : doublons (`Supplier/`, `Categorie`/`Category`), ViewModels et scripts orphelins, code commenté, encodage mixte, symbole € au lieu de DT.
- **PDF** : en-tête en dur, paramètres société non lus, mentions légales incomplètes, PDF non figé.

### Tableau priorisé
**Bloquant**
| Module | Problème | Fichier | Effort |
|---|---|---|---|
| Build | Erreur de compilation | `Pages/Clients/Create.cshtml:46` | S |
| Migrations | `Client.Code`, `ModeFacturation`, index unique absents ; `InitialCreate` modifiée | `Migrations/`, `Models/Client.cs` | M |
| Clients | Edit, Delete, Details factices | `Pages/Clients/*` | M |
| Stock et commandes | Double sortie de stock | `Commande_Client/Create:145`, `BonLivraisonService:55` | M |
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
| Tests | Aucun projet de tests | – | M |

**Souhaitable**
| Module | Problème | Fichier | Effort |
|---|---|---|---|
| Règlements | Échéancier, relances, multi-factures, trop-perçu | `ReglementService` | G |
| Avoirs | Inexistants | – | G |
| Audit | Journal des actions sensibles | – | M |
| Import Excel | Articles, clients, fournisseurs | – | G |
| Reporting | Balance âgée, ventes, stock, TVA, retenues | – | G |
| Performance | Pagination BL et commandes fournisseur, projections | Pages Index | S à M |
| Nettoyage | Doublons, code mort, encodage, € vers DT | divers | M |
| Exploitation | Dockerfile, secrets, configuration par environnement | `Dockerfile` | M |

---

## Sessions de travail

### S1 – Remise en état (réalisée ; découpage de l'historique abandonné)
- **Objectif** : un build vert et un dépôt propre.

**Réalisé** (preuve entre parenthèses, vérifiée le 2026-11-17)
- **Erreur de build corrigée** : `@using GestionCommerciale.Enums` est en ligne 3 de `Pages/Clients/Create.cshtml` (commit `5ed700a` « Build corrigé »).
- **Projet de tests créé avec un premier test** : `GestionCommerciale.Tests` et `ClientCodeTests.cs` (commit `3819ba1`). La solution compte aujourd'hui 89 tests.
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
- **Double configuration de `TauxTVA` : déplacée vers S4** (décision du 2026-11-17), car elle concerne les Articles. Elle n'est pas faite ; voir S4.
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
5. **Symbole € dans les autres modules** (Articles, commandes, réceptions, fournisseurs) : à traiter dans leurs sessions.

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
- **À faire** : Details et Delete réels ; unicité de la référence et du code-barres ; filtres catégorie et rupture corrects ; corriger le signe inversé de `Articles/Edit.cshtml.cs:223` ; affichage en DT ; **supprimer la double configuration de `TauxTVA`** (déplacée depuis S2) : `Infrastructure/ArticleConfiguration.cs:23` donne `HasPrecision(18, 3)` et `Data/ApplicationDbContext.cs:105` donne `HasPrecision(5, 2)` ; la colonne `Articles.TauxTVA` est déjà en `decimal(5,2)` dans le snapshot (le DbContext l'emporte), donc il reste à retirer une des deux configurations (probablement celle de `ArticleConfiguration`) et à vérifier avec `dotnet ef migrations has-pending-model-changes` qu'aucune migration n'est nécessaire.
- **Effort** : M.
- **Test à la main** : ajouter une entrée de stock manuelle, la quantité augmente ; un retour fournisseur ou un ajustement négatif la diminue ; la fiche affiche le vrai historique.
- **Limite connue (étape 2, mouvements manuels)** : les erreurs de saisie de la fenêtre « Nouveau mouvement de stock » (motif manquant, stock insuffisant, quantité nulle…) ne sont pas affichées dans la fenêtre elle-même : elles passent par le message d'erreur en haut de la page Edit, après rechargement. À améliorer plus tard (affichage dans la fenêtre).
- **Suppression sûre d'un article (étape 5)** : un article n'est supprimable que s'il n'apparaît dans aucune des 8 tables qui le référencent (`MouvementsStock`, `LignesBonLivraison`, `LignesFacture`, `LignesBonReception`, `LignesCommandeFournisseur`, `LignesCommandeClient`, `LigneDevis`, `ArticleFournisseurs`) **et** si son stock est à 0 (message : « Stock non nul (N unités) : ajustez le stock à 0 ou désactivez l'article »). Le contrôle est dans `ArticleService.SupprimerAsync`, refait à la suppression ; Index, Edit et la page Delete passent par lui.
- **Limite de test (étape 5)** : les tests de suppression tournent sur SQLite avec les clés étrangères désactivées (`Foreign Keys=False`) pour créer une ligne qui référence l'article sans fabriquer tout le document parent. Le contrôle applicatif est donc validé, mais le repli sur une violation de clé étrangère de SQL Server (ligne créée entre le contrôle et la suppression) n'est pas testé.
- **Dette (étape 5)** : `ArticleService` réutilise `ClientService.EstViolationCleEtrangere` ; à déplacer dans un helper commun (par ex. `Helpers/`) lors d'une session qui touche aussi le module Clients.
- **Création et modification réelles via `ArticleService` (étape 3)** : `CreerAsync` et `ModifierAsync` portent les règles N1 à N8 (nettoyage des textes, unicité de la référence et du code-barres insensible à la casse, TVA 0 à 100 avec 2 décimales, stocks, catégorie facultative mais existante, valeurs imposées à la création, courses doublon/catégorie sans exception). Les pages Create et Edit passent par le service ; Create reçoit les prix d'achat HT, de vente HT et TTC (décision D) ; le TTC est libre, jamais calculé ni vérifié (décision C, règle FODEC non validée).
- **Limites de l'étape 3** : (1) tests sur SQLite ; la détection SQL Server d'un doublon simultané (erreurs 2601/2627 avec le nom d'index) et d'une clé étrangère (547) n'est pas exercée ; seules les requêtes d'unicité et de catégorie ont été vérifiées sur la base SQL Server de dev (refus uniquement, aucune écriture). (2) Rien n'a été vérifié dans un navigateur (affichage des erreurs par champ, champs de prix de Create). (3) Corrigé après l'étape 3 : le libellé « Catégorie » du formulaire de création n'a plus la classe `required` (commit `dd7dfe9`), et la fiche Edit affiche maintenant les erreurs du code-barres, de la marque, du modèle, de la couleur et de la taille (emplacements `asp-validation-for` rétablis) ; rendu toujours non vérifié dans un navigateur. (4) L'index unique filtré sur le code-barres reste à créer (étape 4) : sans lui, seul le contrôle applicatif protège le code-barres.
- **BLOQUANT avant toute base neuve ou tout déploiement : la chaîne de migrations ne reconstruisait pas la base (constaté à l'étape 4)** : 2 migrations appliquées sur la base de dev (`20261109184448_AjoutParametres`, `20261111082521_AjoutDevis1`) n'avaient jamais été commitées et étaient absentes du dépôt (`Migrate()` sur base vide échouait à `AjoutParametres1`). Retrouvées dans la corbeille Windows et réintégrées sans modification (commit `3ddb9d5`) : `Migrate()` sur base vide réussit désormais avec les 19 migrations. **Écart restant** : 9 colonnes de montants (`BonsReception`, `CommandesClient`, `CommandesFournisseur` : `MontantHT`, `MontantTVA`, `MontantTTC`) sont en `decimal(18,3)` dans la chaîne mais en `decimal(18,2)` dans la base de dev (à traiter dans la migration des précisions decimal, S2 non faite). **La production n'a pas été examinée** : sa chaîne et son schéma sont inconnus et doivent être vérifiés avant toute application.
- **Prérequis écrits de l'étape 4 (index unique filtré sur `CodeBarre`)** : (1) rejouer sur CHAQUE base cible, avant l'application, les 4 requêtes de contrôle (doublons après nettoyage, chaînes vides ou blanches, espaces au début ou à la fin, tabulation / saut de ligne / retour chariot / espace insécable) ; (2) une sauvegarde de la base cible est un prérequis, vérifiée avant toute application ; (3) la production n'a pas été examinée ; (4) la migration ne s'applique sur aucune base sans accord explicite après affichage du script.
- **Migration `UniciteCodeBarre` générée, NON appliquée (étape 4)** : `20261118231853_UniciteCodeBarre` remplace l'index non unique `IX_Articles_CodeBarre` par un index unique filtré (`WHERE [CodeBarre] IS NOT NULL`) ; elle ne touche à aucune précision decimal (vérifié : le script ne contient ni `decimal` ni `ALTER COLUMN`). `Down` rétablit l'index non unique. À appliquer seulement après : sauvegarde de la base cible, rejeu des 4 requêtes de contrôle sur cette base, accord explicite. Sur la base de dev, les 4 requêtes sont vides (6 articles, codes tous distincts) ; la production n'a pas été examinée. La détection d'un doublon de code-barres simultané (2601 sur `IX_Articles_CodeBarre`) est écrite et testée sur SQLite et sur SQL Server 13.0.4001 (base jetable).
- **Tests sur un vrai SQL Server (étape 4)** : base jetable LocalDB créée par `Migrate()` et supprimée à la fin (garde-fous : LocalDB obligatoire, nom de base `GestionCommerciale_Test_*`), exécutés seulement avec `GC_TESTS_SQLSERVER=1` (sinon « ignorés »). Numéros réels relevés sur SQL Server 13.0.4001 : doublon de référence = **2601** (message contenant `IX_Articles_Reference`, aussi pour une casse différente) ; violation de clé primaire = **2627** (`PK_Articles`, non confondue avec un doublon de référence) ; clé étrangère = **547** (`FK_Articles_Categories_CategoryId`). Valable pour cette version seulement.

### S5 – Fournisseurs
- **Objectif** : un seul module fournisseur.
- **À faire** : supprimer `Pages/Supplier/` (décision 2, confirmation explicite au moment de la session) en reportant la requête allégée de son Index ; Details et Delete réels ; unicité du nom, de l'email et du matricule ; liste unique de gouvernorats ; script JS manquant ; pagination bornée.
- **Effort** : M.
- **Test à la main** : créer, modifier, supprimer un fournisseur ; refuser la suppression d'un fournisseur ayant des articles.

### S6 – Stock (historique des mouvements)
- **Objectif** : une seule règle pour tous les mouvements.
- **À faire** : fixer la convention (quantité signée) ; enregistrer `StockService` dans `Program.cs` et l'utiliser partout ; type de mouvement correct à l'annulation d'un BL ; protection de concurrence sur `Article.Quantite` ; page d'historique réelle par article.
- **Changement de contrat de `StockService` (fait en S4 étape 2, à connaître)** : `StockService` est maintenant enregistré dans `Program.cs`. `EnregistrerMouvementAsync` lève `NotSupportedException` pour `AjustementInventaire` (un ajustement est signé : utiliser `EnregistrerMouvementManuelAsync`, qui exige un motif et refuse un stock négatif) ; `RetourFournisseur` y est géré en sortie ; `QuantiteAvant` y est renseignée. Aucun code n'appelait cette méthode avant S4 (recherche sur `.cs` et `.cshtml`), donc rien n'était cassé. **`BonLivraisonService` ne l'utilise toujours pas** : il modifie `Article.Quantite` directement (`BonLivraisonService.cs:55` et `:109`), enregistre les sorties en quantité positive (`:62`), enregistre l'annulation d'un BL avec le type `EntreeReception` (`:115`) et ne renseigne pas `QuantiteAvant`. À reprendre ici en S6.
- **Limite de test (S4 étape 2)** : le `catch (DbUpdateConcurrencyException)` de `Articles/Edit.OnPostAddStockMovementAsync` n'est couvert par aucun test (SQLite ne reproduit pas `rowversion`). À vérifier sur SQL Server lors de la protection de concurrence de `Article.Quantite`.
- **Effort** : M.
- **Test à la main** : valider puis annuler un BL, le stock revient et l'historique montre deux mouvements cohérents.

### S7 – Commandes client
- **Objectif** : commandes justes, sans toucher au stock (décision 1).
- **À faire** : supprimer la sortie de stock à la création et à l'édition ; copier `TauxTVA` dans la ligne à sa création et renseigner `MontantLigne` ; remise globale ; corriger l'Edit (`PrixVenteHT`, conservation de `QuantiteLivree`) ; transitions de statut contrôlées ; plafond de crédit et délai pour les clients en compte ; numérotation sûre ; vraie page Delete.
- **Effort** : M.
- **Test à la main** : créer une commande, le stock ne bouge pas ; modifier une commande partiellement livrée sans perdre la livraison ; dépasser le plafond de crédit est signalé.

### S8 – Commandes fournisseur et bons de réception
- **Objectif** : la marchandise reçue entre en stock.
- **À faire** : TVA par ligne issue de l'article ; prix recalculés côté serveur ; reliquat par ligne ; validation et annulation du BR avec entrée de stock ; statut de la commande mis à jour ; Details réel ; corriger l'injection de `IRetenueSourceService` ; numérotation sûre.
- **Effort** : G.
- **Test à la main** : créer puis valider un BR, la quantité de l'article augmente ; recevoir plus que le reliquat est refusé.

### S9 – Bons de livraison
- **Objectif** : BL complets et cohérents avec la commande.
- **À faire** : statut de la commande après livraison (partielle ou totale) ; édition et suppression d'un brouillon ; prix et TVA figés sur la ligne du BL ; contrôle du client et de la commande au POST ; gestion d'erreurs et de concurrence sur Valider et Annuler.
- **Effort** : M.
- **Test à la main** : deux livraisons partielles d'une commande, puis annulation de l'une.

### S10 – Facturation (après avis du comptable)
- **Objectif** : factures conformes.
- **Prérequis** : réponses du comptable sur FODEC, timbre, retenue à la source, numérotation et exercice. Aucune règle fiscale supposée.
- **À faire** : une seule numérotation (`NumerotationService`) ; un BL ne donne qu'une facture non annulée (index unique filtré) ; prix du BL repris ; échéance selon `Client.DelaiPaiement` ; facture directe pour le client passager ; facture mensuelle depuis les BL du client en compte ; modification du brouillon ; facture figée après validation ; FODEC et timbre selon les règles validées.
- **Effort** : G.
- **Test à la main** : facturer un BL, tenter de le facturer à nouveau (refusé), générer la facture mensuelle d'un client.

### S11 – Règlements puis échéanciers
- **Objectif** : encaissements justes.
- **À faire** : supprimer la triple déduction de la retenue à la source ; refuser un règlement sur brouillon ou facture annulée ; transaction et concurrence ; remplacer la cascade Règlement vers Facture ; échéancier et suivi des retards.
- **Effort** : G.
- **Test à la main** : deux règlements partiels puis solde ; un règlement supérieur au reste est refusé.

### S12 – PDF et mentions légales
- **Objectif** : facture imprimable et conforme.
- **À faire** : lire les paramètres de la société (nom, matricule, adresse, logo) ; mentions légales validées par le comptable ; récapitulatif de TVA par taux ; FODEC ; montant en lettres ; « net à payer » correct ; archivage du PDF figé à la validation ; formats de nombres indépendants du serveur.
- **Effort** : M.
- **Test à la main** : comparer un PDF avec un exemple validé par le comptable.

### S13 – Avoirs
- **Objectif** : corriger une facture validée sans la modifier.
- **À faire** : modèle, numérotation, lien avec la facture d'origine, retour de stock éventuel, impact sur le solde client, PDF.
- **Prérequis** : règles d'avoir validées par le comptable.
- **Effort** : G.

### S14 – Sécurité
- **Objectif** : accès contrôlé et traçable avant toute vraie donnée.
- **À faire** : authentification, rôles, `[Authorize]` sur toutes les pages, protection des paramètres fiscaux et des PDF ; journal d'audit (qui, quand, quoi) sur validations, annulations, règlements et paramètres ; seed de paramètres factices retiré ; `Migrate()` et configuration par environnement.
- **Effort** : G.

### S15 – Import Excel
- **Objectif** : reprendre les données existantes.
- **À faire** : import des articles, clients et fournisseurs avec contrôle, rapport d'erreurs et mode simulation avant écriture.
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
