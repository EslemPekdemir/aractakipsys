// File: Services/Implementations/AssignmentOrderDocument.cs
using aractakipsys.Models.ViewModels.Assignment;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace aractakipsys.Services.Implementations;

public class AssignmentOrderDocument : IDocument
{
    private readonly AssignmentDetailsViewModel _model;
    private readonly string? _generatedByUserName;
    private readonly bool _emptyValues;
    private readonly List<string>? _additionalDrivers;
    private readonly string? _routeInfo;
    private readonly string? _specificDriverFullName;
    private static readonly byte[]? LogoBytes = LoadLogoBytes();

    public AssignmentOrderDocument(
        AssignmentDetailsViewModel model, 
        string? generatedByUserName, 
        bool emptyValues = false,
        List<string>? additionalDrivers = null,
        string? routeInfo = null,
        string? specificDriverFullName = null)
    {
        _model = model;
        _emptyValues = emptyValues;
        _additionalDrivers = additionalDrivers;
        _routeInfo = routeInfo;
        _specificDriverFullName = FilterPersonName(specificDriverFullName);

        var candidate = !string.IsNullOrWhiteSpace(generatedByUserName) 
            ? generatedByUserName 
            : _model.CreatedByUserName;

        _generatedByUserName = FilterPersonName(candidate);
    }

