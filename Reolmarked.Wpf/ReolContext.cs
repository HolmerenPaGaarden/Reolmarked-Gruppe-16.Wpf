// File: ReolContext.cs
using Microsoft.EntityFrameworkCore;

namespace Reolmarked.Data
{
    public class ReolContext : DbContext
    {
        private const bool UseSqlite = true;

        public DbSet<Lejer> Lejere => Set<Lejer>();
        public DbSet<Reol> Reoler => Set<Reol>();
        public DbSet<Lejeaftale> Lejeaftaler => Set<Lejeaftale>();
        public DbSet<Produkt> Produkter => Set<Produkt>();
        public DbSet<Salg> Salg => Set<Salg>();
        public DbSet<Afregning> Afregninger => Set<Afregning>();
        public DbSet<AfregningLinje> AfregningLinjer => Set<AfregningLinje>();
        public DbSet<Betaling> Betalinger => Set<Betaling>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (UseSqlite)
                optionsBuilder.UseSqlite("Data Source=reolmarked.db");
            else
                optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=Reolmarked;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Lejer>(e =>
            {
                e.HasKey(x => x.LejerID);
                e.Property(x => x.Navn).IsRequired().HasMaxLength(100);
                e.Property(x => x.Tlf).HasMaxLength(30);
                e.Property(x => x.Email).HasMaxLength(256);
            });

            model.Entity<Reol>(e =>
            {
                e.HasKey(x => x.ReolID);
                e.Property(x => x.Type).IsRequired().HasMaxLength(50);
            });

            model.Entity<Lejeaftale>(e =>
            {
                e.HasKey(x => x.LejeaftaleID);
                e.HasOne(x => x.Lejer)
                    .WithMany(l => l.Lejeaftaler)
                    .HasForeignKey(x => x.LejerID)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Reol)
                    .WithMany(r => r.Lejeaftaler)
                    .HasForeignKey(x => x.ReolID)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(x => x.LejePrisPrMaaned).HasColumnType("decimal(10,2)");
                e.Property(x => x.KommissionProcent).HasColumnType("decimal(5,2)");

                // Hjælpeindekser
                e.HasIndex(x => new { x.ReolID, x.StartDato, x.SlutDato });
                e.HasIndex(x => new { x.LejerID, x.StartDato, x.SlutDato });
            });

            model.Entity<Produkt>(e =>
            {
                e.HasKey(x => x.ProduktID);
                e.Property(x => x.Pris).HasColumnType("decimal(10,2)");
                e.Property(x => x.Stregkode).IsRequired();
                e.HasIndex(x => x.Stregkode).IsUnique();

                e.HasOne(x => x.Reol)
                    .WithMany(r => r.Produkter)
                    .HasForeignKey(x => x.ReolID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            model.Entity<Salg>(e =>
            {
                e.HasKey(x => x.SalgID);
                e.Property(x => x.Pris).HasColumnType("decimal(10,2)");
                e.Property(x => x.KommissionProcent).HasColumnType("decimal(5,2)");

                e.HasOne(x => x.Produkt)
                    .WithMany(p => p.Salg)
                    .HasForeignKey(x => x.ProduktID)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => x.Dato);
            });

            model.Entity<Afregning>(e =>
            {
                e.HasKey(x => x.AfregningID);
                e.HasOne(x => x.Lejer)
                    .WithMany()
                    .HasForeignKey(x => x.LejerID)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(x => x.TotalSalg).HasColumnType("decimal(10,2)");
                e.Property(x => x.TotalKommission).HasColumnType("decimal(10,2)");
                e.Property(x => x.TotalReolleje).HasColumnType("decimal(10,2)");
                e.Property(x => x.Netto).HasColumnType("decimal(10,2)");

                e.HasIndex(x => new { x.LejerID, x.PeriodeStart, x.PeriodeSlut }).IsUnique();
            });

            model.Entity<AfregningLinje>(e =>
            {
                e.HasKey(x => x.AfregningLinjeID);
                e.Property(x => x.Type).IsRequired().HasMaxLength(20);
                e.Property(x => x.Beløb).HasColumnType("decimal(10,2)");

                e.HasOne(x => x.Afregning)
                    .WithMany(a => a.Linjer)
                    .HasForeignKey(x => x.AfregningID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            model.Entity<Betaling>(e =>
            {
                e.HasKey(x => x.BetalingID);
                e.HasOne(x => x.Lejer)
                    .WithMany()
                    .HasForeignKey(x => x.LejerID)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(x => x.Beløb).HasColumnType("decimal(10,2)");
                e.Property(x => x.Metode).HasMaxLength(40);
            });
        }
    }
}
