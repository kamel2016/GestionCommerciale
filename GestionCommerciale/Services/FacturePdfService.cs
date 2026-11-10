using GestionCommerciale.Data;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionCommerciale.Services
{
    public class FacturePdfService : IFacturePdfService
    {
        private readonly ApplicationDbContext _db;

        public FacturePdfService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<byte[]> GenererAsync(int factureId)
        {
            // Adapte les Include à tes noms de navigation
            var f = await _db.Factures
                .Include(x => x.Client)
                .Include(x => x.Lignes)
                    .ThenInclude(l => l.Article)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == factureId)
                ?? throw new InvalidOperationException("Facture introuvable.");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("VOTRE SOCIÉTÉ").Bold().FontSize(16);
                            c.Item().Text("Adresse de la société");
                            c.Item().Text("MF : 0000000/X/A/M/000");
                            c.Item().Text("Tél : 00 000 000");
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("FACTURE").Bold().FontSize(20);
                            c.Item().Text($"N° : {(string.IsNullOrEmpty(f.Numero) ? "BROUILLON" : f.Numero)}").Bold();
                            c.Item().Text($"Date : {f.Date:dd/MM/yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        // Bloc client
                        col.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Item().Text("Client").Bold();
                            c.Item().Text(f.Client?.Nom ?? "");
                            c.Item().Text(f.Client?.Adresse ?? "");
                            c.Item().Text($"MF : {f.Client?.MatriculeFiscale}");
                        });

                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);    // Désignation
                                c.RelativeColumn(1);    // Qté
                                c.RelativeColumn(1.5f); // PU HT
                                c.RelativeColumn(1);    // Remise
                                c.RelativeColumn(1);    // TVA
                                c.RelativeColumn(1.8f); // Total HT
                            });

                            static IContainer HeaderCell(IContainer c) =>
                                c.Background(Colors.Grey.Lighten2).Padding(4).DefaultTextStyle(t => t.Bold());
                            static IContainer BodyCell(IContainer c) =>
                                c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4);

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Désignation");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Qté");
                                h.Cell().Element(HeaderCell).AlignRight().Text("PU HT");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Remise %");
                                h.Cell().Element(HeaderCell).AlignRight().Text("TVA %");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Total HT");
                            });

                            foreach (var l in f.Lignes)
                            {
                                table.Cell().Element(BodyCell).Text(l.Designation ?? l.Article?.Libelle ?? "");
                                table.Cell().Element(BodyCell).AlignRight().Text(l.Quantite.ToString("0.##"));
                                table.Cell().Element(BodyCell).AlignRight().Text(l.PrixUnitaireHt.ToString("N3"));
                                table.Cell().Element(BodyCell).AlignRight().Text(l.RemisePourcent.ToString("0.##"));
                                table.Cell().Element(BodyCell).AlignRight().Text(l.TauxTva.ToString("0.##"));
                                table.Cell().Element(BodyCell).AlignRight().Text(l.TotalHt.ToString("N3"));
                            }
                        });

                        // Totaux
                        col.Item().PaddingTop(15).AlignRight().Width(220).Column(c =>
                        {
                            Ligne(c, "Total HT", f.TotalHt);
                            if (f.RemiseGlobalePourcent > 0)
                                Ligne(c, "Remise", f.RemiseGlobalePourcent);
                            Ligne(c, "Total TVA", f.TotalTva);
                            Ligne(c, "Timbre fiscal", f.TimbreFiscal);

                            c.Item().PaddingTop(4).BorderTop(1).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL TTC").Bold();
                                r.RelativeItem().AlignRight().Text(f.TotalTtc.ToString("N3") + " DT").Bold();
                            });

                            if (f.MontantRetenueSource > 0)
                            {
                                Ligne(c, $"Retenue à la source ({f.TauxRetenueSource:0.##} %)", -f.MontantRetenueSource);
                            }

                            c.Item().PaddingTop(4).BorderTop(1).Row(r =>
                            {
                                r.RelativeItem().Text("NET À PAYER").Bold();
                                r.RelativeItem().AlignRight().Text(f.ResteAPayer.ToString("N3") + " DT").Bold();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }
        private static void Ligne(ColumnDescriptor c, string label, decimal valeur)
        {
            c.Item().Row(r =>
            {
                r.RelativeItem().Text(label);
                r.RelativeItem().AlignRight().Text(valeur.ToString("N3"));
            });
        }
    }
}