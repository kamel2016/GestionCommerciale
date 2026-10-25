# Architecture — Gestion commerciale

> **Document vivant**  
> **Version :** 0.3  
> Dernière mise à jour : 10/10/2026 — v0.3

## 1. Contexte et périmètre

Ce document décrit l’architecture cible d’une application de gestion commerciale en **.NET 8 LTS / C# 12**. Le périmètre couvre la gestion des fournisseurs, articles/produits, catégories, clients, commandes clients, commandes fournisseurs, ainsi que le **stock** : mouvements et niveaux de stock. Le stock est inclus dans le périmètre et cette décision est validée.

Le document fixe les décisions déjà validées, les conventions communes et les choix proposés. Les éléments marqués « à trancher » restent ouverts jusqu’à validation explicite.

## 2. Stack technique

| Domaine | Choix | Statut |
|---|---|---|
| Runtime et langage | .NET 8 LTS / C# 12 | Validé |
| API HTTP | ASP.NET Core Web API avec **Controllers** (pas de Minimal APIs) | Validé |
| Accès aux données | Entity Framework Core 8, provider SQL Server | Validé |
| Base de données | SQL Server | Validé |
| Validation | FluentValidation | Validé |
| Mapping | Mapperly ou mapping manuel ; pas d’AutoMapper | Validé |
| Tests unitaires | xUnit, FluentAssertions, NSubstitute | Validé |
| Tests d’intégration | Testcontainers | Validé |
| Journalisation | Serilog | Validé |
| Contrat API | OpenAPI / Swagger | Validé |

Les versions de packages devront être alignées sur les versions compatibles avec .NET 8 et maintenues par la CI.

## 3. Style d’architecture

Le système adopte une **Clean Architecture** dans un **monolithe modulaire**. Les modules restent séparés par leurs responsabilités et leurs contrats, tout en étant déployés dans une même application au départ.

### Arborescence cible

```text
src/
├── Gestion.Domain/
├── Gestion.Application/
├── Gestion.Infrastructure/
└── Gestion.Api/

tests/
├── Gestion.Domain.Tests/
├── Gestion.Application.Tests/
└── Gestion.Api.IntegrationTests/
```

### Règle de dépendance

- `Gestion.Api` → `Gestion.Application` → `Gestion.Domain`
- `Gestion.Infrastructure` → `Gestion.Application` et `Gestion.Domain`
- `Gestion.Domain` ne référence aucun autre projet de la solution.

Les abstractions nécessaires à l’application (par exemple les interfaces de persistance) sont définies dans `Gestion.Application` ou `Gestion.Domain` selon leur nature ; leurs implémentations résident dans `Gestion.Infrastructure`.

### Dispatch applicatif

Le projet n’utilise pas MediatR. Les commandes et requêtes sont traitées par des **handlers maison**, au moyen des contrats suivants :

```csharp
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
```

L’enregistrement des handlers dans l’injection de dépendances se fera par assembly scanning ou manuellement, selon le module concerné. Les Controllers délèguent l’exécution aux handlers sans embarquer de logique métier.

## 4. Conventions

- Utiliser le **PascalCase** pour les types, propriétés et méthodes publiques.
- Préfixer les interfaces par `I` (`ICustomerRepository`, par exemple).
- Suffixer les opérations asynchrones par `Async`.
- Écrire le code, les noms de types, les propriétés, les endpoints et les identifiants en anglais (`Customer`, `PurchaseOrder`, `SalesOrder`).
- Écrire l’UI, les libellés et les messages destinés aux utilisateurs en français.
- Activer les références nullables (`<Nullable>enable</Nullable>`).
- Activer `TreatWarningsAsErrors` en configuration `Release`.
- Utiliser l’asynchronisme de bout en bout et propager un `CancellationToken` jusqu’aux opérations d’I/O.
- Ne jamais exposer les entités EF Core depuis l’API : utiliser des DTOs pour les entrées et les sorties.

### Devise, montants et quantités

- La devise de référence et l’unique devise du système est le **dinar tunisien (TND)** ; la gestion multi-devises est hors convention et hors périmètre.
- Les montants monétaires sont représentés par `decimal` et persistés en `decimal(18,3)`, avec `HasPrecision(18,3)` dans les configurations EF Core.
- Les arrondis monétaires sont effectués à trois décimales avec `MidpointRounding.AwayFromZero`.
- Les quantités de stock et de lignes sont également représentées et persistées en `decimal(18,3)` lorsque la quantité peut être fractionnaire ; elles suivent la même précision de trois décimales.
- Les calculs doivent éviter les conversions implicites vers `double` ou `float`.

