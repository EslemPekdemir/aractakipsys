// File: Services/Implementations/VehicleTiresReportPdfDocument.cs
using aractakipsys.Models.ViewModels.Vehicle;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class VehicleTiresReportPdfDocument : IDocument
{
    private readonly List<VehicleTireItemViewModel> _items;
    private readonly string _generatedByUserName;

    public VehicleTiresReportPdfDocument(List<VehicleTireItemViewModel> items, string generatedByUserName)
    {
        _items = items;
        _generatedByUserName = generatedByUserName;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Arial"));

            // 1. BAŞLIK (HEADER)
            page.Header().Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(titleCol =>
                    {
                        titleCol.Item().Text("DÜZCE ÜNİVERSİTESİ")
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                        titleCol.Item().Text("Bilgi İşlem Daire Başkanlığı — Lastik Kullanım ve Durum Raporu")
                            .SemiBold().FontSize(11).FontColor(Colors.Grey.Darken3);
                    });

                    row.ConstantItem(260).Column(statCol =>
                    {
                        var total = _items.Count;
                        var alerts = _items.Count(x => x.IsAlert);
                        statCol.Item().AlignRight().Text($"Toplam Araç / Lastik: {total} Adet").Bold().FontSize(9);
                        if (alerts > 0)
                        {
                            statCol.Item().AlignRight().Text($"Değişim / Eşik Uyarısı: {alerts} Araç")
                                .Bold().FontColor(Colors.Red.Darken3).FontSize(9);
                        }
                        else
                        {
                            statCol.Item().AlignRight().Text("Tüm Lastikler Normal Durumda")
                                .Bold().FontColor(Colors.Green.Darken3).FontSize(9);
                        }
                    });
                });

                headerCol.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });

            // 2. TABLO İÇERİĞİ
            page.Content().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(75);  // Plaka
                    columns.RelativeColumn(1.3f); // Araç Marka / Model
                    columns.RelativeColumn(1.1f); // Lastik Marka / Ebat
                    columns.ConstantColumn(55);  // Üretim (DOT)
                    columns.ConstantColumn(55);  // Mevsim
                    columns.ConstantColumn(65);  // Takılma Tarihi
                    columns.ConstantColumn(60);  // Takıldığı KM
                    columns.ConstantColumn(60);  // Güncel KM
                    columns.ConstantColumn(60);  // Kullanılan KM
                    columns.RelativeColumn(1.2f); // Kalan KM / Gün / Durum
                });

                // TABLO BAŞLIĞI
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Plaka").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Araç").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Marka / Ebat").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("DOT / Yıl").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Mevsim").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("Takılma").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Takılma KM").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Güncel KM").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Kullanım").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Kalan / Durum").Bold();
                });

                // SATIRLAR
                foreach (var item in _items)
                {
                    var bg = item.IsAlert ? Colors.Red.Lighten5 : Colors.White;

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(item.VehiclePlate).Bold();

                    var vehicleBrandModel = $"{item.VehicleBrand} {item.VehicleModel}".Trim();
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text(!string.IsNullOrWhiteSpace(vehicleBrandModel) ? vehicleBrandModel : "-");

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .Text($"{item.Brand} {(string.IsNullOrWhiteSpace(item.Size) ? "" : $"({item.Size})")}".Trim());

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignRight().Text(item.DotDateDisplay);

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignCenter().Text(item.SeasonDisplay);

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignCenter().Text(item.InstalledDate.HasValue ? item.InstalledDate.Value.ToString("dd.MM.yyyy") : "-");

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignRight().Text(item.InstalledKm.HasValue ? $"{item.InstalledKm.Value:N0} km" : "-");

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignRight().Text($"{item.CurrentVehicleKm:N0} km");

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                        .AlignRight().Text(item.UsedKmDisplay).Bold();

                    // Kalan / Durum
                    var statusText = item.AlertReason;
                    if (string.IsNullOrEmpty(statusText))
                    {
                        var remainingParts = new List<string>();
                        if (item.RemainingKm.HasValue)
                            remainingParts.Add($"{item.RemainingKm.Value:N0} km");
                        if (item.RemainingDays.HasValue)
                            remainingParts.Add($"{item.RemainingDays.Value} gün");

                        statusText = remainingParts.Count > 0 ? string.Join(" / ", remainingParts) : "Normal";
                    }

                    var statusCell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                    if (item.IsAlert)
                    {
                        statusCell.Text(statusText).Bold().FontColor(Colors.Red.Darken3);
                    }
                    else
                    {
                        statusCell.Text(statusText).FontColor(Colors.Grey.Darken3);
                    }
                }
            });

            // 3. ALT BİLGİ (FOOTER)
            page.Footer().Column(footerCol =>
            {
                footerCol.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                footerCol.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text($"Raporu Alan: {_generatedByUserName} | Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontSize(7.5f).FontColor(Colors.Grey.Darken1);

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(t => t.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                        text.Span("Sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        });
    }
}
