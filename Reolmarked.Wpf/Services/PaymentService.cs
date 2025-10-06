// File: Services/PaymentService.cs
using System;
using Reolmarked.Data;

namespace Reolmarked.Services
{
    public class PaymentService
    {
        private readonly ReolContext _db;
        public PaymentService(ReolContext db) => _db = db;

        public int RegistrerBetaling(int lejerId, decimal beløb, string metode = "MobilePay", string? note = null, DateTime? dato = null)
        {
            var p = new Betaling
            {
                LejerID = lejerId,
                Beløb = beløb,
                Metode = metode,
                Note = note,
                Dato = dato ?? DateTime.Now
            };
            _db.Betalinger.Add(p);
            _db.SaveChanges();
            return p.BetalingID;
        }
    }
}
