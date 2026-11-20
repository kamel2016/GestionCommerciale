using GestionCommerciale.Helpers;
using GestionCommerciale.Models;

namespace GestionCommerciale.Tests;

/// <summary>
/// S7bis : règle de calcul unique des montants (HT par ligne arrondi au millime, TVA par taux sur la base nette, remise globale en %
/// répartie au prorata, arrondi AwayFromZero). TOUS les résultats attendus ci-dessous ont été calculés À LA MAIN (détail en commentaire),
/// indépendamment du code testé. Les cas marqués « ToEven ≠ AwayFromZero » échoueraient avec l'arrondi bancaire de Math.Round par défaut.
/// </summary>
public class CalculTotauxTests
{
    // ------------------------------------------------------------------ Arrondir (3 décimales, 0,0005 arrondi au-dessus)

    [Theory]
    [InlineData("0.0005", "0.001")]     // ToEven donnerait 0.000
    [InlineData("0.0015", "0.002")]
    [InlineData("0.0025", "0.003")]     // ToEven donnerait 0.002
    [InlineData("1.2345", "1.235")]     // ToEven donnerait 1.234
    [InlineData("2.0005", "2.001")]     // ToEven donnerait 2.000
    [InlineData("-0.0005", "-0.001")]   // « loin de zéro » vaut aussi pour les négatifs
    [InlineData("0.0004", "0.000")]
    [InlineData("0.0006", "0.001")]
    [InlineData("1.2344", "1.234")]
    [InlineData("12.345", "12.345")]    // déjà au millime : inchangé
    public void Arrondir_au_millime_avec_demi_arrondi_loin_de_zero(string valeur, string attendu)
        => Assert.Equal(decimal.Parse(attendu, System.Globalization.CultureInfo.InvariantCulture),
                        CalculTotaux.Arrondir(decimal.Parse(valeur, System.Globalization.CultureInfo.InvariantCulture)));

    // ------------------------------------------------------------------ Montant d'une ligne

    [Theory]
    [InlineData(3, "0.335", "1.005")]                    // 3 x 0,335 = 1,005
    [InlineData(1, "0.0005", "0.001")]                   // ToEven : 0.000
    [InlineData(3, "0.1675", "0.503")]                   // 0,5025 ; ToEven : 0.502
    [InlineData(1, "12.345", "12.345")]                  // quantité 1
    [InlineData(0, "12.345", "0.000")]
    [InlineData(1_000_000, "999.999", "999999000.000")]  // grande quantité : 999 999 000
    public void Le_montant_de_ligne_est_quantite_fois_prix_arrondi_au_millime(int quantite, string prix, string attendu)
    {
        var ligne = new LigneCommandeClient { Quantite = quantite, PrixUnitaire = decimal.Parse(prix, System.Globalization.CultureInfo.InvariantCulture) };

        ligne.CalculerMontant();

        Assert.Equal(decimal.Parse(attendu, System.Globalization.CultureInfo.InvariantCulture), ligne.MontantLigne);
    }

    // ------------------------------------------------------------------ Totaux d'un document

    private static (decimal, decimal) L(string ht, decimal taux) => (decimal.Parse(ht, System.Globalization.CultureInfo.InvariantCulture), taux);

    [Fact]
    public void Une_commande_vide_donne_des_totaux_a_zero()
    {
        var t = CalculTotaux.CalculerTotaux(Array.Empty<(decimal, decimal)>(), 0m);

        Assert.Equal(0m, t.TotalHTBrut);
        Assert.Equal(0m, t.MontantRemiseGlobale);
        Assert.Equal(0m, t.TotalHTNet);
        Assert.Equal(0m, t.TotalTVA);
        Assert.Equal(0m, t.TotalTTC);
        Assert.Empty(t.DetailTVA);
    }

    [Fact]
    public void Le_timbre_est_ajoute_au_ttc_sans_lignes()
        => Assert.Equal(1.000m, CalculTotaux.CalculerTotaux(Array.Empty<(decimal, decimal)>(), 0m, timbre: 1.000m).TotalTTC);

