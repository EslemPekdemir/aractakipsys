// File: Services/Implementations/InsuranceInspectionReportPdfDocument.cs
using aractakipsys.Models.ViewModels.Report;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class InsuranceInspectionReportPdfDocument : IDocument
{
    private readonly InsuranceInspectionReportViewModel _model;
    private readonly string _generatedByUserName;

    public InsuranceInspectionReportPdfDocument(InsuranceInspectionReportViewModel model, string generatedByUserName)
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

            var scope = _model.Filter.Scope;
            var subTitle = scope switch
            {
                ReportScopeType.Insurance => "Bilgi İşlem Daire Başkanlığı — Araç Trafik Sigortası Durum Raporu",
                ReportScopeType.Inspection => "Bilgi İşlem Daire Başkanlığı — Araç Fenni Muayene Durum Raporu",
                ReportScopeType.Kasko => "Bilgi İşlem Daire Başkanlığı — Araç Kasko Durum Raporu",
                _ => "Bilgi İşlem Daire Başkanlığı — Araç Belge Durum Raporu (Muayene, Sigorta, Kasko)"
            };

            // 1. BAŞLIK (HEADER)
            page.Header().Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(titleCol =>
                    {
                        titleCol.Item().Text("DÜZCE ÜNİVERSİTESİ")
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                        titleCol.Item().Text(subTitle)
                            .SemiBold().FontSize(11).FontColor(Colors.Grey.Darken3);
                    });

                    row.ConstantItem(260).Column(statCol =>
                    {
                        statCol.Item().AlignRight().Text($"Toplam Araç: {_model.TotalVehiclesCount} Adet").Bold().FontSize(9);
                        if (scope == ReportScopeType.Insurance)
                        {
                            statCol.Item().AlignRight().Text($"Sigortası Geçenler: {_model.ExpiredCount} Araç").Bold().FontColor(Colors.Red.Darken3).FontSize(9);
                            statCol.Item().AlignRight().Text($"Sigortası 30 Gün Kalanlar: {_model.ExpiringSoonCount} Araç").Bold().FontColor(Colors.Orange.Darken3).FontSize(9);
                        }
                        else if (scope == ReportScopeType.Inspection)
                        {
                            statCol.Item().AlignRight().Text($"Muayenesi Geçenler: {_model.ExpiredCount} Araç").Bold().FontColor(Colors.Red.Darken3).FontSize(9);
                            statCol.Item().AlignRight().Text($"Muayenesi 30 Gün Kalanlar: {_model.ExpiringSoonCount} Araç").Bold().FontColor(Colors.Orange.Darken3).FontSize(9);
                        }
                        else if (scope == ReportScopeType.Kasko)
                        {
                            statCol.Item().AlignRight().Text($"Kaskosu Geçenler: {_model.ExpiredCount} Araç").Bold().FontColor(Colors.Red.Darken3).FontSize(9);
                            statCol.Item().AlignRight().Text($"Kaskosu 30 Gün Kalanlar: {_model.ExpiringSoonCount} Araç").Bold().FontColor(Colors.Orange.Darken3).FontSize(9);
                        }
                        else
                        {
                            statCol.Item().AlignRight().Text($"Süresi Geçenler: {_model.ExpiredCount} Araç").Bold().FontColor(Colors.Red.Darken3).FontSize(9);
                            statCol.Item().AlignRight().Text($"30 Gün İçinde Dolacaklar: {_model.ExpiringSoonCount} Araç").Bold().FontColor(Colors.Orange.Darken3).FontSize(9);
                        }
                    });
                });

                // Filtre Özeti Çubuğu
                headerCol.Item().PaddingTop(6).BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(6).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Kapsam: ").Bold();
                            text.Span(_model.ScopeDisplayName);
                            text.Span("   |   Filtre: ").Bold();
                            text.Span(_model.FilterOptionDisplayName);
                            if (!string.IsNullOrWhiteSpace(_model.Filter.SearchTerm))
                            {
                                text.Span("   |   Arama: ").Bold();
                                text.Span(_model.Filter.SearchTerm);
                            }
                        });

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Rapor Tarihi: ").Bold();
                            text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                        });
                    });
            });

            // 2. TABLO İÇERİĞİ
            // Kullanıcı Belirtimi: Sütunlar: plaka, araç (marka/model), belge türü, başlangıç, bitiş, kalan gün/durum, firma/kurum, poliçe/istasyon no, ödenen tutar; sonda toplam tutar.
            page.Content().PaddingVertical(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(22);   // #
                    columns.RelativeColumn(1.8f); // Plaka
                    columns.RelativeColumn(2.3f); // Araç (Marka / Model)
                    columns.RelativeColumn(2.0f); // Belge Türü
                    columns.RelativeColumn(1.8f); // Başlangıç
                    columns.RelativeColumn(1.8f); // Bitiş
                    columns.RelativeColumn(2.4f); // Kalan Gün / Durum
                    columns.RelativeColumn(2.4f); // Firma / Kurum
                    columns.RelativeColumn(2.0f); // Poliçe / İstasyon No
                    columns.RelativeColumn(1.8f); // Ödenen Tutar
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Plaka").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Araç (Marka / Model)").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Belge Türü").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Başlangıç").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Bitiş").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Kalan Gün / Durum").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Firma / Kurum").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Poliçe / İstasyon No").Bold();
                    header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignRight().Text("Ödenen Tutar").Bold();
                });

                int rowIndex = 1;
                decimal grandTotalCost = 0;

                foreach (var item in _model.Items)
                {
                    var brandModel = $"{item.Brand} {item.Model}".Trim();
                    if (string.IsNullOrWhiteSpace(brandModel)) brandModel = "-";
                    else brandModel = $"{brandModel} ({item.Year})";

                    // Hangi belgelerin basılacağı
                    var docsToPrint = new List<(string DocType, DateTime StartDate, DateTime EndDate, int DaysRemaining, decimal? Cost, string? Company, string? PolicyNo, bool IsExpired, bool IsExpiringSoon)>();

                    if (scope == ReportScopeType.Insurance || scope == ReportScopeType.All)
                    {
                        docsToPrint.Add((
                            "Trafik Sigortası",
                            item.InsuranceStartDate,
                            item.InsuranceExpiryDate,
                            item.InsuranceDaysRemaining,
                            item.InsuranceCost,
                            item.InsuranceCompanyName,
                            item.InsurancePolicyNumber,
                            item.IsInsuranceExpired,
                            item.IsInsuranceExpiringSoon
                        ));
                    }

                    if (scope == ReportScopeType.Inspection || scope == ReportScopeType.All)
                    {
                        docsToPrint.Add((
                            "Fenni Muayene",
                            item.InspectionStartDate,
                            item.InspectionExpiryDate,
                            item.InspectionDaysRemaining,
                            item.InspectionCost,
                            item.InspectionStationName,
                            item.InspectionPolicyNumber,
                            item.IsInspectionExpired,
                            item.IsInspectionExpiringSoon
                        ));
                    }

                    if (scope == ReportScopeType.Kasko || scope == ReportScopeType.All)
                    {
                        if (item.KaskoExpiryDate.HasValue || scope == ReportScopeType.Kasko)
                        {
                            docsToPrint.Add((
                                "Kasko",
                                item.KaskoStartDate ?? DateTime.MinValue,
                                item.KaskoExpiryDate ?? DateTime.MinValue,
                                item.KaskoDaysRemaining ?? 0,
                                item.KaskoCost,
                                item.KaskoCompanyName,
                                item.KaskoPolicyNumber,
                                item.IsKaskoExpired,
                                item.IsKaskoExpiringSoon
                            ));
                        }
                    }

                    foreach (var doc in docsToPrint)
                    {
                        var bg = rowIndex % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                        if (doc.IsExpired) bg = Colors.Red.Lighten5;

                        if (doc.Cost.HasValue && doc.Cost.Value > 0)
                        {
                            grandTotalCost += doc.Cost.Value;
                        }

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(rowIndex.ToString());
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.PlateNumber).Bold();
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(brandModel);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(doc.DocType).SemiBold();
                        
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(doc.StartDate != DateTime.MinValue ? doc.StartDate.ToString("dd.MM.yyyy") : "-");
                        
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(doc.EndDate != DateTime.MinValue ? doc.EndDate.ToString("dd.MM.yyyy") : "-").Bold();

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text =>
                        {
                            if (doc.EndDate == DateTime.MinValue)
                            {
                                text.Span("Kayıt Yok").FontColor(Colors.Grey.Darken1);
                            }
                            else if (doc.IsExpired)
                            {
                                text.Span($"{Math.Abs(doc.DaysRemaining)} gün geçti").Bold().FontColor(Colors.Red.Darken2);
                                text.Span(" (Süresi Dolmuş)").FontColor(Colors.Red.Darken2);
                            }
                            else if (doc.IsExpiringSoon)
                            {
                                text.Span($"{doc.DaysRemaining} gün kaldı").Bold().FontColor(Colors.Orange.Darken3);
                                text.Span(" (Kritik)").FontColor(Colors.Orange.Darken3);
                            }
                            else
                            {
                                text.Span($"{doc.DaysRemaining} gün").FontColor(Colors.Grey.Darken2);
                                text.Span(" (Geçerli)").FontColor(Colors.Green.Darken3);
                            }
                        });

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(!string.IsNullOrWhiteSpace(doc.Company) ? doc.Company : "-");

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(!string.IsNullOrWhiteSpace(doc.PolicyNo) ? doc.PolicyNo : "-");

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                            .Text(doc.Cost.HasValue && doc.Cost.Value > 0 ? $"{doc.Cost.Value:N2} ₺" : "-").Bold().FontColor(Colors.Green.Darken3);

                        rowIndex++;
                    }
                }

                // Dip Toplam Satırı
                table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text("TOPLAM TUTAR:").Bold().FontSize(9);

                table.Cell().Background(Colors.Grey.Lighten3).BorderTop(1.5f).BorderColor(Colors.Grey.Darken1).Padding(5).AlignRight()
                    .Text($"{grandTotalCost:N2} ₺").Bold().FontColor(Colors.Green.Darken4).FontSize(9);
            });

            // 3. ALT BİLGİ (FOOTER)
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