### Identifiants et sécurité d’accès

- Les identifiants techniques sont des `int` générés par SQL Server avec `IDENTITY(1,1)`, configuré par `UseIdentityColumn()`.
- Les numéros métier sont distincts des identifiants techniques, par exemple `CC-2026-000123` pour une commande client, et disposent d’un index unique.
- Chaque endpoint doit vérifier l’autorisation sur la ressource effectivement demandée. L’utilisation d’un `int` prévisible crée un risque d’**IDOR** si le contrôle d’accès n’est pas réalisé au niveau de la ressource.

## 5. Modèle de domaine

### Entités principales

- `Supplier` — fournisseur.
- `Customer` — client.
- `Category` — catégorie d’articles.
- `Article` / `Product` — article commercial ; le nom canonique reste à confirmer.
- `SalesOrder` et ses lignes (`SalesOrderLine`) — commande client.
- `PurchaseOrder` et ses lignes (`PurchaseOrderLine`) — commande fournisseur.
- `StockMovement` / `MouvementStock` — mouvement de stock.
- `StockArticle` — niveau courant du stock par article.

Les agrégats, invariants et transitions seront définis dans le domaine. Les **statuts de commande sont pilotés par le Domain** : les changements de statut passent par des comportements métier explicites et ne sont pas modifiables arbitrairement par une couche de persistance ou un contrôleur.

Les référentiels (`Supplier`, `Customer`, `Category`, `Article`/`Product`) utilisent le **soft delete** lorsque la conservation de l’historique l’exige. Les requêtes applicables filtrent par défaut les éléments supprimés ; la restauration et les vues d’administration feront l’objet d’opérations explicites.

### Stock

Le modèle de stock comprend un `MouvementStock` et un `StockArticle`. Un `MouvementStock` porte au minimum un type (`Entrée`, `Sortie` ou `Ajustement`), une quantité en `decimal(18,3)`, l’article concerné et la référence du document source. `StockArticle` conserve le niveau courant par article.

Par défaut, une sortie est impossible si le stock disponible est insuffisant, sauf si un paramètre métier explicite autorise le stock négatif. Un mouvement est généré à la réception fournisseur et à la livraison client. Les règles de réservation, d’inventaire et de valorisation pourront être détaillées ultérieurement.

Les montants sont représentés par `decimal`, avec une précision définie explicitement au niveau de la persistance : `decimal(18,3)` pour les montants monétaires et `HasPrecision(18,3)` dans les configurations EF Core. Les quantités suivent également la précision `decimal(18,3)` lorsqu’elles sont pertinentes.

## 6. Persistance

- Utiliser EF Core 8 avec le provider SQL Server.
- Définir une configuration Fluent dédiée par entité via `IEntityTypeConfiguration<TEntity>` ; éviter de concentrer le mapping dans `OnModelCreating`.
- Configurer les montants et quantités concernés avec `HasPrecision(18,3)`.
- Versionner le schéma par migrations EF Core ; les migrations sont revues et exécutées selon le processus de livraison.
- Activer la concurrence optimistique avec une colonne `rowversion` et gérer explicitement les conflits de mise à jour.
- Ne pas utiliser le lazy loading. Les chargements de navigation sont explicites et maîtrisés (`Include`, projections ou requêtes dédiées).
- Préférer les projections vers DTOs pour limiter les colonnes lues et empêcher toute fuite d’entité.

## 7. API

L’API expose des ressources versionnées sous le préfixe `/api/v1/`, au moyen de **Controllers ASP.NET Core**. Les Minimal APIs ne sont pas utilisées.

Les endpoints appliquent un format standardisé pour :

- la pagination (`page`, `pageSize`, avec limites et valeurs par défaut) ;
- le tri (`sort`), sur une liste blanche de propriétés autorisées ;
- le filtrage (`filter` ou paramètres spécialisés), validé et borné.

Les erreurs HTTP sont rendues au format **ProblemDetails** (RFC 7807). FluentValidation valide les commandes et requêtes entrantes ; les erreurs de validation sont présentées sans informations techniques sensibles. OpenAPI/Swagger documente les contrats, les paramètres, les réponses et les mécanismes d’authentification.

## 8. Sécurité

La sécurité est **validée** comme exigence transversale. **La gestion des secrets est hors périmètre pour l’instant** : aucun secret, mot de passe, clé de signature ou chaîne de connexion sensible ne doit être ajouté à ce document, au dépôt ou à des exemples committés.

