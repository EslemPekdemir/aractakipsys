// File: Services/Implementations/AssignmentOrderDocument.cs
using aractakipsys.Models.ViewModels.Assignment;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class AssignmentOrderDocument : IDocument
{
    private readonly AssignmentDetailsViewModel _model;
    private readonly string _generatedByUserName;

    public AssignmentOrderDocument(AssignmentDetailsViewModel model, string generatedByUserName)
    {
        _model = model;
        _generatedByUserName = generatedByUserName;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(35);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            // 1. BAŞLIK (HEADER)
            page.Header().Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text("T.C.").Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                        col.Item().AlignCenter().Text("DÜZCE ÜNİVERSİTESİ").Bold().FontSize(15).FontColor(Colors.Blue.Darken3);
                        col.Item().AlignCenter().Text("BİLGİ İŞLEM DAİRE BAŞKANLIĞI").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken3);
                        col.Item().AlignCenter().PaddingTop(4).Text("ARAÇ GÖREVLENDİRME BELGESİ").Bold().FontSize(13).Underline().FontColor(Colors.Blue.Darken2);
                    });
                });

                headerCol.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Belge / Görev No: ").Bold();
                        t.Span($"GVR-{_model.Id:D5}");
                    });

                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Düzenlenme Tarihi: ").Bold();
                        t.Span(DateTime.Now.ToString("dd.MM.yyyy"));
                    });
                });

                headerCol.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });

            // 2. GÖVDE (CONTENT)
            page.Content().PaddingVertical(15).Column(col =>
            {
                // Bilgi Tablosu Çerçevesi
                col.Item().Border(1).BorderColor(Colors.Grey.Medium).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });

                    // 1. Satır: Araç Bilgileri
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Araç Plakası:").Bold();
                    table.Cell().Padding(6).Text(_model.VehiclePlate).Bold().FontColor(Colors.Blue.Darken3);
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Marka / Model:").Bold();
                    table.Cell().Padding(6).Text($"{_model.VehicleBrandModel} ({_model.VehicleType})");

                    // 2. Satır: Şoför Bilgileri
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Görevli Şoför:").Bold();
                    table.Cell().Padding(6).Text(_model.DriverFullName).Bold();
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Ehliyet No / Sınıf:").Bold();
                    table.Cell().Padding(6).Text(string.IsNullOrWhiteSpace(_model.LicenseNumber) ? "-" : $"{_model.LicenseNumber} ({_model.LicenseClass ?? "-"})");

                    // 3. Satır: Talep Eden Birim & İletişim
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Talep Eden Birim:").Bold();
                    table.Cell().Padding(6).Text(string.IsNullOrWhiteSpace(_model.RequestedByDepartment) ? "Belirtilmemiş" : _model.RequestedByDepartment);
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Şoför İletişim:").Bold();
                    table.Cell().Padding(6).Text(string.IsNullOrWhiteSpace(_model.DriverPhone) ? "-" : _model.DriverPhone);

                    // 4. Satır: Görev Zaman Aralığı
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Planlanan Başlangıç:").Bold();
                    table.Cell().Padding(6).Text(_model.PlannedStartTime.ToString("dd.MM.yyyy HH:mm")).Bold();
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Planlanan Bitiş:").Bold();
                    table.Cell().Padding(6).Text(_model.PlannedEndTime.ToString("dd.MM.yyyy HH:mm")).Bold();

                    // 5. Satır: Görev Amacı & Güzergah
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Görev Amacı / Güzergah:").Bold();
                    table.Cell().ColumnSpan(3).Padding(6).Text(_model.Purpose);

                    // Çok Günlük Görevler İçin Ek Satır (Varsa)
                    if (_model.HasAccommodationOrPerDiem)
                    {
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Konaklama / Görev Ücreti:").Bold();
                        table.Cell().ColumnSpan(3).Padding(6).Text(
                            $"Konaklama: {(_model.AccommodationNights.HasValue ? $"{_model.AccommodationNights.Value} Gece" : "-")} | " +
                            $"Ücretli Gün: {(_model.PerDiemDays.HasValue ? $"{_model.PerDiemDays.Value} Gün" : "-")}" +
                            (!string.IsNullOrWhiteSpace(_model.ExpenseNotes) ? $" (Not: {_model.ExpenseNotes})" : "")
                        );
                    }
                });

                // Resmi İbraz ve Görevlendirme Metni
                col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.Grey.Lighten5).Padding(10).Column(textCol =>
                    {
                        textCol.Item().Text(
                            "Yukarıda bilgileri belirtilen personel, belirtilen tarih aralığında ve güzergahta belirtilen aracı kullanmak üzere görevlendirilmiştir."
                        ).Italic().SemiBold().FontSize(10).FontColor(Colors.Grey.Darken4);
                    });

                // İki Yan Yana Resmi İmza Alanı (Fiziksel Çıktı İçin Çizgili / Boş Kutular)
                col.Item().PaddingTop(35).Row(signRow =>
                {
                    // Sol Kutu: Görevlendiren Yetkili
                    signRow.RelativeItem().PaddingRight(20).Border(1).BorderColor(Colors.Grey.Medium).Padding(12).Column(leftSign =>
                    {
                        leftSign.Item().AlignCenter().Text("GÖREVLENDİREN YETKİLİ").Bold().FontSize(10);
                        leftSign.Item().AlignCenter().Text("Ulaşım / Birim Şefi").FontSize(9).FontColor(Colors.Grey.Darken2);
                        leftSign.Item().PaddingTop(8).AlignCenter().Text($"Adı Soyadı: {_generatedByUserName}").FontSize(9);
                        leftSign.Item().PaddingTop(4).AlignCenter().Text($"Tarih: {DateTime.Now:dd.MM.yyyy}").FontSize(9);
                        leftSign.Item().PaddingTop(25).AlignCenter().Text("İmza: ........................................").FontSize(9);
                    });

                    // Sağ Kutu: Görevli Şoför
                    signRow.RelativeItem().PaddingLeft(20).Border(1).BorderColor(Colors.Grey.Medium).Padding(12).Column(rightSign =>
                    {
                        rightSign.Item().AlignCenter().Text("GÖREVLİ PERSONEL (ŞOFÖR)").Bold().FontSize(10);
                        rightSign.Item().AlignCenter().Text("Araç Sürücüsü").FontSize(9).FontColor(Colors.Grey.Darken2);
                        rightSign.Item().PaddingTop(8).AlignCenter().Text($"Adı Soyadı: {_model.DriverFullName}").FontSize(9);
                        rightSign.Item().PaddingTop(4).AlignCenter().Text($"Tarih: {_model.PlannedStartTime:dd.MM.yyyy}").FontSize(9);
                        rightSign.Item().PaddingTop(25).AlignCenter().Text("İmza: ........................................").FontSize(9);
                    });
                });
            });

            // 3. SAYFA ALTI (FOOTER)
            page.Footer().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    t.Span("DÜ Araç Takip Sistemi   |   Belge No: ").SemiBold();
                    t.Span($"#{_model.Id}");
                    t.Span("   |   Oluşturulma: ").SemiBold();
                    t.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}");
                });

                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    t.Span("Sayfa ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });
    }
}
