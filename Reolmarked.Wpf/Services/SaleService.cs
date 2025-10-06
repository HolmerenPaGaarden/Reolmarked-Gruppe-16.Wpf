// File: Services/SaleService.cs
using System;
using System.Linq;
using Reolmarked.Data;

namespace Reolmarked.Services
{
    public class SaleService
    {
        private readonly ReolContext _db;
        public SaleService(ReolContext db) => _db = db;

        public int RegisterSale(int produktId, decimal pris, DateTime? when = null)
        {
            var produkt = _db.Produkter.FirstOrDefault(p => p.ProduktID == produktId)
                          ?? throw new InvalidOperationException("Produkt ikke fundet.");

            var dato = when ?? DateTime.Now;

            // Find aktiv lejeaftale for denne reol på salgsdatoen
            var la = _db.Lejeaftaler
                .Where(x => x.ReolID == produkt.ReolID
                            && x.StartDato <= dato
                            && (x.SlutDato == null || dato <= x.SlutDato))
                .OrderByDescending(x => x.StartDato)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Ingen aktiv lejeaftale for reolen på salgstidspunktet.");

            var salg = new Salg
            {
                ProduktID = produktId,
                Dato = dato,
                Pris = pris,
                KommissionProcent = la.KommissionProcent // LÅS kommission nu
            };

            _db.Salg.Add(salg);
            _db.SaveChanges();
            return salg.SalgID;
        }

        public int RegisterSaleFromBarcode(string barcode)
        {
            var produkt = _db.Produkter.FirstOrDefault(p => p.Stregkode == barcode)
                          ?? throw new InvalidOperationException("Produkt ikke fundet for stregkode.");
            return RegisterSale(produkt.ProduktID, produkt.Pris);
        }
    }
}
