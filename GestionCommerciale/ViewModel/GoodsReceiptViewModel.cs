using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.ViewModel
{
    /// <summary>
    /// ViewModel pour la gestion des Bons de Réception
    /// </summary>
    public class GoodsReceiptViewModel
    {
        /// <summary>
        /// Identifiant unique du bon de réception
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Numéro interne du bon de réception
        /// </summary>
        [Required(ErrorMessage = "Le numéro interne est requis")]
        [StringLength(50, ErrorMessage = "Le numéro ne doit pas dépasser 50 caractères")]
        public string InternalNumber { get; set; } = string.Empty;

        /// <summary>
        /// Code du fournisseur
        /// </summary>
        [Required(ErrorMessage = "Le code fournisseur est requis")]
        [StringLength(50, ErrorMessage = "Le code fournisseur ne doit pas dépasser 50 caractères")]
        public string SupplierCode { get; set; } = string.Empty;

        /// <summary>
        /// Nom du fournisseur
        /// </summary>
        [Required(ErrorMessage = "Le nom du fournisseur est requis")]
        [StringLength(255, ErrorMessage = "Le nom ne doit pas dépasser 255 caractères")]
        public string SupplierName { get; set; } = string.Empty;

        /// <summary>
        /// Référence de la commande fournisseur
        /// </summary>
        [StringLength(50, ErrorMessage = "La référence ne doit pas dépasser 50 caractères")]
        public string SupplierReference { get; set; } = string.Empty;

        /// <summary>
        /// Date de saisie du bon
        /// </summary>
        [Required(ErrorMessage = "La date de saisie est requise")]
        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Heure de réception
        /// </summary>
        public TimeSpan ReceiptTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Date de la facture fournisseur
        /// </summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>
        /// Date de commande (du bon de commande)
        /// </summary>
        public DateTime? OrderDate { get; set; }

        /// <summary>
        /// Numéro de commande fournisseur
        /// </summary>
        [StringLength(50, ErrorMessage = "Le numéro de commande ne doit pas dépasser 50 caractères")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Montant HT (Hors Taxes)
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant HT doit être positif")]
        public decimal AmountExcludingTax { get; set; }

        /// <summary>
        /// Montant HT à recevoir
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal AmountToReceive { get; set; }

        /// <summary>
        /// Montant réglé
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant réglé doit être positif")]
        public decimal AmountPaid { get; set; }

        /// <summary>
        /// Montant régularisé
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal AmountRegularized { get; set; }

        /// <summary>
        /// Reste à regler
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal AmountRemaining { get; set; }

        /// <summary>
        /// Montant imputé
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal AmountCharged { get; set; }

        /// <summary>
        /// Montant TTC (Toutes Taxes Comprises)
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant TTC doit être positif")]
        public decimal AmountIncludingTax { get; set; }

        /// <summary>
        /// Avoir (remise, retour, etc.)
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal CreditNote { get; set; }

        /// <summary>
        /// Taux de TVA à appliquer
        /// </summary>
        [Range(0, 100, ErrorMessage = "Le taux de TVA doit être entre 0 et 100")]
        public decimal VatRate { get; set; } = 20;

        /// <summary>
        /// Indique si le bon est assujetti à la TVA
        /// </summary>
        public bool IsVatSubject { get; set; } = true;

        /// <summary>
        /// Devise du document
        /// </summary>
        [StringLength(3, ErrorMessage = "La devise ne doit pas dépasser 3 caractères")]
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Taux de change
        /// </summary>
        [Range(0.01, 999.99, ErrorMessage = "Le taux de change doit être valide")]
        public decimal ExchangeRate { get; set; } = 1;

        /// <summary>
        /// Statut du bon (Brouillon, Validé, Liquidé, Annulé)
        /// </summary>
        [StringLength(50, ErrorMessage = "Le statut ne doit pas dépasser 50 caractères")]
        public string Status { get; set; } = "Brouillon";

        /// <summary>
        /// Commentaires/Notes
        /// </summary>
        [StringLength(1000, ErrorMessage = "Les commentaires ne doivent pas dépasser 1000 caractères")]
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// Liste des articles reçus
        /// </summary>
        public List<GoodsReceiptLineViewModel> Lines { get; set; } = new();

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Utilisateur ayant créé le bon
        /// </summary>
        [StringLength(100, ErrorMessage = "Le nom d'utilisateur ne doit pas dépasser 100 caractères")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel pour une ligne de bon de réception
    /// </summary>
    public class GoodsReceiptLineViewModel
    {
        /// <summary>
        /// Identifiant unique de la ligne
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Code article (référence)
        /// </summary>
        [Required(ErrorMessage = "Le code article est requis")]
        [StringLength(50, ErrorMessage = "Le code ne doit pas dépasser 50 caractères")]
        public string ArticleCode { get; set; } = string.Empty;

        /// <summary>
        /// Désignation de l'article
        /// </summary>
        [Required(ErrorMessage = "La désignation est requise")]
        [StringLength(255, ErrorMessage = "La désignation ne doit pas dépasser 255 caractères")]
        public string ArticleDescription { get; set; } = string.Empty;

        /// <summary>
        /// Code interne du fournisseur pour cet article
        /// </summary>
        [StringLength(50, ErrorMessage = "Le code fournisseur ne doit pas dépasser 50 caractères")]
        public string SupplierArticleCode { get; set; } = string.Empty;

        /// <summary>
        /// Prix unitaire HT
        /// </summary>
        [Range(0, 99999.99, ErrorMessage = "Le prix doit être positif")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantité reçue
        /// </summary>
        [Range(0, 999999, ErrorMessage = "La quantité doit être positive")]
        public int Quantity { get; set; }

        /// <summary>
        /// Unité de mesure
        /// </summary>
        [StringLength(20, ErrorMessage = "L'unité ne doit pas dépasser 20 caractères")]
        public string Unit { get; set; } = "pcs";

        /// <summary>
        /// Taux de TVA pour cette ligne
        /// </summary>
        [Range(0, 100, ErrorMessage = "Le taux doit être entre 0 et 100")]
        public decimal VatRate { get; set; } = 20;

        /// <summary>
        /// Remise en pourcentage
        /// </summary>
        [Range(0, 100, ErrorMessage = "La remise doit être entre 0 et 100")]
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>
        /// Montant HT de la ligne
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal LineAmountExcludingTax { get; set; }

        /// <summary>
        /// Montant TVA de la ligne
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal LineVatAmount { get; set; }

        /// <summary>
        /// Montant TTC de la ligne
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "Le montant doit être positif")]
        public decimal LineAmountIncludingTax { get; set; }

        /// <summary>
        /// Numéro de ligne
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Commentaires pour cette ligne
        /// </summary>
        [StringLength(500, ErrorMessage = "Les commentaires ne doivent pas dépasser 500 caractères")]
        public string Comments { get; set; } = string.Empty;
    }
}