    private static string? FilterPersonName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var trimmed = name.Trim();
        // Hiçbir koşulda kimlik (GUID vb.) basılmasın; kullanıcı adı soyadı bulunamazsa boş kalsın
        if (Guid.TryParse(trimmed, out _)) return null;
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[0-9a-fA-F\-]{30,}$")) return null;
        return trimmed;
    }

    private static string CleanPurpose(string? rawPurpose)
    {
        if (string.IsNullOrWhiteSpace(rawPurpose)) return "";
        // Resmi belgede şef notu hiçbir yerde yer almaz; görevin konusundan tamamen temizlenir
        // 1. Parantez içindeki şef notunu temizle: (Şef Notu: ...)
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            rawPurpose,
            @"\s*\(\s*Şef\s+Notu\s*:[^\)]*\)",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        // 2. Parantezsiz şef notu varsa satır sonuna kadar temizle: Şef Notu: ...
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s*Şef\s+Notu\s*:[^\r\n]*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        return cleaned.Trim();
    }

    private (string? DispatcherText, string? ReasonText) GetOffRouteDispatchDisplay()
    {
        // "Boş çıktı": kutu HER ZAMAN boş (ıslak imza için)
        // "Dolu çıktı": Görevde Güzergah Dışı Sevk Kaydı varsa doldurulur; yoksa boş kalır
        // Kural: Şoför güzergah değişiklikleri bu kutuya ASLA yazılmaz (yalnızca sol taraftaki güzergah alanında kalır)
        if (_emptyValues || !_model.HasOffRouteDispatch)
        {
            return (null, null);
        }

        var dispatcherName = _model.OffRouteDispatcherName?.Trim() ?? "";
        var dispatcherTitle = _model.OffRouteDispatcherTitle?.Trim() ?? "";
        var rawReason = _model.OffRouteReason?.Trim() ?? "";

        // Yetkili Adı Soyadı ve Unvanı
        string? dispatcherText = null;
        if (!string.IsNullOrWhiteSpace(dispatcherName) && !string.IsNullOrWhiteSpace(dispatcherTitle))
        {
            dispatcherText = $"{dispatcherName} ({dispatcherTitle})";
        }
        else if (!string.IsNullOrWhiteSpace(dispatcherName))
        {
            dispatcherText = dispatcherName;
        }
        else if (!string.IsNullOrWhiteSpace(dispatcherTitle))
        {
            dispatcherText = dispatcherTitle;
        }

        // Sevk Nedeni
        string? reasonText = null;
        if (!string.IsNullOrWhiteSpace(rawReason))
        {
            // Şablondaki "Sevk Nedeni:" başlığıyla mükerrer olmaması için baştaki "Gerekçe:" ön ekini temizle
            reasonText = System.Text.RegularExpressions.Regex.Replace(rawReason, @"^\s*Gerekçe\s*:\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        }

        return (
            string.IsNullOrWhiteSpace(dispatcherText) ? null : dispatcherText,
            string.IsNullOrWhiteSpace(reasonText) ? null : reasonText
        );
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
            // Logo bulunamazsa yer tutucu kullanılır
        }
        return null;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        // Tek sayfa (A4 dikey)
        container.Page(ComposeSinglePage);
    }

    private void ComposeSinglePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(24);
        page.MarginVertical(20);
        page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Arial").FontColor(Colors.Black));

        // TÜM BELGE TEK DIŞ ÇERÇEVE İÇİNDE (TÜM HÜCRELER ÇİZGİLİ)
        page.Content().Column(pageCol =>
        {
            pageCol.Item().Border(1.2f).BorderColor(Colors.Black).Column(formCol =>
            {
                // 1. BÖLÜM: ÜSTTE BÜYÜK LOGO VE BAŞLIK
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Row(row =>
                {
                    // Sol üstte büyük logo
                    if (LogoBytes != null)
                    {
                        row.ConstantItem(85).Height(62).Padding(4).Image(LogoBytes).FitArea();
                    }
                    else
                    {
                        row.ConstantItem(85).Height(62).Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                            .AlignCenter().AlignMiddle().Text("DÜZCE ÜNV.").FontSize(8).Bold();
                    }

                    // Ortada "T.C." / "DÜZCE ÜNİVERSİTESİ REKTÖRLÜĞÜ" / "TAŞIT GÖREV EMRİ"
                    row.RelativeItem().PaddingVertical(5).AlignCenter().AlignMiddle().Column(tCol =>
                    {
                        tCol.Item().AlignCenter().Text("T.C.").Bold().FontSize(10.5f);
                        tCol.Item().AlignCenter().Text("DÜZCE ÜNİVERSİTESİ REKTÖRLÜĞÜ").Bold().FontSize(12.5f);
                        tCol.Item().AlignCenter().Text("TAŞIT GÖREV EMRİ").Bold().FontSize(12f);
                    });

                    // Sağ Simetri Boşluğu
                    row.ConstantItem(85);
                });

                // SAYI VE TARİH SATIRI
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingHorizontal(8).PaddingVertical(4).Row(row =>
                {
                    var sayiStr = _emptyValues ? "............" : _model.Id.ToString("D5");
                    var tarihStr = _emptyValues 
                        ? "....../....../20....." 
                        : (_model.PlannedStartTime != default ? _model.PlannedStartTime.ToString("dd.MM.yyyy") : DateTime.Now.ToString("dd.MM.yyyy"));

                    row.RelativeItem().Text(t =>
                    {
                        t.Span("SAYI: ").Bold().FontSize(8.5f);
                        t.Span($"B.30.2.DZC.0.73.00/{sayiStr}").FontSize(8.5f);
                    });

                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("TARİH: ").Bold().FontSize(8.5f);
                        t.Span(tarihStr).FontSize(8.5f);
                    });
                });

                // 2. BÖLÜM: İKİ EŞİT SÜTUNLU BLOK (ARACI GÖREVLENDİRENİN | ARACI SEVK EDENİN)
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(7); // Sol: Adı Soyadı ve Unvanı
                        cols.RelativeColumn(3); // Sol: İmzası
                        cols.RelativeColumn(7); // Sağ: Adı Soyadı ve Unvanı
                        cols.RelativeColumn(3); // Sağ: İmzası
                    });

                    // Başlık Satırı
                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).BorderRight(1).BorderBottom(1).BorderColor(Colors.Black)
                        .Padding(3).AlignCenter().Text("ARACI GÖREVLENDİRENİN").Bold().FontSize(8.5f);
                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black)
                        .Padding(3).AlignCenter().Text("ARACI SEVK EDENİN").Bold().FontSize(8.5f);

                    // Alt Başlık Satırı
                    table.Cell().BorderRight(1).BorderBottom(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Adı Soyadı ve Unvanı").Bold().FontSize(7.5f);
                    table.Cell().BorderRight(1).BorderBottom(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("İmzası").Bold().FontSize(7.5f);
                    table.Cell().BorderRight(1).BorderBottom(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Adı Soyadı ve Unvanı").Bold().FontSize(7.5f);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("İmzası").Bold().FontSize(7.5f);

                    // Veri / İmza Boşluğu Satırı
                    var creatorDisplay = (!_emptyValues && !string.IsNullOrWhiteSpace(_generatedByUserName)) ? _generatedByUserName : "";
                    table.Cell().BorderRight(1).BorderColor(Colors.Black).MinHeight(38).Padding(4).AlignCenter().AlignMiddle()
                        .Text(creatorDisplay).FontSize(8.5f);
                    table.Cell().BorderRight(1).BorderColor(Colors.Black).MinHeight(38).Padding(4); // İmza alanı
                    table.Cell().BorderRight(1).BorderColor(Colors.Black).MinHeight(38).Padding(4); // Sevk eden (boş)
                    table.Cell().MinHeight(38).Padding(4); // Sevk eden imza (boş)
                });

                // 3. BÖLÜM: ALT BÖLÜM (İKİ SÜTUNLU: SOL GÖREVE AİT, SAĞ GÜZERGAH DIŞI + ARACA AİT)
                // Görevde güzergah değişikliği yapılmış olsa bile sol sütuna görevin İLK (orijinal) güzergahı basılır
                string initialDestination;
                string initialRoute;

                var earliestChange = _model.RouteChanges?
                    .OrderBy(rc => rc.CreatedAt)
                    .FirstOrDefault();

                if (earliestChange != null)
                {
                    initialDestination = !string.IsNullOrWhiteSpace(earliestChange.OldDestination) 
                        ? earliestChange.OldDestination 
                        : _model.Destination;

                    initialRoute = !string.IsNullOrWhiteSpace(earliestChange.OldRoute) 
                        ? earliestChange.OldRoute 
                        : _model.Route;
                }
                else
                {
                    initialDestination = _model.Destination;
                    initialRoute = _model.Route;
                }

                var destination = !_emptyValues 
                    ? (!string.IsNullOrWhiteSpace(initialDestination) 
                        ? initialDestination 
                        : (!string.IsNullOrWhiteSpace(_routeInfo) 
                            ? _routeInfo 
                            : (!string.IsNullOrWhiteSpace(_model.RequestedByDepartment) ? _model.RequestedByDepartment : "Belirtilen Görev Yeri")))
                    : "";

                var routeText = !_emptyValues
                    ? (!string.IsNullOrWhiteSpace(initialRoute) 
                        ? initialRoute 
                        : (!string.IsNullOrWhiteSpace(_routeInfo) ? _routeInfo : "Belirtilen Güzergah"))
                    : "";

                string driverDisplay;
                if (_emptyValues)
                {
                    driverDisplay = "";
                }
                else if (_model.IsDriverless)
                {
                    driverDisplay = "Şoförsüz Görevlendirme";
                }
                else if (_model.IsExternalDriver)
                {
                    driverDisplay = string.IsNullOrWhiteSpace(_model.ExternalDriverDepartment)
                        ? (_model.ExternalDriverFullName ?? "Dış Şoför")
                        : $"{_model.ExternalDriverFullName} ({_model.ExternalDriverDepartment})";
                }
                else if (!string.IsNullOrWhiteSpace(_specificDriverFullName))
                {
                    driverDisplay = _specificDriverFullName;
                }
                else
                {
                    driverDisplay = _model.DriverFullName;
                    if (_additionalDrivers != null && _additionalDrivers.Count > 0)
                    {
                        driverDisplay += " (Ek Şoförler: " + string.Join(", ", _additionalDrivers) + ")";
                    }
                }

                // ÇIKIŞ VE GİRİŞ BİLGİLERİ (KM VE YALNIZCA SAAT)
                var showActuals = !_emptyValues && _model.Status != aractakipsys.Models.Enums.AssignmentStatus.Planned;
                var outKm = (showActuals && _model.StartKm.HasValue) ? $"{_model.StartKm.Value:N0} KM" : "....................";
                var inKm = (showActuals && _model.EndKm.HasValue) ? $"{_model.EndKm.Value:N0} KM" : "....................";

                var outTime = (showActuals && _model.ActualStartTime.HasValue) 
                    ? _model.ActualStartTime.Value.ToString("HH:mm") 
                    : (!_emptyValues && _model.PlannedStartTime != default ? _model.PlannedStartTime.ToString("HH:mm") : "....................");

                var inTime = (showActuals && _model.ActualEndTime.HasValue) 
                    ? _model.ActualEndTime.Value.ToString("HH:mm") 
                    : (!_emptyValues && _model.PlannedEndTime != default ? _model.PlannedEndTime.ToString("HH:mm") : "....................");

                var (offRouteDispatcher, offRouteReason) = GetOffRouteDispatchDisplay();

                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Table(mainTable =>
                {
                    mainTable.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(); // Sol Sütun
                        cols.RelativeColumn(); // Sağ Sütun
                    });

                    // SOL SÜTUN
                    mainTable.Cell().BorderRight(1).BorderColor(Colors.Black).Column(leftCol =>
                    {
                        // Başlık: GÖREVE AİT BİLGİLER
                        leftCol.Item().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black)
                            .Padding(4).AlignCenter().Text("GÖREVE AİT BİLGİLER").Bold().FontSize(8.5f);

                        // Gidilen Yer Satırı
                        leftCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                        {
                            t.Span("Gidilen Yer: ").Bold().FontSize(8.5f);
                            t.Span(destination).FontSize(8.5f);
                        });

                        // Güzergah Satırı (Faz 2-A)
                        leftCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                        {
                            t.Span("Güzergah: ").Bold().FontSize(8.5f);
                            t.Span(routeText).FontSize(8.5f);
                        });

                        // Görevin Konusu Kutusu (sayfa oranını korumak için 165 min height)
                        leftCol.Item().BorderBottom(1).BorderColor(Colors.Black).MinHeight(165).Padding(4).Column(kCol =>
                        {
                            kCol.Item().Text("Görevin Konusu:").Bold().FontSize(8.5f);
                            var cleanedPurpose = !_emptyValues ? CleanPurpose(_model.Purpose) : "";
                            if (!string.IsNullOrWhiteSpace(cleanedPurpose))
                            {
                                kCol.Item().PaddingTop(4).Text(cleanedPurpose).FontSize(8.5f);
                            }
                        });

                        // En Altta: Görevin Türü | Resmi
                        leftCol.Item().Padding(4).Text(t =>
                        {
                            t.Span("Görevin Türü : ").Bold().FontSize(8.5f);
                            t.Span("Resmi").FontSize(8.5f);
                        });
                    });

                    // SAĞ SÜTUN (YUKARIDAN AŞAĞI)
                    mainTable.Cell().Column(rightCol =>
                    {
                        // 1. Başlık: ACİL DURUMLARDA ARACI GÜZERGAH DIŞINA SEVK EDENİN
                        rightCol.Item().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black)
                            .Padding(4).AlignCenter().Text("ACİL DURUMLARDA ARACI GÜZERGAH DIŞINA SEVK EDENİN").Bold().FontSize(7.5f);

                        // 2. Adı Soyadı, Unvanı ve İmzası (Dolu çıktıda kayıt varsa basılır, imza alanı boş kalır; boş çıktıda her zaman boş)
                        rightCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).MinHeight(32).Text(t =>
                        {
                            t.Span("Adı Soyadı, Unvanı ve İmzası:").Bold().FontSize(8f);
                            if (!string.IsNullOrWhiteSpace(offRouteDispatcher))
                            {
                                t.Span(" ");
                                t.Span(offRouteDispatcher).FontSize(8.5f);
                            }
                        });

                        // 3. Sevk Nedeni (Dolu çıktıda kayıt varsa basılır; boş çıktıda her zaman boş)
                        rightCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).MinHeight(32).Text(t =>
                        {
                            t.Span("Sevk Nedeni:").Bold().FontSize(8f);
                            if (!string.IsNullOrWhiteSpace(offRouteReason))
                            {
                                t.Span(" ");
                                t.Span(offRouteReason).FontSize(8.5f);
                            }
                        });

                        // 4. Başlık: ARACA AİT BİLGİLER
                        rightCol.Item().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black)
                            .Padding(4).AlignCenter().Text("ARACA AİT BİLGİLER").Bold().FontSize(8.5f);

                        // 5. ŞOFÖRÜN ADI SOYADI
                        rightCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                        {
                            t.Span("ŞOFÖRÜN ADI SOYADI: ").Bold().FontSize(8f);
                            t.Span(driverDisplay).FontSize(8.5f);
                        });

                        // 6. İMZASI (BOŞ İMZA ALANI)
                        rightCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).MinHeight(32).Text("İMZASI:").Bold().FontSize(8f);

                        // 7. ARACIN PLAKASI
                        rightCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                        {
                            t.Span("ARACIN PLAKASI: ").Bold().FontSize(8f);
                            if (!_emptyValues) t.Span(_model.VehiclePlate).Bold().FontSize(8.5f);
                        });

                        // 8. MARKA/MODELİ
                        rightCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                        {
                            t.Span("MARKA/MODELİ: ").Bold().FontSize(8f);
                            if (!_emptyValues) t.Span(_model.VehicleBrandModel).FontSize(8.5f);
                        });

                        // 9. ÇIKIŞ | GİRİŞ İKİ SÜTUNU (KM VE SAAT NOKTALI ÇİZGİLİ ALANLAR)
                        rightCol.Item().Table(moveTable =>
                        {
                            moveTable.ColumnsDefinition(mCols =>
                            {
                                mCols.RelativeColumn();
                                mCols.RelativeColumn();
                            });

                            // Başlık: ÇIKIŞ | GİRİŞ
                            moveTable.Cell().Background(Colors.Grey.Lighten3).BorderRight(1).BorderBottom(1).BorderColor(Colors.Black)
                                .Padding(3).AlignCenter().Text("ÇIKIŞ").Bold().FontSize(8.5f);
                            moveTable.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black)
                                .Padding(3).AlignCenter().Text("GİRİŞ").Bold().FontSize(8.5f);

                            // KM Satırı
                            moveTable.Cell().BorderRight(1).BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                            {
                                t.Span("KM: ").Bold().FontSize(8f);
                                t.Span(outKm).FontSize(8f);
                            });
                            moveTable.Cell().BorderBottom(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                            {
                                t.Span("KM: ").Bold().FontSize(8f);
                                t.Span(inKm).FontSize(8f);
                            });

                            // SAAT Satırı (Yalnızca SAAT)
                            moveTable.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(4).Text(t =>
                            {
                                t.Span("SAAT: ").Bold().FontSize(8f);
                                t.Span(outTime).FontSize(8f);
                            });
                            moveTable.Cell().Padding(4).Text(t =>
                            {
                                t.Span("SAAT: ").Bold().FontSize(8f);
                                t.Span(inTime).FontSize(8f);
                            });
                        });
                    });
                });

                // 4. BÖLÜM: ALTA ÜÇ MADDELİK NOT METNİ (KUTU BAŞLIĞI OLMADAN DOĞRUDAN MADDELER)
                formCol.Item().BorderBottom(1).BorderColor(Colors.Black).Padding(5).Column(notesCol =>
                {
                    notesCol.Item().Text("1- Taşıt görev emri iki nüsha olarak düzenlenecek olup bir nüshası kurumda kalacak diğer nüsha araçta bulundurulacaktır.").FontSize(7.5f);
                    notesCol.Item().PaddingTop(1.5f).Text("2- Taşıt görev emri yetkililerin istemesi halinde gösterilecektir.").FontSize(7.5f);
                    notesCol.Item().PaddingTop(1.5f).Text("3- Araç sürücüleri ve araçta bulunan personel resmi sıfatın gerektirdiği saygınlığa uygun davranışlarda bulunacaklardır.").FontSize(7.5f);
                });

                // 5. BÖLÜM: 5 BÜYÜK KARE KUTU VE PARAF KUTUSU
                formCol.Item().Table(checkTable =>
                {
                    checkTable.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.ConstantColumn(85);
                    });

                    var oilChecked = !_emptyValues && _model.CheckOil;
                    var waterChecked = !_emptyValues && _model.CheckWater;
                    var fuelChecked = !_emptyValues && _model.CheckFuel;
                    var hydChecked = !_emptyValues && _model.CheckHydraulic;
                    var tireChecked = !_emptyValues && _model.CheckTires;

                    ComposeCheckCell(checkTable.Cell().BorderRight(1).BorderColor(Colors.Black), "Yağ Kontrol", oilChecked);
                    ComposeCheckCell(checkTable.Cell().BorderRight(1).BorderColor(Colors.Black), "Su Kontrol", waterChecked);
                    ComposeCheckCell(checkTable.Cell().BorderRight(1).BorderColor(Colors.Black), "Yakıt Kontrol", fuelChecked);
                    ComposeCheckCell(checkTable.Cell().BorderRight(1).BorderColor(Colors.Black), "Hidrolik Kontrol", hydChecked);
                    ComposeCheckCell(checkTable.Cell().BorderRight(1).BorderColor(Colors.Black), "Lastik Kontrol", tireChecked);

                    // PARAF Kutusu
                    checkTable.Cell().Padding(4).AlignCenter().AlignMiddle().Column(pCol =>
                    {
                        pCol.Item().AlignCenter().Text("PARAF").Bold().FontSize(8.5f);
                        pCol.Item().Height(24); // Boş paraf alanı
                    });
                });
            });
        });
    }

    private static void ComposeCheckCell(IContainer container, string title, bool isChecked)
    {
        container.Padding(4).AlignCenter().AlignMiddle().Column(col =>
        {
            // Büyük kare kutu (X işaretli veya boş)
            col.Item().AlignCenter().Width(20).Height(20).Border(1.2f).BorderColor(Colors.Black)
                .AlignCenter().AlignMiddle().Text(isChecked ? "X" : "").Bold().FontSize(12);
            
            col.Item().PaddingTop(3).AlignCenter().Text(title).Bold().FontSize(7.5f);
        });
    }
}
