using System.Text.RegularExpressions;

namespace BtPlatform.Core.Services;

// Sürüm metinlerini sayı parçalarına bölerek karşılaştırır.
// Metin olarak karşılaştırma yanlış sonuç verir: "16.0.10" < "16.0.2" olurdu.
public static class SoftwareVersion
{
    private static readonly Regex Digits = new(@"\d+", RegexOptions.Compiled);

    // "v2.1.0-beta" -> [2, 1, 0]
    private static List<long> Parse(string? version)
    {
        var parts = new List<long>();
        if (string.IsNullOrWhiteSpace(version))
        {
            parts.Add(0);
            return parts;
        }

        foreach (Match m in Digits.Matches(version))
            parts.Add(long.TryParse(m.Value, out var n) ? n : long.MaxValue);

        if (parts.Count == 0) parts.Add(0);
        return parts;
    }

    // Küçükse -1, eşitse 0, büyükse 1. Eksik parçalar 0 sayılır ("1.0" == "1.0.0").
    public static int Compare(string? a, string? b)
    {
        var x = Parse(a);
        var y = Parse(b);
        var length = Math.Max(x.Count, y.Count);

        for (var i = 0; i < length; i++)
        {
            var xi = i < x.Count ? x[i] : 0;
            var yi = i < y.Count ? y[i] : 0;
            if (xi != yi) return xi < yi ? -1 : 1;
        }
        return 0;
    }

    public static bool IsOlder(string? installed, string? latest) => Compare(installed, latest) < 0;

    // Sürümün son sayı parçasını kaydırır (örnek veri üretmek için): "23.01", +1 -> "23.02"
    public static string ShiftLast(string version, int delta)
    {
        var matches = Digits.Matches(version);
        if (matches.Count == 0) return version;

        var last = matches[matches.Count - 1];
        var value = long.TryParse(last.Value, out var n) ? n : 0;
        var shifted = Math.Max(0, value + delta);
        var text = shifted.ToString().PadLeft(last.Value.Length, '0');

        return version[..last.Index] + text + version[(last.Index + last.Length)..];
    }
}