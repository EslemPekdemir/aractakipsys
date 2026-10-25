// File: Services/Implementations/MaintenanceReportPdfDocument.cs
using aractakipsys.Models.ViewModels.Report;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class MaintenanceReportPdfDocument : IDocument
{
    private readonly MaintenanceReportViewModel _model;
    private readonly string _generatedByUserName;

    public MaintenanceReportPdfDocument(MaintenanceReportViewModel model, string generatedByUserName)
    {
        _model = model;
        _generatedByUserName = generatedByUserName;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

            // 1. SAYFA ÜSTÜ (HEADER)
            page.Header().Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(titleCol =>
                    {
                        titleCol.Item().Text("DÜZCE ÜNİVERSİTESİ")
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                        titleCol.Item().Text("Ulaşım ve Taşıt İşletme — Araç Bakım ve Onarım Döküm Raporu")
                            .SemiBold().FontSize(11).FontColor(Colors.Grey.Darken3);
                    });

                    row.ConstantItem(220).Column(statCol =>
                    {
                        statCol.Item().AlignRight().Text($"Toplam Bakım / Onarım: {_model.TotalMaintenances} Adet").Bold().FontSize(9);
                        statCol.Item().AlignRight().Text($"Serviste Geçen Süre: {_model.TotalDaysInService} Gün").Bold().FontColor(Colors.Orange.Darken3).FontSize(9);
                        statCol.Item().AlignRight().Text($"Tamamlanan: {_model.CompletedCount}  |  Devam Eden: {_model.OngoingCount}").FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });

                // Filtre Özeti Çubuğu
                headerCol.Item().PaddingTop(6).BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(6).Row(row =>
                    {
                        var vehicleText = string.IsNullOrWhiteSpace(_model.SelectedVehicleName) ? "Tüm Araçlar" : _model.SelectedVehicleName;
                        var typeText = string.IsNullOrWhiteSpace(_model.Filter.MaintenanceType) ? "Tüm Türler" : _model.Filter.MaintenanceType;
                        var statusText = _model.Filter.IsOngoing.HasValue
                            ? (_model.Filter.IsOngoing.Value ? "Devam Edenler" : "Tamamlananlar")
                            : "Tümü";

                        row.RelativeItem(3).Text(text =>
                        {
                            text.Span("Araç: ").Bold();
                            text.Span(vehicleText);
                            text.Span("   |   Tür: ").Bold();
                            text.Span(typeText);
                            text.Span("   |   Durum: ").Bold();
                            text.Span(statusText);
                        });

                        row.RelativeItem(2).AlignRight().Text(text =>
                        {
                            text.Span("Dönem / Tarih: ").Bold();
                            text.Span(_model.DateRangeText);
                        });
                    });
            });

            // 2. SAYFA İÇERİĞİ (TABLO)
            page.Content().PaddingVertical(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);   // Sıra (#)
                    columns.RelativeColumn(1.8f); // Plaka / Araç
                    columns.RelativeColumn(1.8f); // Bakım Türü
                    columns.RelativeColumn(1.8f); // Servis / Firma
                    columns.RelativeColumn(1.9f); // Giriş Tarihi (Tarih & Saat)
                    columns.RelativeColumn(1.9f); // Çıkış Tarihi (Tarih & Saat)
                    columns.RelativeColumn(1.0f); // Süre (Gün)
                    columns.RelativeColumn(1.5f); // Maliyet
                    columns.RelativeColumn(1.2f); // Durum
                    columns.RelativeColumn(2.6f); // Açıklama / Yapılan İşlem
                });

                // Tablo Başlık Satırı
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Plaka / Araç").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Bakım Türü").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Servis / Firma").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Giriş Tarih/Saat").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Çıkış Tarih/Saat").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignCenter().Text("Süre").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Maliyet").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignCenter().Text("Durum").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Açıklama / İşlemler").Bold();
                });

                // Tablo Veri Satırları
                var rowIndex = 1;
                foreach (var item in _model.Items)
                {
                    var bg = (rowIndex % 2 == 0) ? Colors.White : Colors.Grey.Lighten5;

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{rowIndex++}").FontColor(Colors.Grey.Darken1);

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(col =>
                    {
                        col.Item().Text(item.PlateNumber).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(item.VehicleBrandModel).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.MaintenanceType).Bold();

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(string.IsNullOrWhiteSpace(item.PerformedBy) ? "-" : item.PerformedBy);

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.StartDate.ToString("dd.MM.yyyy HH:mm"));

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.EndDate.HasValue ? item.EndDate.Value.ToString("dd.MM.yyyy HH:mm") : "-");

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{item.DurationDays} Gün").Bold();

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                        .Text(item.CostDisplay).Bold().FontColor(item.Cost.HasValue ? Colors.Green.Darken3 : Colors.Grey.Darken1);

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(item.IsOngoing ? "Devam Ediyor" : "Tamamlandı")
                        .Bold().FontColor(item.IsOngoing ? Colors.Orange.Darken3 : Colors.Green.Darken3);

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description).FontSize(8);
                }

                // Tablo Dip Toplam Satırı (Genel Toplam Maliyet)
                table.Cell().ColumnSpan(7).Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text("GENEL TOPLAM BAKIM MALİYETİ:").Bold().FontSize(9);

                table.Cell().Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text(_model.TotalCostFormatted).Bold().FontColor(Colors.Green.Darken4).FontSize(9);

                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5)
                    .Text("");
            });

            // 3. SAYFA ALTI (FOOTER)
            page.Footer().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Raporu Oluşturan: ").SemiBold();
                    text.Span(_generatedByUserName);
                    text.Span("  |  Tarih: ").SemiBold();
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });

                row.RelativeItem().AlignCenter().Text("Resmî Belge Niteliğindedir — Düzce Üniversitesi Taşıt İşletme Yönergesi")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }
}
