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
        decimal TotalTTC,
        decimal MontantFodec = 0m);

    public static class CalculTotaux
    {
        /// <summary>Arrondi au millime (3 décimales), 0,0005 arrondi au-dessus.</summary>
        public static decimal Arrondir(decimal valeur)
            => Math.Round(valeur, 3, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Calcule les totaux d'un document (commande, devis, facture) SANS FODEC.
        /// La remise globale est répartie au prorata sur chaque taux de TVA.
        /// Résultats strictement identiques à ceux d'avant l'extension FODEC (voir les tests).
        /// </summary>
        /// <param name="lignes">Pour chaque ligne : montant HT (après remise ligne) et taux de TVA (ex : 19 pour 19 %).</param>
        /// <param name="tauxRemiseGlobale">Remise globale en % (ex : 10 pour 10 %).</param>
        /// <param name="timbre">Timbre fiscal (1,000 DT sur une facture, 0 sinon).</param>
        public static TotauxDocument CalculerTotaux(
            IEnumerable<(decimal MontantHT, decimal TauxTVA)> lignes,
            decimal tauxRemiseGlobale,
            decimal timbre = 0m)
            => CalculerTotaux(lignes.Select(l => (l.MontantHT, l.TauxTVA, false)), tauxRemiseGlobale, 0m, false, timbre);

        /// <summary>
        /// Calcule les totaux d'un document avec FODEC. Ordre : remise globale, puis FODEC sur le HT net des lignes marquées,
        /// puis TVA (la base de TVA inclut ou non le FODEC selon <paramref name="baseTvaInclutFodec"/>), puis timbre.
        /// Un seul mode d'arrondi : au millime, demi arrondi loin de zéro. Le FODEC et la TVA sont arrondis PAR TAUX de TVA.
        /// </summary>
        /// <param name="lignes">Pour chaque ligne : montant HT, taux de TVA et indicateur « soumise au FODEC ».</param>
        /// <param name="tauxRemiseGlobale">Remise globale en %.</param>
        /// <param name="tauxFodec">Taux du FODEC en % (appliqué aux seules lignes marquées).</param>
        /// <param name="baseTvaInclutFodec">Vrai : la TVA porte aussi sur le FODEC. Faux : elle ne porte que sur le HT net.</param>
        /// <param name="timbre">Timbre fiscal, ajouté une fois au TTC.</param>
        public static TotauxDocument CalculerTotaux(
            IEnumerable<(decimal MontantHT, decimal TauxTVA, bool SoumisFodec)> lignes,
            decimal tauxRemiseGlobale,
            decimal tauxFodec,
            bool baseTvaInclutFodec,
            decimal timbre = 0m)
        {
            // 1. Regroupement des lignes par taux de TVA (et par marquage FODEC)
            var groupes = lignes
                .GroupBy(l => (l.TauxTVA, l.SoumisFodec))
                .Select(g => new { Taux = g.Key.TauxTVA, Fodec = g.Key.SoumisFodec, BaseBrute = Arrondir(g.Sum(x => x.MontantHT)) })
                .OrderBy(g => g.Taux).ThenBy(g => g.Fodec)
                .ToList();

            decimal totalHTBrut = groupes.Sum(g => g.BaseBrute);

            // 2. Montant total de la remise globale
            decimal totalRemise = Arrondir(totalHTBrut * tauxRemiseGlobale / 100m);

            // 3. Répartition au prorata par groupe.
            //    Le dernier groupe reçoit le reste, pour que la somme tombe juste au millime.
            var nettes = new List<(decimal Taux, bool Fodec, decimal BaseNette)>();
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
                nettes.Add((g.Taux, g.Fodec, g.BaseBrute - remiseGroupe));
            }

            // 4. FODEC et TVA, par taux de TVA
            var detail = new List<LigneTVA>();
            decimal totalFodec = 0m;

            foreach (var parTaux in nettes.GroupBy(n => n.Taux).OrderBy(g => g.Key))
            {
                decimal baseNette = parTaux.Sum(n => n.BaseNette);
                decimal baseFodec = parTaux.Where(n => n.Fodec).Sum(n => n.BaseNette);
                decimal fodec = tauxFodec > 0 ? Arrondir(baseFodec * tauxFodec / 100m) : 0m;
                totalFodec += fodec;

                decimal baseTva = baseTvaInclutFodec ? baseNette + fodec : baseNette;
                decimal tva = Arrondir(baseTva * parTaux.Key / 100m);

                detail.Add(new LigneTVA(parTaux.Key, baseNette, tva));
            }

            // 5. Totaux finaux
            decimal totalHTNet = totalHTBrut - totalRemise;
            decimal totalTVA = detail.Sum(d => d.MontantTVA);
            decimal totalTTC = totalHTNet + totalFodec + totalTVA + timbre;

            return new TotauxDocument(
                totalHTBrut, totalRemise, totalHTNet,
                detail, totalTVA, timbre, totalTTC, totalFodec);
        }
    }
}