    [Fact]
    public void Un_seul_taux()
    {
        // 100,000 à 19 % : TVA = 19,000 ; TTC = 119,000
        var t = CalculTotaux.CalculerTotaux(new[] { L("100.000", 19m) }, 0m);

        Assert.Equal(100.000m, t.TotalHTNet);
        Assert.Equal(19.000m, t.TotalTVA);
        Assert.Equal(119.000m, t.TotalTTC);
    }

    [Fact]
    public void Trois_fois_0_335_a_20_pour_cent()
    {
        // HT 1,005 ; TVA = 1,005 x 0,20 = 0,201 exactement ; TTC = 1,206
        var t = CalculTotaux.CalculerTotaux(new[] { L("1.005", 20m) }, 0m);

        Assert.Equal(1.005m, t.TotalHTNet);
        Assert.Equal(0.201m, t.TotalTVA);
        Assert.Equal(1.206m, t.TotalTTC);
    }

    [Fact]
    public void Plusieurs_taux_dans_une_commande_sont_regroupes_et_tries_par_taux()
    {
        // 0 % : 1,000 -> 0 ; 7 % : 10,000 -> 0,700 ; 19 % : 20,000 + 5,000 = 25,000 -> 4,750
        // TVA 5,450 ; HT 36,000 ; TTC 41,450
        var t = CalculTotaux.CalculerTotaux(new[] { L("10.000", 7m), L("20.000", 19m), L("5.000", 19m), L("1.000", 0m) }, 0m);

        Assert.Equal(new[] { 0m, 7m, 19m }, t.DetailTVA.Select(d => d.TauxTVA).ToArray());
        Assert.Equal(new[] { 1.000m, 10.000m, 25.000m }, t.DetailTVA.Select(d => d.BaseHT).ToArray());
        Assert.Equal(new[] { 0.000m, 0.700m, 4.750m }, t.DetailTVA.Select(d => d.MontantTVA).ToArray());
        Assert.Equal(5.450m, t.TotalTVA);
        Assert.Equal(36.000m, t.TotalHTNet);
        Assert.Equal(41.450m, t.TotalTTC);
    }

    [Fact]
    public void La_tva_au_demi_millime_est_arrondie_au_dessus_ToEven_donnerait_0_010()
    {
        // 0,150 x 7 % = 0,0105 -> 0,011 (ToEven : 0,010) ; TTC = 0,161
        var t = CalculTotaux.CalculerTotaux(new[] { L("0.150", 7m) }, 0m);

        Assert.Equal(0.011m, t.TotalTVA);
        Assert.Equal(0.161m, t.TotalTTC);
    }

    [Fact]
    public void Une_tva_de_0_0005_est_arrondie_a_0_001_ToEven_donnerait_0()
    {
        // 0,050 x 1 % = 0,0005 -> 0,001 ; TTC 0,051
        var t = CalculTotaux.CalculerTotaux(new[] { L("0.050", 1m) }, 0m);

        Assert.Equal(0.001m, t.TotalTVA);
        Assert.Equal(0.051m, t.TotalTTC);
    }

    [Fact]
    public void Les_lignes_d_un_meme_taux_sont_additionnees_avant_d_arrondir_la_tva()
    {
        // 0,400 + 0,350 = 0,750 à 7 % : 0,0525 -> 0,053 (ToEven : 0,052) ; TTC 0,803
        var t = CalculTotaux.CalculerTotaux(new[] { L("0.400", 7m), L("0.350", 7m) }, 0m);

        Assert.Equal(0.053m, t.TotalTVA);
        Assert.Equal(0.803m, t.TotalTTC);
    }

    [Fact]
    public void Remise_globale_exacte()
    {
        // 10,000 à 19 %, remise 10 % : remise 1,000 ; net 9,000 ; TVA 1,710 ; TTC 10,710
        var t = CalculTotaux.CalculerTotaux(new[] { L("10.000", 19m) }, 10m);

        Assert.Equal(1.000m, t.MontantRemiseGlobale);
        Assert.Equal(9.000m, t.TotalHTNet);
        Assert.Equal(1.710m, t.TotalTVA);
        Assert.Equal(10.710m, t.TotalTTC);
    }

