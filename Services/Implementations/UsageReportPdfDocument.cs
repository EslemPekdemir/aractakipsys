// File: Services/Implementations/UsageReportPdfDocument.cs
using aractakipsys.Models.ViewModels.Report;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class UsageReportPdfDocument : IDocument
{
    private readonly UsageReportViewModel _model;
    private readonly string _generatedByUserName;

    public UsageReportPdfDocument(UsageReportViewModel model, string generatedByUserName)
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
                        titleCol.Item().Text("Bilgi İşlem Daire Başkanlığı — Araç Kullanım Raporu")
                            .SemiBold().FontSize(11).FontColor(Colors.Grey.Darken3);
                    });

                    row.ConstantItem(180).Column(statCol =>
                    {
                        statCol.Item().AlignRight().Text($"Toplam Sefer: {_model.TotalTrips}").Bold().FontSize(9);
                        statCol.Item().AlignRight().Text($"Toplam Mesafe: {_model.TotalDistanceKm:N0} KM").Bold().FontColor(Colors.Green.Darken3).FontSize(9);
                        statCol.Item().AlignRight().Text($"Ort. Sefer: {_model.AverageDistanceKm:N1} KM").FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });

                // Filtre Özeti Çubuğu
                headerCol.Item().PaddingTop(6).BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(6).Row(row =>
                    {
                        var vehicleText = string.IsNullOrWhiteSpace(_model.SelectedVehicleName) ? "Tüm Araçlar" : _model.SelectedVehicleName;
                        var driverText = string.IsNullOrWhiteSpace(_model.SelectedDriverName) ? "Tüm Şoförler" : _model.SelectedDriverName;

                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Araç: ").Bold();
                            text.Span(vehicleText);
                            text.Span("   |   Şoför: ").Bold();
                            text.Span(driverText);
                        });

                        row.RelativeItem().AlignRight().Text(text =>
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
                    columns.ConstantColumn(30);   // Sıra (#)
                    columns.RelativeColumn(2.0f); // Plaka / Araç
                    columns.RelativeColumn(2.8f); // Görev Amacı
                    columns.RelativeColumn(2.0f); // Talep Eden Birim
                    columns.RelativeColumn(2.0f); // Tarih / Saat
                    columns.RelativeColumn(1.6f); // Süre
                    columns.RelativeColumn(1.4f); // Başlangıç KM
                    columns.RelativeColumn(1.4f); // Bitiş KM
                    columns.RelativeColumn(1.6f); // Kat Edilen Mesafe
                });

                // Tablo Başlık Satırı
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Plaka / Araç").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Görev Amacı").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Talep Eden Birim").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Tarih").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignCenter().Text("Süre").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Başlangıç KM").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Bitiş KM").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Mesafe (KM)").Bold();
                });

                // Tablo Veri Satırları - Şoför Grupları
                var rowIndex = 0;
                foreach (var group in _model.DriverGroups)
                {
                    // Şoför Grup Başlığı
                    table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text(text =>
                    {
                        text.Span("ŞOFÖR: ").Bold();
                        text.Span(group.DriverFullName).Bold().FontColor(Colors.Blue.Darken3);
                        text.Span($" ({group.DriverDepartment}) — {group.SubtotalTrips} Sefer").FontSize(8).FontColor(Colors.Grey.Darken2);
                    });

                    foreach (var item in group.Items)
                    {
                        var bg = (rowIndex++ % 2 == 0) ? Colors.White : Colors.Grey.Lighten5;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"#{item.AssignmentId}").FontColor(Colors.Grey.Darken1);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text =>
                        {
                            text.Line(item.PlateNumber).Bold();
                            text.Span(item.VehicleBrandModel).FontSize(8).FontColor(Colors.Grey.Darken2);
                        });

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.Purpose);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.RequestedByDepartment ?? "-");

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text =>
                        {
                            text.Line(item.TripDate.ToString("dd.MM.yyyy"));
                            if (item.ActualStartTime.HasValue && item.ActualEndTime.HasValue)
                            {
                                text.Span($"{item.ActualStartTime.Value:HH:mm} - {item.ActualEndTime.Value:HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken2);
                            }
                        });

                        // Süre Sütunu (DurationHelper ile "2 sa 40 dk")
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter()
                            .Text(item.DurationFormatted).Bold().FontColor(Colors.Blue.Darken2);

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                            .Text(item.StartKm.HasValue ? $"{item.StartKm.Value:N0} KM" : "-");

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                            .Text(item.EndKm.HasValue ? $"{item.EndKm.Value:N0} KM" : "-");

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                            .Text($"+{item.DistanceKm:N0} KM").Bold().FontColor(Colors.Green.Darken2);
                    }

                    // Şoför Bazlı Alt Toplam Satırı
                    table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten4).BorderTop(0.5f).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                        .Text($"{group.DriverFullName} — Alt Toplam:").Bold().FontSize(8);

                    table.Cell().Background(Colors.Grey.Lighten4).BorderTop(0.5f).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignCenter()
                        .Text(group.SubtotalDurationFormatted).Bold().FontColor(Colors.Blue.Darken3).FontSize(8);

                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten4).BorderTop(0.5f).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                        .Text("Alt Toplam Mesafe:").Bold().FontSize(8);

                    table.Cell().Background(Colors.Grey.Lighten4).BorderTop(0.5f).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight()
                        .Text($"{group.SubtotalDistanceKm:N0} KM").Bold().FontColor(Colors.Green.Darken3).FontSize(8);
                }

                // Tablo Dip Genel Toplam Satırı (Tekil görevler - Çift sayım engellendi)
                table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten2).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text("GENEL TOPLAM (TEKİL GÖREVLER):").Bold().FontSize(9);

                table.Cell().Background(Colors.Grey.Lighten2).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignCenter()
                    .Text(_model.TotalDurationFormatted).Bold().FontColor(Colors.Blue.Darken4).FontSize(9);

                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten2).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text("GENEL TOPLAM MESAFE:").Bold().FontSize(9);

                table.Cell().Background(Colors.Grey.Lighten2).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text($"{_model.TotalDistanceKm:N0} KM").Bold().FontColor(Colors.Green.Darken4).FontSize(9);
            });

            // 3. SAYFA ALTI (FOOTER)
            page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    text.Span("Oluşturulma Tarihi: ").SemiBold();
                    text.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                    text.Span("   |   Oluşturan: ").SemiBold();
                    text.Span(_generatedByUserName);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }
}
