using GestionCommerciale.Helpers;
using GestionCommerciale.Models;

namespace GestionCommerciale.Tests;

public class ClientValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567A/P/M/000")]
    [InlineData("1234567A")]
    [InlineData(" 1234567A ")]
    public void Matricule_valide_ou_vide_est_accepte(string? valeur)
        => Assert.Null(ClientValidation.ValiderMatriculeFiscale(valeur));

    [Theory]
    [InlineData("1234-567")]
    [InlineData("ABC 123")]
    [InlineData("12345678901234567890X")] // 21 caractères
    [InlineData("é123")]
    public void Matricule_invalide_est_refuse(string valeur)
        => Assert.NotNull(ClientValidation.ValiderMatriculeFiscale(valeur));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1000")]
    [InlineData(" 8000 ")]
    public void Code_postal_valide_ou_vide_est_accepte(string? valeur)
        => Assert.Null(ClientValidation.ValiderCodePostal(valeur));

    [Theory]
    [InlineData("75001")]   // 5 chiffres (format français)
    [InlineData("100")]
    [InlineData("10A0")]
    public void Code_postal_invalide_est_refuse(string valeur)
        => Assert.NotNull(ClientValidation.ValiderCodePostal(valeur));

    [Theory]
    [InlineData(0)]
    [InlineData(12.5)]
    [InlineData(100)]
    public void Remise_entre_0_et_100_est_acceptee(double valeur)
        => Assert.Null(ClientValidation.ValiderRemise((decimal)valeur));

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Remise_hors_bornes_est_refusee(double valeur)
        => Assert.NotNull(ClientValidation.ValiderRemise((decimal)valeur));

    [Fact]
    public void Plafond_et_delai_negatifs_sont_refuses_zero_est_accepte()
    {
        Assert.Null(ClientValidation.ValiderPlafondCredit(0));
        Assert.NotNull(ClientValidation.ValiderPlafondCredit(-1));
        Assert.Null(ClientValidation.ValiderDelaiPaiement(0));
        Assert.NotNull(ClientValidation.ValiderDelaiPaiement(-1));
    }

    [Fact]
    public void Valider_un_client_correct_ne_renvoie_aucune_erreur()
    {
        var client = new Client
        {
            Nom = "Test",
            MatriculeFiscale = "1234567A/P/M/000",
            CodePostal = "1000",
            RemiseDefaut = 5,
            PlafondCredit = 5000,
            DelaiPaiement = 30
        };

        Assert.Empty(ClientValidation.Valider(client));
    }

    [Fact]
    public void Valider_renvoie_une_erreur_par_champ_incorrect()
    {
        var client = new Client
        {
            Nom = "Test",
            MatriculeFiscale = "12-34",
            CodePostal = "75001",
            RemiseDefaut = 150,
            PlafondCredit = -10,
            DelaiPaiement = -5
        };

        var champs = ClientValidation.Valider(client).Select(e => e.Champ).ToList();

        Assert.Equal(
            new[] { "MatriculeFiscale", "CodePostal", "RemiseDefaut", "PlafondCredit", "DelaiPaiement" },
            champs);
    }
}
