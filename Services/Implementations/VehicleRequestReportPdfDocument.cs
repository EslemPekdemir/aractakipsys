// File: Services/Implementations/VehicleRequestReportPdfDocument.cs
using aractakipsys.Models.ViewModels.Report;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class VehicleRequestReportPdfDocument : IDocument
{
    private readonly VehicleRequestReportViewModel _model;
    private readonly string _generatedByUserName;

    public VehicleRequestReportPdfDocument(VehicleRequestReportViewModel model, string generatedByUserName)
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

            // 1. HEADER
            page.Header().Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(titleCol =>
                    {
                        titleCol.Item().Text("DÜZCE ÜNİVERSİTESİ")
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                        titleCol.Item().Text("Bilgi İşlem Daire Başkanlığı — Birimler Bazında Araç Talep İstatistiği")
                            .SemiBold().FontSize(11).FontColor(Colors.Grey.Darken3);
                    });

                    row.ConstantItem(200).Column(statCol =>
                    {
                        statCol.Item().AlignRight().Text($"Toplam Talep: {_model.GrandTotalRequests}").Bold().FontSize(9);
                        statCol.Item().AlignRight().Text($"Karşılanan: {_model.GrandTotalFulfilled} | Red: {_model.GrandTotalRejected} | Bekleyen: {_model.GrandTotalPending}").FontSize(8).FontColor(Colors.Grey.Darken2);
                        statCol.Item().AlignRight().Text($"Kayıtlı Birim Sayısı: {_model.Items.Count}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                // Filtre Özeti Çubuğu
                headerCol.Item().PaddingTop(6).BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(6).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Filtre / Kapsam: ").Bold();
                            text.Span("Tüm Talep Eden Birimler (Talep Oluşturulma Tarihine Göre)");
                        });

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Dönem / Tarih Aralığı: ").Bold();
                            text.Span(_model.DateRangeText);
                        });
                    });
            });

            // 2. CONTENT (TABLO)
            page.Content().PaddingTop(10).Column(contentCol =>
            {
                contentCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);  // Sıra No
                        columns.RelativeColumn(6);   // Birim
                        columns.RelativeColumn(2);   // Toplam Talep
                        columns.RelativeColumn(2);   // Karşılanan
                        columns.RelativeColumn(2);   // Reddedilen
                        columns.RelativeColumn(2);   // Bekleyen / Diğer
                    });

                    // Tablo Başlığı
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("No").Bold().FontColor(Colors.White).FontSize(8.5f);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignLeft().Text("Birim / Fakülte / Daire").Bold().FontColor(Colors.White).FontSize(8.5f);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Toplam Talep").Bold().FontColor(Colors.White).FontSize(8.5f);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Karşılanan").Bold().FontColor(Colors.White).FontSize(8.5f);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Reddedilen").Bold().FontColor(Colors.White).FontSize(8.5f);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Bekleyen / Diğer").Bold().FontColor(Colors.White).FontSize(8.5f);
                    });

                    int index = 1;
                    foreach (var item in _model.Items)
                    {
                        var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(index.ToString()).FontSize(8.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignLeft().Text(item.DepartmentName).SemiBold().FontSize(8.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(item.TotalRequests.ToString()).Bold().FontSize(8.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(item.FulfilledCount.ToString()).FontColor(Colors.Green.Darken3).Bold().FontSize(8.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(item.RejectedCount.ToString()).FontColor(item.RejectedCount > 0 ? Colors.Red.Darken2 : Colors.Grey.Darken1).FontSize(8.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(item.PendingCount.ToString()).FontColor(item.PendingCount > 0 ? Colors.Orange.Darken2 : Colors.Grey.Darken1).FontSize(8.5f);

                        index++;
                    }

                    // GENEL TOPLAM SATIRI
                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight().Text("GENEL TOPLAM:").Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignCenter().Text(_model.GrandTotalRequests.ToString()).Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignCenter().Text(_model.GrandTotalFulfilled.ToString()).Bold().FontColor(Colors.Green.Darken3).FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignCenter().Text(_model.GrandTotalRejected.ToString()).Bold().FontColor(Colors.Red.Darken2).FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignCenter().Text(_model.GrandTotalPending.ToString()).Bold().FontColor(Colors.Orange.Darken2).FontSize(9);
                });

                // Durum Eşleşmesi Bilgilendirme Notu (Koddaki mevcut durumlar)
                contentCol.Item().PaddingTop(12).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(6).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Durum Eşlemesi: ").Bold().FontSize(8);
                        text.Span("Koddaki mevcut talep durumlarından; 'Fulfilled' olanlar Karşılanan, 'Rejected' olanlar Reddedilen, 'Pending' olanlar Bekleyen/Diğer kategorisinde sayılmıştır.").FontSize(8).FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            // 3. FOOTER
            page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Raporu Alan: {_generatedByUserName}  |  Üretilme Zamanı: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Sayfa ").FontSize(7.5f);
                    text.CurrentPageNumber().FontSize(7.5f);
                    text.Span(" / ").FontSize(7.5f);
                    text.TotalPages().FontSize(7.5f);
                });
            });
        });
    }
}
