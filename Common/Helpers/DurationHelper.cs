// File: Common/Helpers/DurationHelper.cs
using System;

namespace aractakipsys.Common.Helpers;

public static class DurationHelper
{
    /// <summary>
    /// Verilen süreyi "X sa Y dk" biçiminde biçimlendirir (dakika 0-59).
    /// Asla ondalık saat döndürmez.
    /// </summary>
    public static string FormatDuration(TimeSpan? duration)
    {
        if (!duration.HasValue || duration.Value.TotalSeconds <= 0)
        {
            return "0 sa 0 dk";
        }

        var totalMinutes = (int)Math.Round(duration.Value.TotalMinutes);
        if (totalMinutes <= 0)
        {
            return "0 sa 0 dk";
        }

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return $"{hours} sa {minutes} dk";
    }

    /// <summary>
    /// Başlangıç ve bitiş zamanı arasındaki süreyi "X sa Y dk" biçiminde biçimlendirir.
    /// </summary>
    public static string FormatDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue || !end.HasValue || end.Value <= start.Value)
        {
            return "-";
        }

        return FormatDuration(end.Value - start.Value);
    }

    /// <summary>
    /// Ondalık saat değerini (örn: 2.67 saat) "X sa Y dk" biçimine dönüştürür.
    /// </summary>
    public static string FormatDuration(decimal? decimalHours)
    {
        if (!decimalHours.HasValue || decimalHours.Value <= 0)
        {
            return "-";
        }

        var totalMinutes = (int)Math.Round((double)(decimalHours.Value * 60m));
        if (totalMinutes <= 0)
        {
            return "0 sa 0 dk";
        }

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return $"{hours} sa {minutes} dk";
    }

    // Extension Metotlar
    public static string FormatDurationHours(decimal? decimalHours) => FormatDuration(decimalHours);
    public static string ToDurationString(this TimeSpan timeSpan) => FormatDuration(timeSpan);
    public static string ToDurationString(this TimeSpan? timeSpan) => FormatDuration(timeSpan);
    public static string ToFormattedDuration(this decimal? decimalHours) => FormatDuration(decimalHours);
    public static string ToFormattedDuration(this decimal decimalHours) => FormatDuration((decimal?)decimalHours);
}
