// File: Models.cs
using System;
using System.Collections.Generic;

namespace Reolmarked.Data
{
    public class Lejer
    {
        public int LejerID { get; set; }
        public string Navn { get; set; } = null!;
        public string? Tlf { get; set; }
        public string? Email { get; set; }

        public ICollection<Lejeaftale> Lejeaftaler { get; set; } = new List<Lejeaftale>();
    }

    public class Reol
    {
        public int ReolID { get; set; }
        public string Type { get; set; } = null!;

        public ICollection<Lejeaftale> Lejeaftaler { get; set; } = new List<Lejeaftale>();
        public ICollection<Produkt> Produkter { get; set; } = new List<Produkt>();
    }

    public class Lejeaftale
    {
        public int LejeaftaleID { get; set; }

        public int LejerID { get; set; }
        public Lejer Lejer { get; set; } = null!;

        public int ReolID { get; set; }
        public Reol Reol { get; set; } = null!;

        public DateTime StartDato { get; set; }
        public DateTime? SlutDato { get; set; }

        public decimal LejePrisPrMaaned { get; set; }    // personlig rabat pr. aftale
        public decimal KommissionProcent { get; set; }   // fx 25.00 = 25%
    }

    public class Produkt
    {
        public int ProduktID { get; set; }
        public int ReolID { get; set; }
        public Reol Reol { get; set; } = null!;
        public decimal Pris { get; set; }
        public string Stregkode { get; set; } = null!;

        public ICollection<Salg> Salg { get; set; } = new List<Salg>();
    }

    public class Salg
    {
        public int SalgID { get; set; }
        public int ProduktID { get; set; }
        public Produkt Produkt { get; set; } = null!;
        public DateTime Dato { get; set; }
        public decimal Pris { get; set; }

        // LÅSES ved salg (kopieres fra aktiv lejeaftale på salgstidspunktet)
        public decimal KommissionProcent { get; set; }
    }

    // Gemt afregning for sporbarhed
    public class Afregning
    {
        public int AfregningID { get; set; }
        public int LejerID { get; set; }
        public Lejer Lejer { get; set; } = null!;

        // YYYY-MM (vi gemmer første dag i måneden for enkelhed)
        public DateTime PeriodeStart { get; set; } // fx 2025-09-01
        public DateTime PeriodeSlut { get; set; }  // fx 2025-09-30

        public decimal TotalSalg { get; set; }
        public decimal TotalKommission { get; set; }
        public decimal TotalReolleje { get; set; }
        public decimal Netto { get; set; } // Omsætning - Kommission - Leje

        public DateTime Oprettet { get; set; }

        public ICollection<AfregningLinje> Linjer { get; set; } = new List<AfregningLinje>();
    }

    public class AfregningLinje
    {
        public int AfregningLinjeID { get; set; }
        public int AfregningID { get; set; }
        public Afregning Afregning { get; set; } = null!;

        public string Type { get; set; } = null!;   // "Salg", "Kommission", "Reolleje"
        public string Beskrivelse { get; set; } = null!;
        public decimal Beløb { get; set; }
    }

    public class Betaling
    {
        public int BetalingID { get; set; }
        public int LejerID { get; set; }
        public Lejer Lejer { get; set; } = null!;

        public DateTime Dato { get; set; }
        public decimal Beløb { get; set; }          // + betyder butik betaler lejer, - betyder lejer betaler butik
        public string Metode { get; set; } = "Ukendt"; // MobilePay, Bank, Kontant...
        public string? Note { get; set; }
    }
}