    [Fact]
    public void La_remise_au_demi_millime_est_arrondie_au_dessus_ToEven_donnerait_0_100()
    {
        // 1,005 à 20 %, remise 10 % : 0,1005 -> 0,101 ; net 0,904 ; TVA 0,904 x 0,20 = 0,1808 -> 0,181 ; TTC 1,085
        var t = CalculTotaux.CalculerTotaux(new[] { L("1.005", 20m) }, 10m);

        Assert.Equal(0.101m, t.MontantRemiseGlobale);
        Assert.Equal(0.904m, t.TotalHTNet);
        Assert.Equal(0.181m, t.TotalTVA);
        Assert.Equal(1.085m, t.TotalTTC);
    }

    [Fact]
    public void Remise_globale_repartie_au_prorata_le_dernier_taux_recoit_le_reste()
    {
        // 3 x 1,000 aux taux 7, 13, 19 ; brut 3,000 ; remise 3,333 % : 3,000 x 3,333 / 100 = 0,09999 -> 0,100
        // 7 % : 0,100 x 1/3 = 0,0333 -> 0,033 ; 13 % : 0,033 ; 19 % (dernier) : 0,100 - 0,066 = 0,034
        // bases nettes 0,967 / 0,967 / 0,966 ; TVA 0,06769 -> 0,068 ; 0,12571 -> 0,126 ; 0,18354 -> 0,184 ; TVA 0,378
        // HT net 2,900 ; TTC 3,278
        var t = CalculTotaux.CalculerTotaux(new[] { L("1.000", 7m), L("1.000", 13m), L("1.000", 19m) }, 3.333m);

        Assert.Equal(0.100m, t.MontantRemiseGlobale);
        Assert.Equal(new[] { 0.967m, 0.967m, 0.966m }, t.DetailTVA.Select(d => d.BaseHT).ToArray());
        Assert.Equal(new[] { 0.068m, 0.126m, 0.184m }, t.DetailTVA.Select(d => d.MontantTVA).ToArray());
        Assert.Equal(0.378m, t.TotalTVA);
        Assert.Equal(2.900m, t.TotalHTNet);
        Assert.Equal(3.278m, t.TotalTTC);
    }

    [Fact]
    public void Remise_de_100_pour_cent()
    {
        var t = CalculTotaux.CalculerTotaux(new[] { L("10.000", 19m) }, 100m);

        Assert.Equal(10.000m, t.MontantRemiseGlobale);
        Assert.Equal(0m, t.TotalHTNet);
        Assert.Equal(0m, t.TotalTVA);
        Assert.Equal(0m, t.TotalTTC);
    }

    [Fact]
    public void Valeurs_elevees_restent_exactes()
    {
        // 999 999 000,000 à 19 % : TVA 189 999 810,000 ; TTC 1 189 998 810,000
        var t = CalculTotaux.CalculerTotaux(new[] { L("999999000.000", 19m) }, 0m);

        Assert.Equal(189999810.000m, t.TotalTVA);
        Assert.Equal(1189998810.000m, t.TotalTTC);
    }

    // ------------------------------------------------------------------ CommandeClient.RecalculerTotaux

