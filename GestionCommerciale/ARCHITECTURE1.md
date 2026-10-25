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