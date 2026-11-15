namespace GestionCommerciale.Helpers
{
    /// <summary>Détail de la TVA pour un taux donné.</summary>
    public record LigneTVA(decimal TauxTVA, decimal BaseHT, decimal MontantTVA);

    /// <summary>Résultat complet du calcul des totaux d'un document.</summary>
    public record TotauxDocument(
        decimal TotalHTBrut,
        decimal MontantRemiseGlobale,
        decimal TotalHTNet,
        IReadOnlyList<LigneTVA> DetailTVA,
        decimal TotalTVA,
        decimal Timbre,
        decimal TotalTTC);

    public static class CalculTotaux
    {
        /// <summary>Arrondi au millime (3 décimales), 0,0005 arrondi au-dessus.</summary>
        public static decimal Arrondir(decimal valeur)
            => Math.Round(valeur, 3, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Calcule les totaux d'un document (commande, devis, facture).
        /// La remise globale est répartie au prorata sur chaque taux de TVA.
        /// </summary>
        /// <param name="lignes">Pour chaque ligne : montant HT (après remise ligne) et taux de TVA (ex : 19 pour 19 %).</param>
        /// <param name="tauxRemiseGlobale">Remise globale en % (ex : 10 pour 10 %).</param>
        /// <param name="timbre">Timbre fiscal (1,000 DT sur une facture, 0 sinon).</param>
        public static TotauxDocument CalculerTotaux(
            IEnumerable<(decimal MontantHT, decimal TauxTVA)> lignes,
            decimal tauxRemiseGlobale,
            decimal timbre = 0m)
        {
            // 1. Regroupement des lignes par taux de TVA
            var groupes = lignes
                .GroupBy(l => l.TauxTVA)
                .Select(g => new { Taux = g.Key, BaseBrute = Arrondir(g.Sum(x => x.MontantHT)) })
                .OrderBy(g => g.Taux)
                .ToList();

            decimal totalHTBrut = groupes.Sum(g => g.BaseBrute);

            // 2. Montant total de la remise globale
            decimal totalRemise = Arrondir(totalHTBrut * tauxRemiseGlobale / 100m);

            // 3. Répartition au prorata par taux.
            //    Le dernier groupe reçoit le reste, pour que la somme tombe juste au millime.
            var detail = new List<LigneTVA>();
            decimal remiseDejaRepartie = 0m;

            for (int i = 0; i < groupes.Count; i++)
            {
                var g = groupes[i];
                decimal remiseGroupe;

                if (i == groupes.Count - 1)
                    remiseGroupe = totalRemise - remiseDejaRepartie;
                else
                    remiseGroupe = totalHTBrut == 0 ? 0 : Arrondir(totalRemise * g.BaseBrute / totalHTBrut);

                remiseDejaRepartie += remiseGroupe;

                decimal baseNette = g.BaseBrute - remiseGroupe;
                decimal tva = Arrondir(baseNette * g.Taux / 100m);

                detail.Add(new LigneTVA(g.Taux, baseNette, tva));
            }

            // 4. Totaux finaux
            decimal totalHTNet = totalHTBrut - totalRemise;
            decimal totalTVA = detail.Sum(d => d.MontantTVA);
            decimal totalTTC = totalHTNet + totalTVA + timbre;

            return new TotauxDocument(
                totalHTBrut, totalRemise, totalHTNet,
                detail, totalTVA, timbre, totalTTC);
        }
    }
}