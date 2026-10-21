// File: Services/Implementations/FaultReportDocument.cs
using aractakipsys.Models.ViewModels.FaultReport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class FaultReportDocument : IDocument
{
    private readonly FaultReportPdfModel _model;
    private static readonly byte[]? LogoBytes = LoadLogoBytes();

    public FaultReportDocument(FaultReportPdfModel model)
    {
        _model = model;
    }

    private static byte[]? LoadLogoBytes()
    {
        try
        {
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "duzce_logo.png");
            if (File.Exists(logoPath))
            {
                return File.ReadAllBytes(logoPath);
            }
        }
        catch
        {
            // Logo bulunamazsa doküman yer tutucu ile derlenir
        }
        return null;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(35);
            page.MarginVertical(30);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Grey.Darken4));

            // 1. BAŞLIK VE LOGO
            page.Header().Column(headerCol =>
            {
                headerCol.Item().Row(row =>
                {
                    // Sol Logo
                    if (LogoBytes != null)
                    {
                        row.ConstantItem(60).Height(50).Image(LogoBytes).FitArea();
                    }
                    else
                    {
                        row.ConstantItem(60).Height(50).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                            .AlignCenter().AlignMiddle().Text("DÜZCE ÜNV.").FontSize(8).Bold();
                    }

                    // Orta Başlık
                    row.RelativeItem().PaddingLeft(10).Column(titleCol =>
                    {
                        titleCol.Item().AlignCenter().Text("T.C.").Bold().FontSize(11).FontColor(Colors.Grey.Darken4);
                        titleCol.Item().AlignCenter().Text("DÜZCE ÜNİVERSİTESİ REKTÖRLÜĞÜ").Bold().FontSize(13).FontColor(Colors.Blue.Darken4);
                        titleCol.Item().AlignCenter().PaddingTop(2).Text("ARAÇ ARIZA BİLDİRİM FORMU").Bold().FontSize(12).FontColor(Colors.Grey.Darken4);
                    });

                    // Sağ Boşluk (Simetri için)
                    row.ConstantItem(60);
                });

                // Tarih Satırı
                headerCol.Item().PaddingTop(8).Row(row =>
                {
                    var dateText = _model.IsBlank 
                        ? ".... / .... / 20...." 
                        : (_model.ReportDate?.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"));

                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("TARİH : ").Bold().FontSize(9.5f);
                        t.Span(dateText).FontSize(9.5f);
                    });
                });

                headerCol.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Colors.Grey.Darken2);
            });

            // 2. GÖVDE VE TABLOLAR
            page.Content().PaddingTop(12).Column(col =>
            {
                // Bilgi Tablosu
                col.Item().Border(1).BorderColor(Colors.Grey.Darken2).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(160);
                        columns.RelativeColumn();
                    });

                    // Araç Plakası
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(6)
                        .Text("Araç Plakası :").Bold().FontSize(9.5f);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(6)
                        .Text(_model.IsBlank ? "" : _model.VehiclePlate).Bold().FontSize(10.5f).FontColor(Colors.Blue.Darken4);

                    // Şoförün Adı Soyadı
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(6)
                        .Text("Şoförün Adı Soyadı :").Bold().FontSize(9.5f);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(6)
                        .Text(_model.IsBlank ? "" : _model.DriverFullName).FontSize(9.5f);

                    // Arıza Bildirim Tarihi
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(6)
                        .Text("Arıza Bildirim Tarihi :").Bold().FontSize(9.5f);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(6)
                        .Text(_model.IsBlank ? "" : (_model.ReportDate?.ToString("dd.MM.yyyy HH:mm") ?? "-")).FontSize(9.5f);

                    // Araç KM
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(6)
                        .Text("Araç KM :").Bold().FontSize(9.5f);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(6)
                        .Text(_model.IsBlank ? "" : (_model.CurrentKm.HasValue ? $"{_model.CurrentKm.Value:N0} KM" : "-")).Bold().FontSize(9.5f);
                });

                // Arıza Açıklaması Alanı (Geniş Çerçeveli Çok Satırlı Kutu)
                col.Item().PaddingTop(12).Border(1).BorderColor(Colors.Grey.Darken2).Column(box =>
                {
                    box.Item().Background(Colors.Grey.Lighten4).Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Darken2)
                        .Text("ARIZA AÇIKLAMASI").Bold().FontSize(9.5f);

                    var descText = _model.IsBlank ? "" : _model.Description;
                    box.Item().MinHeight(130).Padding(8).Text(descText).FontSize(9.5f);
                });

                // Bilgilendirme / İbraz Notu
                col.Item().PaddingTop(10).Text(
                    "Yukarıda plakası ve kilometresi belirtilen kurum taşıtında tespit edilen arıza / hasar durumu ilgililerin bilgisine arz olunur."
                ).Italic().FontSize(8.5f).FontColor(Colors.Grey.Darken3);

                // 3. İMZA ALANLARI (Yan yana iki kutu)
                col.Item().PaddingTop(25).Row(signRow =>
                {
                    signRow.Spacing(30);

                    // Sol Kutu: Ulaştırma Amiri
                    signRow.RelativeItem().Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(leftSign =>
                    {
                        leftSign.Item().AlignCenter().Text("ULAŞTIRMA AMİRİ").Bold().FontSize(9.5f);
                        var revName = (!_model.IsBlank && 
                                       !string.IsNullOrWhiteSpace(_model.ReviewerFullName) && 
                                       _model.ReviewerFullName != "Ulaştırma Amiri" && 
                                       !Guid.TryParse(_model.ReviewerFullName.Trim(), out _))
                            ? _model.ReviewerFullName.Trim()
                            : "........................................";

                        leftSign.Item().PaddingTop(4).AlignCenter().Text($"Adı Soyadı: {revName}").FontSize(9);
                        leftSign.Item().PaddingTop(25).AlignCenter().Text("İmzası: ........................................").FontSize(9);
                    });

                    // Sağ Kutu: Şoför
                    signRow.RelativeItem().Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(rightSign =>
                    {
                        rightSign.Item().AlignCenter().Text("ŞOFÖRÜN").Bold().FontSize(9.5f);
                        var drvName = (!_model.IsBlank && 
                                       !string.IsNullOrWhiteSpace(_model.DriverFullName) && 
                                       _model.DriverFullName != "-" && 
                                       !Guid.TryParse(_model.DriverFullName.Trim(), out _))
                            ? _model.DriverFullName.Trim()
                            : "........................................";

                        rightSign.Item().PaddingTop(4).AlignCenter().Text($"Adı Soyadı: {drvName}").FontSize(9);
                        rightSign.Item().PaddingTop(25).AlignCenter().Text("İmzası: ........................................").FontSize(9);
                    });
                });
            });

            // 4. SAYFA ALTI (FOOTER)
            page.Footer().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    t.Span("Düzce Üniversitesi Araç Takip Sistemi  |  Arıza Bildirim Formu");
                    if (!_model.IsBlank && _model.Id > 0)
                    {
                        t.Span($"  |  Kayıt No: #{_model.Id:D5}");
                    }
                });

                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2));
                    t.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });
            });
        });
    }
}
