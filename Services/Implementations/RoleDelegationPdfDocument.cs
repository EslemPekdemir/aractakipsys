// File: Services/Implementations/RoleDelegationPdfDocument.cs
using aractakipsys.Models.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class RoleDelegationPdfDocument : IDocument
{
    private readonly RoleDelegation _delegation;
    private static readonly byte[]? LogoBytes = LoadLogoBytes();

    public RoleDelegationPdfDocument(RoleDelegation delegation)
    {
        _delegation = delegation;
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
            // Logo yüklenemezse null döner, metin başlık kullanılır
        }
        return null;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(ComposeSinglePage);
    }

    private void ComposeSinglePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(30);
        page.MarginVertical(25);
        page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Arial").FontColor(Colors.Black));

        page.Content().Column(pageCol =>
        {
            pageCol.Item().Border(1.2f).BorderColor(Colors.Black).Column(formCol =>
            {
                // 1. BÖLÜM: ÜSTTE LOGO VE BAŞLIK
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Row(row =>
                {
                    if (LogoBytes != null)
                    {
                        row.ConstantItem(85).Height(62).Padding(4).Image(LogoBytes).FitArea();
                    }
                    else
                    {
                        row.ConstantItem(85).Height(62).Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                            .AlignCenter().AlignMiddle().Text("DÜZCE ÜNV.").FontSize(8).Bold();
                    }

                    row.RelativeItem().PaddingVertical(5).AlignCenter().AlignMiddle().Column(tCol =>
                    {
                        tCol.Item().AlignCenter().Text("T.C.").Bold().FontSize(10.5f);
                        tCol.Item().AlignCenter().Text("DÜZCE ÜNİVERSİTESİ REKTÖRLÜĞÜ").Bold().FontSize(12.5f);
                        tCol.Item().AlignCenter().Text("YETKİ DEVRİ VE VEKÂLET BELGESİ").Bold().FontSize(12f);
                    });

                    row.ConstantItem(85);
                });

                // SAYI VE TARİH SATIRI
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingHorizontal(10).PaddingVertical(5).Row(row =>
                {
                    var sayiStr = _delegation.Id.ToString("D5");
                    var tarihStr = _delegation.CreatedAt.ToString("dd.MM.yyyy");

                    row.RelativeItem().Text(t =>
                    {
                        t.Span("BELGE NO: ").Bold().FontSize(9f);
                        t.Span($"B.30.2.DZC.0.73.00/YD-{sayiStr}").FontSize(9f);
                    });

                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("DÜZENLEME TARİHİ: ").Bold().FontSize(9f);
                        t.Span(tarihStr).FontSize(9f);
                    });
                });

                // YASAL DAYANAK SATIRI (DÜZELTME 10: Uydurma metin yok, nokta nokta doldurulacak alan)
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingHorizontal(10).PaddingVertical(6).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("DAYANAK: ").Bold().FontSize(9f);
                        t.Span(".....................................................................................................................................................................").FontSize(9f);
                    });
                });

                // TARAFLAR TABLOSU
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                    });

                    // Başlıklar
                    table.Cell().BorderRight(1).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3)
                        .Padding(6).AlignCenter().Text("GÖREVİ DEVREDEN (ASIL YETKİLİ)").Bold().FontSize(9.5f);

                    table.Cell().BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3)
                        .Padding(6).AlignCenter().Text("GÖREVİ DEVRALAN (VEKİL)").Bold().FontSize(9.5f);

                    // Bilgiler
                    table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(8).Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Adı Soyadı: ").Bold(); t.Span(_delegation.DelegatorUser?.FullName ?? "-"); });
                        c.Item().PaddingTop(3).Text(t => { t.Span("Görevi/Unvanı: ").Bold(); t.Span("Ulaşım Birim Şefi"); });
                        c.Item().PaddingTop(3).Text(t => { t.Span("E-Posta: ").Bold(); t.Span(_delegation.DelegatorUser?.Email ?? "-"); });
                    });

                    table.Cell().BorderColor(Colors.Black).Padding(8).Column(c =>
                    {
                        var originalRoleTitle = _delegation.OriginalDelegateeRole switch
                        {
                            "Driver" => "Şoför (Saha Personeli)",
                            "Kullanici" => "Ulaşım Şefi",
                            _ => _delegation.OriginalDelegateeRole
                        };

                        c.Item().Text(t => { t.Span("Adı Soyadı: ").Bold(); t.Span(_delegation.DelegateeUser?.FullName ?? "-"); });
                        c.Item().PaddingTop(3).Text(t => { t.Span("Asıl Unvanı: ").Bold(); t.Span(originalRoleTitle); });
                        c.Item().PaddingTop(3).Text(t => { t.Span("Devredilen Rol: ").Bold(); t.Span("Ulaşım Birim Şefi (Geçici Vekâlet)"); });
                    });
                });

                // VEKÂLET DETAYLARI
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(10).Column(c =>
                {
                    c.Item().Text("VEKÂLET BİLGİLERİ VE KAPSAMI").Bold().FontSize(10f);
                    c.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Başlangıç Tarih / Saat: ").Bold();
                            t.Span(_delegation.StartDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                        });

                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Bitiş Tarih / Saat: ").Bold();
                            t.Span(_delegation.EndDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                        });
                    });

                    c.Item().PaddingTop(8).Text(t =>
                    {
                        t.Span("Devir Gerekçesi / Açıklama: ").Bold();
                        t.Span(_delegation.Reason);
                    });

                    if (_delegation.ActualRevokedAt.HasValue)
                    {
                        c.Item().PaddingTop(8).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(4).Text(t =>
                        {
                            t.Span("Erken Sonlandırma (İptal): ").Bold().FontColor(Colors.Red.Medium);
                            t.Span($"{_delegation.ActualRevokedAt.Value.ToLocalTime():dd.MM.yyyy HH:mm} tarihinde sonlandırılmıştır. Gerekçe: {_delegation.RevokeReason ?? "-"}");
                        });
                    }
                });

                // HÜKÜMLER VE TALİMATLAR
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(10).Column(c =>
                {
                    c.Item().Text("UYGULAMA ESASLARI VE YÜKÜMLÜLÜKLER:").Bold().FontSize(9f);
                    c.Item().PaddingTop(4).Text("1. Görevi devralan vekil personel, yetki devri süresince Ulaşım Şefliği görev, yetki ve sorumluluklarını kurum mevzuatına uygun şekilde yürütür.")
                        .FontSize(8.5f);
                    c.Item().PaddingTop(2).Text("2. Devralan personelin şoför olması durumunda; vekâlet süresi boyunca adına yeni taşıt görevi yazılamaz ve saha görevleri icra edemez.")
                        .FontSize(8.5f);
                    c.Item().PaddingTop(2).Text("3. Yetki devri süresi sona erdiğinde veya asıl görevlinin göreve dönmesiyle yapılan devir sonlandırma işleminde yetkiler kendiliğinden sona erer.")
                        .FontSize(8.5f);
                    c.Item().PaddingTop(2).Text("4. Vekâlet süresi boyunca sistem üzerinde gerçekleştirilen tüm işlemler denetim günlüğünde (AuditLog) vekâleten işlem olarak kayıt altına alınır.")
                        .FontSize(8.5f);
                });

                // İMZA KUTULARI (DÜZELTME 10: Görevi Devreden, Görevi Devralan)
                formCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                    });

                    table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(12).Column(c =>
                    {
                        c.Item().AlignCenter().Text("GÖREVİ DEVREDEN").Bold().FontSize(9.5f);
                        c.Item().AlignCenter().Text(_delegation.DelegatorUser?.FullName ?? "").FontSize(9f);
                        c.Item().AlignCenter().Text("Ulaşım Birim Şefi").FontSize(8.5f);
                        c.Item().Height(40);
                        c.Item().AlignCenter().Text("İmza: .......................................").FontSize(8.5f);
                    });

                    table.Cell().BorderColor(Colors.Black).Padding(12).Column(c =>
                    {
                        c.Item().AlignCenter().Text("GÖREVİ DEVRALAN").Bold().FontSize(9.5f);
                        c.Item().AlignCenter().Text(_delegation.DelegateeUser?.FullName ?? "").FontSize(9f);
                        c.Item().AlignCenter().Text("Vekil").FontSize(8.5f);
                        c.Item().Height(40);
                        c.Item().AlignCenter().Text("İmza: .......................................").FontSize(8.5f);
                    });
                });
            });
        });
    }
}
