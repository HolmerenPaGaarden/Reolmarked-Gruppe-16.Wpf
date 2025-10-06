// File: Services/ProductService.cs
using System;
using System.Linq;
using Reolmarked.Data;

namespace Reolmarked.Services
{
    /// <summary>
    /// Håndterer oprettelse og opslag af produkter (inkl. stregkode-streng).
    /// </summary>
    public class ProductService
    {
        private readonly ReolContext _db;

        public ProductService(ReolContext db) => _db = db;

        /// <summary>
        /// Bygger den stregkodestreng vi bruger på labels (stabil og menneskelæsbar).
        /// Format: R{reolId}-P{produktId}-{pris}
        /// </summary>
        public static string BuildBarcodeString(int reolId, decimal pris, int produktId)
            => $"R{reolId}-P{produktId}-{pris:0.00}";

        /// <summary>
        /// Opretter et produkt på en eksisterende reol og genererer stregkoden,
        /// efter produktet har fået sit ProduktID (to SaveChanges-kald med vilje).
        /// Kaster en exception hvis reolen ikke findes.
        /// </summary>
        public int CreateProduct(int reolId, decimal pris)
        {
            // ✅ Valider at reolen findes (ellers får vi FK-fejl)
            if (!_db.Reoler.Any(r => r.ReolID == reolId))
                throw new InvalidOperationException(
                    $"Reol {reolId} findes ikke. Opret den først (menu 4) eller vælg en eksisterende.");

            // Først indsætter vi produktet uden stregkode for at få ProduktID
            var produkt = new Produkt
            {
                ReolID = reolId,
                Pris = pris,
                Stregkode = "PENDING"
            };

            _db.Produkter.Add(produkt);
            _db.SaveChanges(); // giver os ProduktID

            // Nu hvor vi har ProduktID, kan vi generere den endelige stregkode
            produkt.Stregkode = BuildBarcodeString(reolId, pris, produkt.ProduktID);
            _db.SaveChanges();

            return produkt.ProduktID;
        }

        /// <summary>
        /// Finder et produkt ud fra stregkodestrengen.
        /// Returnerer true/false og sætter out-parameteren.
        /// </summary>
        public bool TryGetByBarcode(string barcode, out Produkt? produkt)
        {
            produkt = _db.Produkter.FirstOrDefault(p => p.Stregkode == barcode);
            return produkt != null;
        }
    }
}
