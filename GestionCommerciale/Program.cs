using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chaîne de connexion 'DefaultConnection' introuvable.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<NumerotationService>();

builder.Services.AddScoped<BonLivraisonService>();

builder.Services.AddScoped<ISequenceService, SequenceService>();

builder.Services.AddScoped<IFactureService, FactureService>();

builder.Services.AddScoped<IReglementService, ReglementService>();

builder.Services.AddScoped<IRetenueSourceService, RetenueSourceService>();

// avant builder.Build()
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddScoped<IFacturePdfService, FacturePdfService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!await db.ParametresSociete.AnyAsync())
    {
        db.ParametresSociete.Add(new ParametresSociete
        {
            // Adapte aux propriétés réelles de ta classe
            RaisonSociale = "Raison Sociale 1",
            Adresse = "2 rue des platanes 75013 paris",
            MatriculeFiscale = "132123654",
            TimbreFiscal = 1.000m,
            TauxTVADefaut = 19m
        });
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Configuration basique pour les Razor Pages
app.MapRazorPages();

app.Run();