// File: Common/Helpers/AvatarHelper.cs
namespace aractakipsys.Common.Helpers;

public static class AvatarHelper
{
    /// <summary>
    /// Kullanıcı Ad Soyad veya Kullanıcı Adı üzerinden 2 karakterlik rozet/baş harf metni üretir. (Örn: "Ahmet Yılmaz" -> "AY")
    /// </summary>
    public static string GetInitials(string? fullName, string? userName = null)
    {
        var nameToUse = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : userName?.Trim();
        if (string.IsNullOrWhiteSpace(nameToUse))
        {
            return "U";
        }

        var parts = nameToUse.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var single = parts[0];
            return single.Length >= 2
                ? single.Substring(0, 2).ToUpperInvariant()
                : single.Substring(0, 1).ToUpperInvariant();
        }

        var firstChar = char.ToUpperInvariant(parts[0][0]);
        var lastChar = char.ToUpperInvariant(parts[^1][0]);
        return $"{firstChar}{lastChar}";
    }
}
