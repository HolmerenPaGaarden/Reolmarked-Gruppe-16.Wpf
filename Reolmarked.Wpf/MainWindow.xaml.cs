using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Reolmarked.Data;
using Reolmarked.Services;

// PDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Reolmarked.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly ReolContext _db;
        private readonly ProductService _productSvc;
        private readonly SaleService _saleSvc;
        private readonly LeaseService _leaseSvc;
        private readonly AccountingService _acctSvc;
        private readonly PaymentService _paySvc;

        // Gem sidste kørte afregning til eksport
        private AfregningsResultat? _lastSettlement;
        private int _lastLejerId;
        private int _lastYear;
        private int _lastMonth;
        private string _lastLejerNavn = "";

        public MainWindow()
        {
            InitializeComponent();

            _db = new ReolContext();
            _db.Database.EnsureCreated();

            _productSvc = new ProductService(_db);
            _saleSvc = new SaleService(_db);
            _leaseSvc = new LeaseService(_db);
            _acctSvc = new AccountingService(_db);
            _paySvc = new PaymentService(_db);

            // Lille seed hvis tomt
            if (!_db.Reoler.Any() || !_db.Lejere.Any() || !_db.Lejeaftaler.Any())
            {
                var lejerId = _leaseSvc.CreateLejer("Standard Lejer", "12345678", "lejer@example.com");
                var reolId = _leaseSvc.CreateReol("6 hylder");
                _leaseSvc.CreateLejeaftale(lejerId, reolId, DateTime.Today.AddDays(-7), 250m, 25m, null);
            }

            // Init UI
            RefreshProducts_Click(null!, null!);
            RefreshLejere_Click(null!, null!);
            RefreshReoler_Click(null!, null!);
            RefreshLejeaftaler_Click(null!, null!);
            LoadLejerCombo();
            TbAfregAar.Text = DateTime.Today.Year.ToString();
            TbAfregMaaned.Text = DateTime.Today.Month.ToString();
        }

        // ---------- Hjælpere ----------
        private static bool TryParseDecimal(string? input, out decimal value)
        {
            input = (input ?? "").Trim().Replace(',', '.');
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
        private static bool TryParseDate(string? input, out DateTime value) => DateTime.TryParse(input, out value);

        // ---------- Opret produkt + label ----------
        private void CreateProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(TbReolId.Text, out var reolId)) { MessageBox.Show("Ugyldigt ReolID"); return; }
                if (!TryParseDecimal(TbPris.Text, out var pris)) { MessageBox.Show("Ugyldig pris. Brug fx 79,95 eller 79.95"); return; }

                var produktId = _productSvc.CreateProduct(reolId, pris);
                var produkt = _db.Produkter.Find(produktId)!;

                var labelDir = Path.Combine(AppContext.BaseDirectory, "labels");
                Directory.CreateDirectory(labelDir);
                var labelPath = Path.Combine(labelDir, $"{produkt.Stregkode}.png");
                BarcodeService.SaveQrPng(produkt.Stregkode, labelPath, 8);

                LblCreateResult.Text = $"OK. Produkt {produktId} oprettet. Label: {labelPath}";
                RefreshProducts_Click(null!, null!);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        private void RegisterSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var code = (TbBarcode.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(code)) { MessageBox.Show("Indtast/scan stregkoden."); return; }
                var salgId = _saleSvc.RegisterSaleFromBarcode(code);
                LblSaleResult.Text = $"Salg registreret. SalgID={salgId}";
                TbBarcode.Clear();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        private void RefreshProducts_Click(object? sender, RoutedEventArgs? e)
        {
            var data = _db.Produkter
                .Select(p => new { p.ProduktID, p.Stregkode, p.Pris, p.ReolID })
                .OrderByDescending(p => p.ProduktID)
                .ToList();
            GridProducts.ItemsSource = data;
        }

        private void OpenLabels_Click(object sender, RoutedEventArgs e)
        {
            var labelDir = Path.Combine(AppContext.BaseDirectory, "labels");
            Directory.CreateDirectory(labelDir);
            System.Diagnostics.Process.Start("explorer.exe", labelDir);
        }

        // ---------- Lejere & Reoler ----------
        private void CreateLejer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var navn = (TbLejerNavn.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(navn)) { MessageBox.Show("Navn skal udfyldes."); return; }
                var id = _leaseSvc.CreateLejer(navn, TbLejerTlf.Text, TbLejerEmail.Text);
                _lastLejerNavn = navn;
                TbLejerNavn.Clear(); TbLejerTlf.Clear(); TbLejerEmail.Clear();
                RefreshLejere_Click(null!, null!);
                LoadLejerCombo();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        private void CreateReol_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var type = (TbReolType.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(type)) { MessageBox.Show("Type skal udfyldes."); return; }
                _leaseSvc.CreateReol(type);
                TbReolType.Clear();
                RefreshReoler_Click(null!, null!);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        private void CreateLejeaftale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(TbLA_LejerID.Text, out var lejerId)) { MessageBox.Show("Ugyldigt LejerID"); return; }
                if (!int.TryParse(TbLA_ReolID.Text, out var reolId)) { MessageBox.Show("Ugyldigt ReolID"); return; }
                if (!TryParseDate(TbLA_Start.Text, out var start)) { MessageBox.Show("Ugyldig startdato"); return; }

                DateTime? slut = null;
                if (!string.IsNullOrWhiteSpace(TbLA_Slut.Text))
                {
                    if (!TryParseDate(TbLA_Slut.Text, out var s)) { MessageBox.Show("Ugyldig slutdato"); return; }
                    slut = s;
                }
                if (!TryParseDecimal(TbLA_Lejepris.Text, out var lejePris)) { MessageBox.Show("Ugyldig lejepris"); return; }
                if (!TryParseDecimal(TbLA_Kommission.Text, out var kom)) { MessageBox.Show("Ugyldig kommission %"); return; }

                _leaseSvc.CreateLejeaftale(lejerId, reolId, start, lejePris, kom, slut);
                RefreshLejeaftaler_Click(null!, null!);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        private void RefreshLejere_Click(object? sender, RoutedEventArgs? e)
        {
            var data = _db.Lejere.Select(l => new { l.LejerID, l.Navn, l.Tlf, l.Email })
                .OrderBy(l => l.LejerID).ToList();
            GridLejere.ItemsSource = data;
        }

        private void RefreshReoler_Click(object? sender, RoutedEventArgs? e)
        {
            var data = _db.Reoler.Select(r => new { r.ReolID, r.Type })
                .OrderBy(r => r.ReolID).ToList();
            GridReoler.ItemsSource = data;
        }

        private void RefreshLejeaftaler_Click(object? sender, RoutedEventArgs? e)
        {
            int? filterLejerId = null;
            if (int.TryParse((TbFilterLejerID?.Text ?? "").Trim(), out var lid)) filterLejerId = lid;

            var q = _db.Lejeaftaler.AsQueryable();
            if (filterLejerId.HasValue) q = q.Where(a => a.LejerID == filterLejerId.Value);

            var data = q.Select(a => new
            {
                a.LejeaftaleID,
                a.LejerID,
                a.ReolID,
                StartDato = a.StartDato.ToString("yyyy-MM-dd"),
                SlutDato = a.SlutDato == null ? "" : a.SlutDato.Value.ToString("yyyy-MM-dd"),
                a.LejePrisPrMaaned,
                a.KommissionProcent
            }).OrderByDescending(a => a.LejeaftaleID).ToList();

            GridLejeaftaler.ItemsSource = data;
        }

        private void LoadLejerCombo()
        {
            var data = _db.Lejere.Select(l => new { l.LejerID, l.Navn }).OrderBy(l => l.Navn).ToList();
            CbAfregLejer.ItemsSource = data;
            if (data.Count > 0) CbAfregLejer.SelectedIndex = 0;
        }

        // ---------- Afregning & Betaling ----------
        private void RunSettlement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CbAfregLejer.SelectedValue == null) { MessageBox.Show("Vælg lejer."); return; }
                _lastLejerId = (int)CbAfregLejer.SelectedValue;
                _lastLejerNavn = (CbAfregLejer.SelectedItem as dynamic)?.Navn ?? "";
                if (!int.TryParse(TbAfregAar.Text, out _lastYear) || !int.TryParse(TbAfregMaaned.Text, out _lastMonth) ||
                    _lastMonth < 1 || _lastMonth > 12)
                {
                    MessageBox.Show("Ugyldig år/måned."); return;
                }

                _lastSettlement = _acctSvc.KørAfregning(_lastLejerId, _lastYear, _lastMonth, gem: ChkGemAfregning.IsChecked == true);

                GridAfregLinjer.ItemsSource = _lastSettlement.Linjer;
                LblSumOms.Text = $"Omsætning: {_lastSettlement.Omsaetning:0.00}";
                LblSumKom.Text = $"Kommission: {_lastSettlement.Kommission:0.00}";
                LblSumLeje.Text = $"Reolleje: {_lastSettlement.Reolleje:0.00}";
                LblSumNetto.Text = $"Netto: {_lastSettlement.Netto:0.00}";
                LblAfregningID.Text = _lastSettlement.AfregningID > 0 ? $"(AfregningID: {_lastSettlement.AfregningID})" : "";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        private void RegisterPayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CbAfregLejer.SelectedValue == null) { MessageBox.Show("Vælg lejer."); return; }
                var lejerId = (int)CbAfregLejer.SelectedValue;

                if (!TryParseDecimal(TbPayAmount.Text, out var amount)) { MessageBox.Show("Ugyldigt beløb."); return; }

                var method = (TbPayMethod.Text ?? "MobilePay").Trim();
                var note = TbPayNote.Text;

                var pid = _paySvc.RegistrerBetaling(lejerId, amount, method, note);
                LblPaymentResult.Text = $"Betaling registreret (ID {pid}).";
                TbPayAmount.Clear(); TbPayNote.Clear();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Fejl"); }
        }

        // ---------- Eksport CSV ----------
        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_lastSettlement == null)
            {
                MessageBox.Show("Kør en afregning først."); return;
            }

            var dir = Path.Combine(AppContext.BaseDirectory, "exports");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"Afregning_{_lastLejerNavn}_{_lastYear}-{_lastMonth:00}.csv");

            var sb = new StringBuilder();
            sb.AppendLine($"Lejer;{_lastLejerNavn}");
            sb.AppendLine($"Periode;{_lastYear}-{_lastMonth:00}");
            sb.AppendLine();
            sb.AppendLine("Type;Beskrivelse;Beløb");

            foreach (var l in _lastSettlement.Linjer)
                sb.AppendLine($"{l.Type};{l.Beskrivelse};{l.Beløb:0.00}");

            sb.AppendLine();
            sb.AppendLine($";Omsætning;{_lastSettlement.Omsaetning:0.00}");
            sb.AppendLine($";Kommission;{-_lastSettlement.Kommission:0.00}");
            sb.AppendLine($";Reolleje;{-_lastSettlement.Reolleje:0.00}");
            sb.AppendLine($";Netto;{_lastSettlement.Netto:0.00}");

            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(true));
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file}\"");
        }

        // ---------- Eksport PDF (QuestPDF) ----------
        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_lastSettlement == null)
            {
                MessageBox.Show("Kør en afregning først."); return;
            }

            var dir = Path.Combine(AppContext.BaseDirectory, "exports");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"Afregning_{_lastLejerNavn}_{_lastYear}-{_lastMonth:00}.pdf");

            var res = _lastSettlement; // shorthand
            var periode = $"{_lastYear}-{_lastMonth:00}";

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Reolmarked – Afregning").FontSize(18).SemiBold();
                            col.Item().Text($"Lejer: {_lastLejerNavn}");
                            col.Item().Text($"Periode: {periode}");
                            if (res.AfregningID > 0) col.Item().Text($"AfregningID: {res.AfregningID}").FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Type
                                c.RelativeColumn(6); // Beskrivelse
                                c.RelativeColumn(2); // Beløb
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("Type");
                                h.Cell().Element(CellHeader).Text("Beskrivelse");
                                h.Cell().Element(CellHeader).AlignRight().Text("Beløb");
                            });

                            foreach (var l in res.Linjer)
                            {
                                table.Cell().Element(CellBody).Text(l.Type);
                                table.Cell().Element(CellBody).Text(l.Beskrivelse);
                                table.Cell().Element(CellBody).AlignRight().Text($"{l.Beløb:0.00}");
                            }

                            static IContainer CellHeader(IContainer c) => c.Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
                            static IContainer CellBody(IContainer c) => c.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        });

                        col.Item().Height(8);

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("");
                            r.RelativeItem().Column(s =>
                            {
                                s.Item().Text($"Omsætning: {res.Omsaetning:0.00}");
                                s.Item().Text($"Kommission: {res.Kommission:0.00}");
                                s.Item().Text($"Reolleje: {res.Reolleje:0.00}");
                                s.Item().Text($"Netto: {res.Netto:0.00}").SemiBold();
                            });
                        });
                    });

                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Genereret ").FontColor(Colors.Grey.Medium);
                        txt.Span($"{DateTime.Now:yyyy-MM-dd HH:mm}");
                    });
                });
            }).GeneratePdf(file);

            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file}\"");
        }
    }
}
