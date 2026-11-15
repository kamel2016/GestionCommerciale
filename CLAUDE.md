\# GestionCommerciale



Application de gestion commerciale pour une société tunisienne qui revend

en l'état des produits importés.

Stack : ASP.NET Core Razor Pages, EF Core, SQL Server.



\## Règles de travail (à respecter à chaque session)

\- Réponds en français, simplement. Je ne suis pas développeur expert.

\- Travaille sur une branche git dédiée, un commit par étape.

\- Une session = un module. Ne touche pas aux autres modules.

\- Avant de coder : résume ton plan en quelques lignes.

\- Après chaque modification : `dotnet build` puis `dotnet test`. Corrige avant de rendre la main.

\- Ne lance JAMAIS `dotnet ef database update` sans me montrer le script

&#x20; de migration et obtenir mon accord. Jamais sur une base de production.

\- Ne supprime aucune donnée, migration existante ou fichier sans me le demander.

\- Ne devine pas une règle métier ou fiscale : pose-moi la question.

\- Termine chaque tâche par : ce qui est fait, ce qui reste, les risques, les tests à faire à la main.



\## Commandes

\- Build : `dotnet build`

\- Tests : `dotnet test` (À VÉRIFIER : existe-t-il un projet de tests ?)

\- Base de dev : À COMPLÉTER (nom de la base, jamais de production)



\## Conventions

\- Entités dans Models/, enums dans Enums/, pages dans Pages/<Module>/.

\- Pages Razor : travailler directement sur les entités, pas de ViewModel

&#x20; fictif. Toute page de modification doit réellement enregistrer en base.

\- Noms de propriétés existants à respecter : MatriculeFiscale, RemiseDefaut,

&#x20; PlafondCredit, EstActif, MontantLigne, TauxTVA.

\- Montants : decimal. TauxTVA : decimal(5,2).



\## Règles métier

\- Deux types de clients (enum ModeFacturation) :

&#x20; - client en compte : bon de livraison (BL) puis facture mensuelle

&#x20; - client passager : facture directe

\- Client.Code obligatoire, unique, format CLI-00001, généré à la création,

&#x20; en lecture seule ensuite.

\- Le TauxTVA d'une ligne est copié depuis l'article à la création de la

&#x20; ligne et ne change plus (taux historique).

\- Le montant HT d'une ligne s'appelle MontantLigne. CommandeClient a une remise globale.

\- Plafond de crédit et délai de paiement du client : à contrôler pour les clients en compte.

\- Timbre fiscal, numérotation des factures, mentions légales tunisiennes :

&#x20; À VALIDER avec le comptable avant implémentation. Ne rien supposer.



\## État

Faits : Clients, Articles, Fournisseurs, Commandes, Factures, Bons de livraison, Stock

(à confirmer par audit).



\## Plan restant (dans cet ordre)

0\. Audit en lecture seule : compilation, migrations, CRUD qui enregistre vraiment, dette technique bloquante

1\. Historique stock (mouvements d'entrée et de sortie)

2\. Facturation (directe, mensuelle depuis les BL, numérotation, TVA, timbre)

3\. Règlements, puis échéanciers

4\. Avoirs

5\. Sécurité (authentification, rôles, journal des actions), avant toute vraie donnée

6\. Import Excel

7\. Reporting

8\. Audit final et optimisation

(Sauvegarde SQL Server : tâche d'exploitation, hors développement.)

