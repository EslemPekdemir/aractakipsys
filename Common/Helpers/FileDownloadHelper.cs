// File: Common/Helpers/FileDownloadHelper.cs
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace aractakipsys.Common.Helpers;

public static class FileDownloadHelper
{
    /// <summary>
    /// HTTP başlıklarında (Content-Disposition) RFC 7230 gereği non-ASCII karakterler hata (0x0131 vb.) fırlatır.
    /// Bu yardımcı metod dosya adındaki Türkçe karakterleri güvenli ASCII eşdeğerlerine dönüştürür.
    /// </summary>
    public static string ToSafeAsciiFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "belge.pdf";

        var sb = new StringBuilder();
        foreach (var c in fileName)
        {
            switch (c)
            {
                case 'ı': sb.Append('i'); break;
                case 'İ': sb.Append('I'); break;
                case 'ğ': sb.Append('g'); break;
                case 'Ğ': sb.Append('G'); break;
                case 'ü': sb.Append('u'); break;
                case 'Ü': sb.Append('U'); break;
                case 'ş': sb.Append('s'); break;
                case 'Ş': sb.Append('S'); break;
                case 'ö': sb.Append('o'); break;
                case 'Ö': sb.Append('O'); break;
                case 'ç': sb.Append('c'); break;
                case 'Ç': sb.Append('C'); break;
                case ' ': sb.Append('_'); break;
                default:
                    if (c < 128 && !char.IsControl(c) && c != '"' && c != '\\' && c != ';' && c != ':')
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        var result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? "belge.pdf" : result;
    }

    /// <summary>
    /// HttpResponse başlığına Content-Disposition (inline veya attachment) güvenli bir şekilde ekler.
    /// </summary>
    public static void SetContentDisposition(HttpResponse response, string fileName, bool preview = false)
    {
        var safeAscii = ToSafeAsciiFileName(fileName);
        var cd = new ContentDispositionHeaderValue(preview ? "inline" : "attachment");
        cd.SetHttpFileName(safeAscii);
        response.Headers[HeaderNames.ContentDisposition] = cd.ToString();
    }
}