- Authentification par **JWT Bearer**.
- Fournisseur d’identité : ASP.NET Core Identity intégré **ou** IdP externe, choix à trancher.
- Rôles et policies : `Admin`, `Vente`, `Achat`, `Lecture`.
- `[Authorize]` par défaut ; `AllowAnonymous` uniquement par exception documentée (par exemple un endpoint de santé non sensible).
- HTTPS obligatoire et HSTS activé en production.
- CORS restreint aux origines explicitement autorisées.
- Rate limiting sur le login et les endpoints sensibles.
- Audit des actions critiques : authentification, changements de statut, suppressions/restaurations, modifications de prix ou de données de référence.
- ProblemDetails sans fuite technique : pas de stack trace, de détails SQL, de secrets ni d’informations internes en production.
- Les permissions doivent être vérifiées sur la ressource et son périmètre, pas seulement sur l’identifiant fourni par le client.

## 9. Qualité et exploitation

- Viser une couverture de code du Domain supérieure à **80 %**, en privilégiant les invariants et transitions métier.
- La CI exécute au minimum le build, les tests unitaires et d’intégration, l’analyse statique et les contrôles de qualité configurés.
- Exposer des health checks pour l’application et ses dépendances critiques, sans révéler de secrets ni de détails d’infrastructure inutiles.
- Utiliser Serilog pour des logs structurés, avec corrélation par requête/trace et propagation de l’identifiant de corrélation.
- Prévoir des niveaux de logs adaptés aux environnements et éviter les données personnelles ou commerciales non nécessaires dans les logs.

## 10. ADR

| # | Décision | Statut | Date |
|---|---|---|---|
| ADR-001 | Monolithe modulaire Clean Architecture | Proposée | 10/10/2026 |
| ADR-002 | EF Core ORM unique | Proposée | 10/10/2026 |
| ADR-003 | DTOs exposés, jamais les entités | Proposée | 10/10/2026 |
| ADR-004 | Soft delete sur référentiels | Proposée | 10/10/2026 |
| ADR-005 | Statuts de commande pilotés par le Domain | Proposée | 10/10/2026 |
| ADR-006 | Cible .NET 8 LTS / C# 12 | Acceptée | 10/10/2026 |
| ADR-007 | SQL Server | Acceptée | 10/10/2026 |
| ADR-008 | Identifiants `int` `IDENTITY` | Acceptée | 10/10/2026 |
| ADR-009 | Sécurité JWT + rôles/policies hors secrets | Acceptée | 10/10/2026 |
| ADR-010 | Handlers maison, sans MediatR | Acceptée | 10/10/2026 |
| ADR-011 | Controllers ASP.NET Core | Acceptée | 10/10/2026 |
| ADR-012 | Devise unique TND `decimal(18,3)` | Acceptée | 10/10/2026 |
| ADR-013 | Stock dans le périmètre (mouvements + niveaux) | Acceptée | 10/10/2026 |

> **Note :** une décision au statut **Acceptée** ne change pas sans accord explicite de l’utilisateur.

## 11. Questions ouvertes

1. ASP.NET Core Identity intégré vs IdP externe ?
2. TVA : taux par article/client ? FODEC et timbre fiscal tunisien à considérer ?
3. Réceptions partielles : quel niveau de détail et quelles règles de suivi ?
4. Facturation interne ou non ?
5. Multi-tenant ou instance unique ?
6. Front-end : Blazor, Angular, React ou API seule ?

## 12. Journal des modifications

| Version | Date | Modification |
|---|---|---|
| v0.1 | 10/10/2026 | Création |
| v0.2 | 10/10/2026 | .NET 8, SQL Server, identifiants `int`, sécurité hors secrets validés (ADR-006 à ADR-009). |
| v0.3 | 10/10/2026 | Validation du périmètre stock (mouvements et niveaux), stack Controllers sans Minimal APIs, handlers maison sans MediatR, devise unique TND en `decimal(18,3)`, conventions de quantités et ajout des ADR-010 à ADR-013 ; questions ouvertes resserrées. |

### Fichiers CSS

- `wwwroot/css/site.css` - Styles globaux
- `wwwroot/css/articles.css` - Styles pour la gestion des articles
- `wwwroot/css/clients-form.css` - Styles du formulaire clients (Create/Edit)   

### Fournisseurs
- Pages : `Pages/Fournisseurs/` (Index, Create, Edit, Details, Delete)
- CSS : `wwwroot/css/fournisseurs-index.css`, `fournisseurs-form.css`
- Champs spécifiques : MatriculeFiscale, Gouvernorat (contexte tunisien)