    [Fact]
    public void RecalculerTotaux_renseigne_les_montants_de_ligne_et_les_totaux_de_la_commande()
    {
        // ligne 1 : 3 x 0,335 à 20 % = 1,005 ; ligne 2 : 2 x 10,500 à 19 % = 21,000
        // 19 % : 21,000 -> 3,990 ; 20 % : 1,005 -> 0,201 ; HT 22,005 ; TVA 4,191 ; TTC 26,196
        var commande = new CommandeClient();
        commande.LignesCommande.Add(new LigneCommandeClient { Quantite = 3, PrixUnitaire = 0.335m, TauxTVA = 20m });
        commande.LignesCommande.Add(new LigneCommandeClient { Quantite = 2, PrixUnitaire = 10.500m, TauxTVA = 19m });

        commande.RecalculerTotaux();

        Assert.Equal(new[] { 1.005m, 21.000m }, commande.LignesCommande.Select(l => l.MontantLigne).ToArray());
        Assert.Equal(22.005m, commande.MontantHTBrut);
        Assert.Equal(0m, commande.MontantRemiseGlobale);
        Assert.Equal(22.005m, commande.MontantHT);
        Assert.Equal(4.191m, commande.MontantTVA);
        Assert.Equal(26.196m, commande.MontantTTC);
    }

    [Fact]
    public void RecalculerTotaux_avec_remise_globale()
    {
        // mêmes lignes, remise 10 % : remise 22,005 x 10 % = 2,2005 -> 2,201
        // 19 % (base 21,000) : 2,201 x 21,000 / 22,005 = 2,10047 -> 2,100 ; 20 % (dernier) : 2,201 - 2,100 = 0,101
        // bases nettes 18,900 / 0,904 ; TVA 3,591 / 0,1808 -> 0,181 ; TVA 3,772 ; HT net 19,804 ; TTC 23,576
        var commande = new CommandeClient { TauxRemiseGlobale = 10m };
        commande.LignesCommande.Add(new LigneCommandeClient { Quantite = 3, PrixUnitaire = 0.335m, TauxTVA = 20m });
        commande.LignesCommande.Add(new LigneCommandeClient { Quantite = 2, PrixUnitaire = 10.500m, TauxTVA = 19m });

        commande.RecalculerTotaux();

        Assert.Equal(22.005m, commande.MontantHTBrut);
        Assert.Equal(2.201m, commande.MontantRemiseGlobale);
        Assert.Equal(19.804m, commande.MontantHT);
        Assert.Equal(3.772m, commande.MontantTVA);
        Assert.Equal(23.576m, commande.MontantTTC);
    }

    [Fact]
    public void RecalculerTotaux_sur_une_commande_vide_met_tout_a_zero()
    {
        var commande = new CommandeClient { MontantHT = 5m, MontantTVA = 1m, MontantTTC = 6m, MontantHTBrut = 5m };

        commande.RecalculerTotaux();

        Assert.Equal(0m, commande.MontantHT);
        Assert.Equal(0m, commande.MontantTVA);
        Assert.Equal(0m, commande.MontantTTC);
        Assert.Equal(0m, commande.MontantHTBrut);
    }

    // ================================================================== S10 : extension FODEC (articles marqués) et base de TVA pilotée par paramètre

    private static (decimal, decimal, bool) F(string ht, decimal taux, bool fodec) => (decimal.Parse(ht, System.Globalization.CultureInfo.InvariantCulture), taux, fodec);

    [Fact]
    public void Fodec_sur_une_ligne_marquee_base_de_tva_SANS_fodec()
    {
        // 100,000 à 19 %, marquée FODEC, taux FODEC 1 % : FODEC 1,000 ; TVA = 100,000 x 19 % = 19,000 ; TTC = 100 + 1 + 19 = 120,000
        var t = CalculTotaux.CalculerTotaux(new[] { F("100.000", 19m, true) }, 0m, tauxFodec: 1m, baseTvaInclutFodec: false);

        Assert.Equal(1.000m, t.MontantFodec);
        Assert.Equal(19.000m, t.TotalTVA);
        Assert.Equal(100.000m, t.TotalHTNet);
        Assert.Equal(120.000m, t.TotalTTC);
    }

    [Fact]
    public void Fodec_sur_une_ligne_marquee_base_de_tva_AVEC_fodec()
    {
        // même ligne : TVA = (100,000 + 1,000) x 19 % = 19,190 ; TTC = 100 + 1 + 19,190 = 120,190
        var t = CalculTotaux.CalculerTotaux(new[] { F("100.000", 19m, true) }, 0m, tauxFodec: 1m, baseTvaInclutFodec: true);

        Assert.Equal(1.000m, t.MontantFodec);
        Assert.Equal(19.190m, t.TotalTVA);
        Assert.Equal(120.190m, t.TotalTTC);
    }

