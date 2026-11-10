# Lot 1 — Fondations (Sprint 1)

Périmètre : enums, entités Devise / TauxChange, enrichissement Article et MouvementStock, configuration EF Core. Aucun service métier ni migration fournis dans ce lot (volontairement).

## Fichiers créés

- `Enums/TypeMouvementStock.cs` — nouvel enum (`EntreeReception`, `SortieLivraison`, `AjustementInventaire`, `RetourClient`, `RetourFournisseur`, `TransfertInterne`).

## Fichiers modifiés

- `Models/Devise.cs` *(nouveau contenu dans ce lot)* — devise (TND/EUR/USD) + devise principale unique.
- `Models/TauxChange.cs` *(nouveau contenu dans ce lot)* — taux de conversion vers TND, `decimal(18,6)`.
- `Models/Article.cs` — ajout de `CMUP`, `GereNumeroSerie`, `DeviseAchat` (FK nullable), `DeviseAchatId`, `PrixAchatDevise`, `RowVersion`, propriété calculée `ValeurStock`. Les propriétés existantes sont conservées (incluant `Quantite`).
- `Models/MouvementStock.cs` — `Type` passe de `string` à l'enum `TypeMouvementStock`, ajout de `CoutUnitaire`, `QuantiteAvant` / `QuantiteApres` (l'ancien `StockApres` est remplacé par `QuantiteApres`, plus explicite), `SourceType` / `SourceId` pour la traçabilité, et `RowVersion`.
- `Data/ApplicationDbContext.cs` — `DbSet<Devise>`, `DbSet<TauxChange>`, configuration Fluent : `HasPrecision`, index unique `Devise.Code`, index unique filtré sur `Devise.EstDevisePrincipale`, index unique `TauxChange(DeviseId, Date)`, conversion de l'enum en `int`, seed des 3 devises TND / EUR / USD (TND = devise principale), conservation intégrale des configurations existantes.

## Commandes de migration (base de test)

Les données actuelles sont des données de test uniquement, on peut donc régénérer proprement la migration initiale :

```powershell
# 1. Supprimer l'ancienne base de test (SQL Server)
Drop-Database -ConnectionString "<votre chaîne de connexion>"   # ou via SSMS / sqlcmd

# 2. Supprimer l'historique des migrations EF Core
Remove-Item -Recurse -Force Migrations

# 3. Régénérer la migration initiale
dotnet ef migrations add Initial -o Data/Migrations

# 4. Appliquer à la base
dotnet ef database update
```

> Si la base contient déjà des données que vous voulez conserver, utilisez plutôt `dotnet ef migrations add Lot1_Devises_CMUP` puis `dotnet ef database update` sans supprimer la base ni l'historique. Une relecture manuelle de la migration Up/Down est alors recommandée (drop & recreate des index, conversion du champ `Type` etc.).

## Points d'attention

1. **Enum `MouvementStock.Type`** : le champ passe de `string` à `int` (enum stocké sous forme d'entier). Toute valeur existante en base (`"Entrée"`, `"Sortie"`, ...) ne sera pas migrée automatiquement : sur une base test, la regénération propre est la voie la plus sûre.
2. **Renommage `StockApres` → `QuantiteApres`** : la propriété est renommée pour être symétrique avec `QuantiteAvant`. Adaptez les vues Razor Pages et tous les usages existants (`@item.StockApres`, `Model.StockApres`, etc.).
3. **`Article.RowVersion` et `MouvementStock.RowVersion`** : ce sont des `byte[]` annotés `[Timestamp]`. EF Core créera une colonne `rowversion` côté SQL Server. Ne les affectez jamais manuellement.
4. **`Devise.EstDevisePrincipale`** : un index unique filtré (`HasFilter("[EstDevisePrincipale] = 1")`) empêche d'avoir deux devises principales. Pour changer la devise principale, basculez d'abord l'ancienne à `false` puis la nouvelle à `true`.
5. **`TauxChange` : précision** : taux stockés en `decimal(18,6)` (vs `decimal(18,3)` ailleurs) pour absorber les variations fines.
6. **Seed des devises** : IDs fixes (1 = TND, 2 = EUR, 3 = USD). Si vous avez déjà des seeds concurrents, harmonisez les IDs.
7. **DeviseAchat nullable** : on garde une `OnDelete(SetNull)` pour ne pas perdre l'historique d'achat d'un article si la devise est supprimée.
8. **Pas de suppression de `Categorie.cs`** : voir « Incohérences détectées » plus bas — les deux fichiers coexistent dans le dépôt d'origine et seul `Category.cs` est référencé par `ApplicationDbContext`.

## Compilation

Le SDK `dotnet` n'est pas disponible dans le sandbox actuel. Une relecture attentive a été faite pour assurer la cohérence syntaxique :

- namespaces alignés (`GestionCommerciale.Enums`, `GestionCommerciale.Models`, `GestionCommerciale.Data`) ;
- attributs `using` présents et corrects ;
- les nouvelles entités référencent les types existants (`Category`, `Fournisseur`, `ArticleFournisseur`, `MouvementStock`) déjà présents dans le projet ;
- l'enum `TypeMouvementStock` est utilisé à la fois par `MouvementStock.Type` et par la conversion EF Core (`HasConversion<int>()`).

Une compilation locale avec `dotnet build` reste recommandée pour validation finale avant `dotnet ef migrations add Initial`.

## Hypothèses

- **Le code ISO 4217 est la clé métier** pour `Devise` : index unique sur `Code` (3 caractères fixes).
- **Taux de change toujours vers TND** : le champ `Taux` exprime la conversion *vers la devise principale*. Pas de matrice N×N.
- **CMUP stocké directement sur l'article** (champ calculé mis à jour par les services métier dans un lot ultérieur).
- **`SourceType` reste une chaîne** (pas un enum) pour rester flexible (BonReception, CommandeClient, AjustementManuel, ...) — sera éventuellement promu en enum si le besoin se confirme.
- **Pas de service métier / pas de migration** dans ce lot : juste les fondations de données.
