# EF Core 8 — migrations SQL Server

1. Ajouter les packages EF Core SQL Server/Tools en version 8.x.
2. Mettre la chaîne `DefaultConnection` dans `appsettings.json` ou un secret d’environnement (ne pas y placer de mot de passe en dépôt).
3. Depuis le dossier du projet :

```bash
dotnet tool install --global dotnet-ef --version 8.*   # une seule fois
dotnet ef migrations add InitialArticles
dotnet ef database update
```

Pour une nouvelle évolution : `dotnet ef migrations add NomDeLaMigration`, puis `dotnet ef database update`.
Si le DbContext ou le projet est dans un autre dossier, utiliser `--project` et `--startup-project`.
