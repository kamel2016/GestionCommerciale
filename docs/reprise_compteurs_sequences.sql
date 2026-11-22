-- Reprise des compteurs de numerotation (table SequencesDocument).
-- A REJOUER AVANT TOUT DEPLOIEMENT sur une base existante (voir la section "Procedures" de docs/ROADMAP.md).
--
-- Pour chaque type de document et chaque exercice :
--     DernierNumero = max(valeur actuelle du compteur, plus grand numero deja emis)
-- Ne DIMINUE jamais un compteur ; sans effet si les compteurs sont a jour (idempotent) ; ne touche a aucun document.
--
-- Types couverts (format TYPE-AAAA-NNNNN) :
--     BL  = BonsLivraison.Numero          FAC = Factures.Numero
--     DEV = Devis.Numero                  CC  = CommandesClient.NumeroCommande (format CC-AAAA-NNNNN uniquement)
-- Les autres numeros (CC-AAAAMM-NNNN de la page de commande, CF..., BR-...) n'utilisent pas cette table : ils sont ignores.
-- Les numeros qui ne respectent pas le format (anciens formats, saisies libres) sont ignores.
--
-- Sauvegarde COPY_ONLY obligatoire avant execution. La derniere requete affiche les compteurs crees ou mis a jour.
SET NOCOUNT ON;

WITH numeros AS (
    SELECT N'BL' AS TypeDocument, Numero AS Valeur FROM BonsLivraison
    UNION ALL SELECT N'FAC', Numero FROM Factures WHERE Numero IS NOT NULL
    UNION ALL SELECT N'DEV', Numero FROM Devis
    UNION ALL SELECT N'CC', NumeroCommande FROM CommandesClient
),
analyses AS (
    SELECT TypeDocument,
           TRY_CAST(SUBSTRING(Valeur, LEN(TypeDocument) + 2, 4) AS int)  AS Exercice,
           TRY_CAST(SUBSTRING(Valeur, LEN(TypeDocument) + 7, 10) AS int) AS Rang
    FROM numeros
    WHERE Valeur LIKE TypeDocument + N'-[0-9][0-9][0-9][0-9]-[0-9]%'
),
maxima AS (
    SELECT TypeDocument, Exercice, MAX(Rang) AS Maximum
    FROM analyses
    WHERE Exercice IS NOT NULL AND Rang IS NOT NULL
    GROUP BY TypeDocument, Exercice
)
MERGE SequencesDocument AS cible
USING maxima AS src
    ON cible.TypeDocument = src.TypeDocument AND cible.Exercice = src.Exercice
WHEN MATCHED AND cible.DernierNumero < src.Maximum THEN
    UPDATE SET DernierNumero = src.Maximum
WHEN NOT MATCHED THEN
    INSERT (TypeDocument, Exercice, DernierNumero) VALUES (src.TypeDocument, src.Exercice, src.Maximum)
OUTPUT $action AS Action, inserted.TypeDocument, inserted.Exercice, inserted.DernierNumero;