    [Fact]
    public void Le_timbre_s_ajoute_apres_la_tva_et_le_fodec()
        => Assert.Equal(121.000m, CalculTotaux.CalculerTotaux(new[] { F("100.000", 19m, true) }, 0m, 1m, false, timbre: 1.000m).TotalTTC);

    [Fact]
    public void Seules_les_lignes_marquees_portent_le_fodec_plusieurs_taux()
    {
        // (100,000 ; 19 % ; FODEC), (50,000 ; 19 %), (20,000 ; 7 % ; FODEC) ; FODEC 1 %
        // FODEC par taux : 19 % -> 1,000 ; 7 % -> 0,200 ; total 1,200 ; HT net 170,000
        // base de TVA hors FODEC : 19 % : 150 x 19 % = 28,500 ; 7 % : 20 x 7 % = 1,400 ; TVA 29,900 ; TTC = 170 + 1,2 + 29,9 = 201,100
        var lignes = new[] { F("100.000", 19m, true), F("50.000", 19m, false), F("20.000", 7m, true) };
        var sans = CalculTotaux.CalculerTotaux(lignes, 0m, 1m, false);

        Assert.Equal(1.200m, sans.MontantFodec);
        Assert.Equal(29.900m, sans.TotalTVA);
        Assert.Equal(170.000m, sans.TotalHTNet);
        Assert.Equal(201.100m, sans.TotalTTC);

        // base de TVA incluant le FODEC : 19 % : (150 + 1,000) x 19 % = 28,690 ; 7 % : (20 + 0,200) x 7 % = 1,414 ; TVA 30,104 ; TTC = 170 + 1,2 + 30,104 = 201,304
        var avec = CalculTotaux.CalculerTotaux(lignes, 0m, 1m, true);
        Assert.Equal(1.200m, avec.MontantFodec);
        Assert.Equal(30.104m, avec.TotalTVA);
        Assert.Equal(201.304m, avec.TotalTTC);
    }

    [Fact]
    public void La_remise_globale_est_appliquee_avant_le_fodec_sur_le_ht_net()
    {
        // (60,000 ; 19 % ; FODEC) et (40,000 ; 19 %), remise 10 % : remise 10,000 ; groupes triés (non marqué, puis marqué)
        // non marqué : 10 x 40/100 = 4,000 -> net 36,000 ; marqué (dernier) : 10 - 4 = 6,000 -> net 54,000
        // FODEC 1 % de 54,000 = 0,540 ; TVA hors FODEC = 90,000 x 19 % = 17,100 ; TTC = 90 + 0,54 + 17,1 = 107,640
        var lignes = new[] { F("60.000", 19m, true), F("40.000", 19m, false) };
        var sans = CalculTotaux.CalculerTotaux(lignes, 10m, 1m, false);

        Assert.Equal(10.000m, sans.MontantRemiseGlobale);
        Assert.Equal(90.000m, sans.TotalHTNet);
        Assert.Equal(0.540m, sans.MontantFodec);
        Assert.Equal(17.100m, sans.TotalTVA);
        Assert.Equal(107.640m, sans.TotalTTC);

        // avec FODEC dans la base de TVA : (90,000 + 0,540) x 19 % = 17,2026 -> 17,203 ; TTC = 90 + 0,54 + 17,203 = 107,743
        var avec = CalculTotaux.CalculerTotaux(lignes, 10m, 1m, true);
        Assert.Equal(17.203m, avec.TotalTVA);
        Assert.Equal(107.743m, avec.TotalTTC);
    }

