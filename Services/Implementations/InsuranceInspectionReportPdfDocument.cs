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
                _ => "Bilgi İşlem Daire Başkanlığı — Sigorta ve Muayene Durum Raporu"
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
            page.Content().PaddingVertical(8).Table(table =>
            {
                if (scope == ReportScopeType.Insurance)
                {
                    // Sadece Trafik Sigortası
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);   // #
                        columns.RelativeColumn(2.0f); // Plaka
                        columns.RelativeColumn(3.0f); // Marka / Model
                        columns.RelativeColumn(2.3f); // Sigorta Başlangıç
                        columns.RelativeColumn(2.3f); // Sigorta Bitiş
                        columns.RelativeColumn(2.8f); // Kalan Gün / Durum
                        columns.RelativeColumn(2.0f); // Güncel Durum
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("#").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Plaka").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Marka / Model").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Sigorta Başlangıç").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Sigorta Bitiş").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Kalan Gün / Durum").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Güncel Durum").Bold();
                    });

                    int index = 1;
                    foreach (var item in _model.Items)
                    {
                        var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                        if (item.IsInsuranceExpired) bg = Colors.Red.Lighten5;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(index.ToString());
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.PlateNumber).Bold();
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{item.BrandModel} ({item.Year})");
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.InsuranceStartDate.ToString("dd.MM.yyyy"));
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.InsuranceExpiryDate.ToString("dd.MM.yyyy")).Bold();

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text =>
                        {
                            if (item.IsInsuranceExpired)
                            {
                                text.Span($"{Math.Abs(item.InsuranceDaysRemaining)} gün geçti").Bold().FontColor(Colors.Red.Darken2);
                                text.Span(" (Süresi Dolmuş)").FontColor(Colors.Red.Darken2);
                            }
                            else if (item.IsInsuranceExpiringSoon)
                            {
                                text.Span($"{item.InsuranceDaysRemaining} gün kaldı").Bold().FontColor(Colors.Orange.Darken3);
                                text.Span(" (Kritik)").FontColor(Colors.Orange.Darken3);
                            }
                            else
                            {
                                text.Span($"{item.InsuranceDaysRemaining} gün").FontColor(Colors.Grey.Darken2);
                                text.Span(" (Geçerli)").FontColor(Colors.Green.Darken3);
                            }
                        });

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.StatusDisplayName);
                        index++;
                    }
                }
                else if (scope == ReportScopeType.Inspection)
                {
                    // Sadece Fenni Muayene
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);   // #
                        columns.RelativeColumn(2.0f); // Plaka
                        columns.RelativeColumn(3.0f); // Marka / Model
                        columns.RelativeColumn(2.3f); // Muayene Başlangıç
                        columns.RelativeColumn(2.3f); // Muayene Bitiş
                        columns.RelativeColumn(2.8f); // Kalan Gün / Durum
                        columns.RelativeColumn(2.0f); // Güncel Durum
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("#").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Plaka").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Marka / Model").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Muayene Başlangıç").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Muayene Bitiş").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Kalan Gün / Durum").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Güncel Durum").Bold();
                    });

                    int index = 1;
                    foreach (var item in _model.Items)
                    {
                        var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                        if (item.IsInspectionExpired) bg = Colors.Red.Lighten5;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(index.ToString());
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.PlateNumber).Bold();
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{item.BrandModel} ({item.Year})");
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.InspectionStartDate.ToString("dd.MM.yyyy"));
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.InspectionExpiryDate.ToString("dd.MM.yyyy")).Bold();

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text =>
                        {
                            if (item.IsInspectionExpired)
                            {
                                text.Span($"{Math.Abs(item.InspectionDaysRemaining)} gün geçti").Bold().FontColor(Colors.Red.Darken2);
                                text.Span(" (Süresi Dolmuş)").FontColor(Colors.Red.Darken2);
                            }
                            else if (item.IsInspectionExpiringSoon)
                            {
                                text.Span($"{item.InspectionDaysRemaining} gün kaldı").Bold().FontColor(Colors.Orange.Darken3);
                                text.Span(" (Kritik)").FontColor(Colors.Orange.Darken3);
                            }
                            else
                            {
                                text.Span($"{item.InspectionDaysRemaining} gün").FontColor(Colors.Grey.Darken2);
                                text.Span(" (Geçerli)").FontColor(Colors.Green.Darken3);
                            }
                        });

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.StatusDisplayName);
                        index++;
                    }
                }
                else
                {
                    // Tümü (Sigorta ve Muayene)
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);   // #
                        columns.RelativeColumn(1.8f); // Plaka
                        columns.RelativeColumn(2.5f); // Marka / Model
                        columns.RelativeColumn(3.8f); // Trafik Sigortası (Başlangıç - Bitiş)
                        columns.RelativeColumn(3.8f); // Fenni Muayene (Başlangıç - Bitiş)
                        columns.RelativeColumn(2.0f); // Güncel Durum
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("#").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Plaka").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Marka / Model").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Trafik Sigortası (Başlangıç - Bitiş)").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Fenni Muayene (Başlangıç - Bitiş)").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Güncel Durum").Bold();
                    });

                    int index = 1;
                    foreach (var item in _model.Items)
                    {
                        var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                        if (item.HasAnyExpired) bg = Colors.Red.Lighten5;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(index.ToString());
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.PlateNumber).Bold();
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{item.BrandModel} ({item.Year})");

                        // Sigorta (Başlangıç + Bitiş & Kalan Gün)
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(col =>
                        {
                            col.Item().Text(text =>
                            {
                                text.Span("Başlangıç: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                text.Span(item.InsuranceStartDate.ToString("dd.MM.yyyy")).FontSize(8);
                            });
                            col.Item().Text(text =>
                            {
                                text.Span("Bitiş: ").Bold();
                                text.Span(item.InsuranceExpiryDate.ToString("dd.MM.yyyy")).Bold();
                                if (item.IsInsuranceExpired)
                                {
                                    text.Span($" ({Math.Abs(item.InsuranceDaysRemaining)} gün geçti)").Bold().FontColor(Colors.Red.Darken2);
                                }
                                else if (item.IsInsuranceExpiringSoon)
                                {
                                    text.Span($" ({item.InsuranceDaysRemaining} gün kaldı)").Bold().FontColor(Colors.Orange.Darken3);
                                }
                                else
                                {
                                    text.Span($" ({item.InsuranceDaysRemaining} gün)").FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });

                        // Muayene (Başlangıç + Bitiş & Kalan Gün)
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(col =>
                        {
                            col.Item().Text(text =>
                            {
                                text.Span("Başlangıç: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                text.Span(item.InspectionStartDate.ToString("dd.MM.yyyy")).FontSize(8);
                            });
                            col.Item().Text(text =>
                            {
                                text.Span("Bitiş: ").Bold();
                                text.Span(item.InspectionExpiryDate.ToString("dd.MM.yyyy")).Bold();
                                if (item.IsInspectionExpired)
                                {
                                    text.Span($" ({Math.Abs(item.InspectionDaysRemaining)} gün geçti)").Bold().FontColor(Colors.Red.Darken2);
                                }
                                else if (item.IsInspectionExpiringSoon)
                                {
                                    text.Span($" ({item.InspectionDaysRemaining} gün kaldı)").Bold().FontColor(Colors.Orange.Darken3);
                                }
                                else
                                {
                                    text.Span($" ({item.InspectionDaysRemaining} gün)").FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });

                        // Durum
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(item.StatusDisplayName);

                        index++;
                    }
                }
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
