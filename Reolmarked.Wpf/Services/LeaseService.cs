// File: Services/LeaseService.cs
using System;
using System.Linq;
using Reolmarked.Data;

namespace Reolmarked.Services
{
    public class LeaseService
    {
        private readonly ReolContext _db;
        public LeaseService(ReolContext db) => _db = db;

        public int CreateLejer(string navn, string? tlf = null, string? email = null)
        {
            var l = new Lejer { Navn = navn, Tlf = tlf, Email = email };
            _db.Lejere.Add(l);
            _db.SaveChanges();
            return l.LejerID;
        }

        public int CreateReol(string type)
        {
            var r = new Reol { Type = type };
            _db.Reoler.Add(r);
            _db.SaveChanges();
            return r.ReolID;
        }

        public int CreateLejeaftale(int lejerId, int reolId, DateTime start, decimal lejePrisPrMaaned, decimal kommissionPct, DateTime? slut = null)
        {
            // Simpel overlap-guard i kode:
            var overlap = _db.Lejeaftaler.Any(x =>
                x.ReolID == reolId &&
                !((slut != null && x.StartDato > slut) || (x.SlutDato != null && x.SlutDato < start))
            );
            if (overlap)
                throw new InvalidOperationException("Der findes allerede en overlappende lejeaftale for denne reol.");

            var la = new Lejeaftale
            {
                LejerID = lejerId,
                ReolID = reolId,
                StartDato = start,
                SlutDato = slut,
                LejePrisPrMaaned = lejePrisPrMaaned,
                KommissionProcent = kommissionPct
            };
            _db.Lejeaftaler.Add(la);
            _db.SaveChanges();
            return la.LejeaftaleID;
        }
    }
}