    [Fact]
    public void Le_fodec_au_demi_millime_est_arrondi_au_dessus_ToEven_donnerait_0()
    {
        // 0,050 à 0 %, FODEC 1 % : 0,0005 -> 0,001 ; TTC 0,051
        var t = CalculTotaux.CalculerTotaux(new[] { F("0.050", 0m, true) }, 0m, 1m, false);

        Assert.Equal(0.001m, t.MontantFodec);
        Assert.Equal(0.051m, t.TotalTTC);
    }

    [Fact]
    public void Un_taux_fodec_nul_ou_aucune_ligne_marquee_donne_un_fodec_nul()
    {
        Assert.Equal(0m, CalculTotaux.CalculerTotaux(new[] { F("100.000", 19m, true) }, 0m, 0m, false).MontantFodec);
        Assert.Equal(0m, CalculTotaux.CalculerTotaux(new[] { F("100.000", 19m, false) }, 0m, 1m, false).MontantFodec);
    }

    [Fact]
    public void Les_resultats_de_commande_sont_identiques_avant_et_apres_l_extension()
    {
        // L'ancienne signature (2 valeurs par ligne) et la nouvelle (ligne non marquée, FODEC nul) donnent les MÊMES totaux,
        // sur tous les cas chiffrés à la main de cette classe (taux unique, plusieurs taux, remises, demi-millimes, vide, grandes valeurs).
        var jeux = new (string Nom, (decimal, decimal)[] Lignes, decimal Remise, decimal Timbre)[]
        {
            ("vide", Array.Empty<(decimal, decimal)>(), 0m, 0m),
            ("vide + timbre", Array.Empty<(decimal, decimal)>(), 0m, 1.000m),
            ("un taux", new[] { L("100.000", 19m) }, 0m, 0m),
            ("3 x 0,335 a 20 %", new[] { L("1.005", 20m) }, 0m, 0m),
            ("plusieurs taux", new[] { L("10.000", 7m), L("20.000", 19m), L("5.000", 19m), L("1.000", 0m) }, 0m, 0m),
            ("tva demi-millime", new[] { L("0.150", 7m) }, 0m, 0m),
            ("tva 0,0005", new[] { L("0.050", 1m) }, 0m, 0m),
            ("meme taux additionne", new[] { L("0.400", 7m), L("0.350", 7m) }, 0m, 0m),
            ("remise exacte", new[] { L("10.000", 19m) }, 10m, 0m),
            ("remise demi-millime", new[] { L("1.005", 20m) }, 10m, 0m),
            ("remise avec reste", new[] { L("1.000", 7m), L("1.000", 13m), L("1.000", 19m) }, 3.333m, 0m),
            ("remise 100 %", new[] { L("10.000", 19m) }, 100m, 0m),
            ("grande valeur", new[] { L("999999000.000", 19m) }, 0m, 0m),
            ("remise + timbre", new[] { L("22.005", 19m), L("1.005", 20m) }, 10m, 1.000m),
        };

        foreach (var (nom, lignes, remise, timbre) in jeux)
        {
            var ancien = CalculTotaux.CalculerTotaux(lignes, remise, timbre);
            var nouveau = CalculTotaux.CalculerTotaux(lignes.Select(l => (l.Item1, l.Item2, false)), remise, 0m, false, timbre);

            Assert.True(ancien.TotalHTBrut == nouveau.TotalHTBrut, nom + " : brut");
            Assert.True(ancien.MontantRemiseGlobale == nouveau.MontantRemiseGlobale, nom + " : remise");
            Assert.True(ancien.TotalHTNet == nouveau.TotalHTNet, nom + " : HT net");
            Assert.True(ancien.TotalTVA == nouveau.TotalTVA, nom + " : TVA");
            Assert.True(ancien.TotalTTC == nouveau.TotalTTC, nom + " : TTC");
            Assert.True(ancien.Timbre == nouveau.Timbre, nom + " : timbre");
            Assert.Equal(0m, ancien.MontantFodec);
            Assert.Equal(0m, nouveau.MontantFodec);
            Assert.Equal(ancien.DetailTVA, nouveau.DetailTVA);
        }
    }
}
