// File: Services/AccountingService.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Reolmarked.Data;

namespace Reolmarked.Services
{
    public record AfregningsResultat(
        int AfregningID,
        decimal Omsaetning,
        decimal Kommission,
        decimal Reolleje,
        decimal Netto,
        List<AfregningLinje> Linjer);

    public class AccountingService
    {
        private readonly ReolContext _db;
        public AccountingService(ReolContext db) => _db = db;

        // Pro-rata leje: (antal aktive dage i måneden / dage i måned) * pris
        private static decimal Prorata(decimal pris, DateTime start, DateTime? slut, DateTime periodStart, DateTime periodEnd)
        {
            var s = start > periodStart ? start : periodStart;
            var e = (slut == null || slut > periodEnd) ? periodEnd : slut.Value;
            if (e < s) return 0m;

            int daysActive = (e - s).Days + 1;
            int daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
            return Math.Round(pris * daysActive / daysInMonth, 2);
        }

        public AfregningsResultat KørAfregning(int lejerId, int year, int month, bool gem = true)
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            // Find lejerens reoler via lejeaftaler i perioden
            var lejeaftaler = _db.Lejeaftaler
                .Where(a => a.LejerID == lejerId &&
                            a.StartDato <= periodEnd &&
                            (a.SlutDato == null || a.SlutDato >= periodStart))
                .ToList();

            // Salg i perioden på disse reoler
            var reolIds = lejeaftaler.Select(a => a.ReolID).Distinct().ToList();

            var salg = _db.Salg
                .Where(s => s.Dato >= periodStart && s.Dato <= periodEnd && reolIds.Contains(s.Produkt.ReolID))
                .Select(s => new { s.SalgID, s.Pris, s.KommissionProcent, s.Dato, s.Produkt.ReolID })
                .ToList();

            decimal omsaetning = salg.Sum(x => x.Pris);
            decimal kommission = salg.Sum(x => Math.Round(x.Pris * x.KommissionProcent / 100m, 2));

            // Reolleje (prorata for den del af perioden aftalen er aktiv)
            decimal reolleje = 0m;
            foreach (var a in lejeaftaler)
            {
                reolleje += Prorata(a.LejePrisPrMaaned, a.StartDato, a.SlutDato, periodStart, periodEnd);
            }

            decimal netto = omsaetning - kommission - reolleje;

            // Linjer (for sporbarhed)
            var linjer = new List<AfregningLinje>
            {
                new AfregningLinje { Type = "Salg",        Beskrivelse = "Månedens omsætning",      Beløb = omsaetning },
                new AfregningLinje { Type = "Kommission",  Beskrivelse = "Butikkens kommission",    Beløb = -kommission },
                new AfregningLinje { Type = "Reolleje",    Beskrivelse = "Leje af reoler (prorata)",Beløb = -reolleje }
            };

            int afregningId = 0;
            if (gem)
            {
                var a = new Afregning
                {
                    LejerID = lejerId,
                    PeriodeStart = periodStart,
                    PeriodeSlut = periodEnd,
                    TotalSalg = omsaetning,
                    TotalKommission = kommission,
                    TotalReolleje = reolleje,
                    Netto = netto,
                    Oprettet = DateTime.Now,
                    Linjer = linjer
                };
                _db.Afregninger.Add(a);
                _db.SaveChanges();
                afregningId = a.AfregningID;
            }

            return new AfregningsResultat(afregningId, omsaetning, kommission, reolleje, netto, linjer);
        }
    }
}
