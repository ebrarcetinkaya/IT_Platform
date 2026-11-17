using System.Text.RegularExpressions;

namespace BtPlatform.Core.Services;

// Winget'in "güncellenebilir uygulama" satırı
public record WingetUpgrade(string Name, string Id, string CurrentVersion, string AvailableVersion, string Source);

// TableFound = false: çıktıda tablo yok (güncelleme yok, boş çıktı ya da beklenmeyen metin).
// Bu durumu "hata" mı "güncelleme yok" mu saymak toplayıcının (15b) kararıdır.
public record WingetParseResult(IReadOnlyList<WingetUpgrade> Items, bool TableFound);

// "winget upgrade" çıktısını okur. Çıktıya güvenmeyiz: boyut ve satır sayısı sınırlıdır.
// Başlık dili Windows diline göre değişir; bu yüzden başlık metnine değil, tire çizgisine
// ve başlıktaki sütun konumlarına bakarız.
public static class WingetOutputParser
{
    public const int MaxItems = 100;
    public const int MaxChars = 256 * 1024;

    // Başlık hücreleri en az iki boşlukla ayrılır; tek boşluk hücrenin içindedir
    private static readonly Regex HeaderCell = new(@"\S+(?: \S+)*", RegexOptions.Compiled);

    public static WingetParseResult Parse(string? output)
    {
        var none = new WingetParseResult(new List<WingetUpgrade>(), false);
        if (string.IsNullOrWhiteSpace(output) || output.Length > MaxChars) return none;

        // \r\n önce gelir; ilerleme çubuğundan kalan tek \r'lar da satır sonu sayılır
        var lines = output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        // Tire çizgisi: en az 20 tire, üstünde dolu bir başlık satırı
        var sep = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            var t = lines[i].Trim();
            if (t.Length >= 20 && t.All(c => c == '-') && lines[i - 1].Trim().Length > 0)
            {
                sep = i;
                break;
            }
        }
        if (sep < 0) return none;

        var starts = HeaderCell.Matches(lines[sep - 1]).Select(m => m.Index).ToList();
        if (starts.Count < 4) return none;   // Ad, Kimlik, Sürüm, Yeni sürüm şart; Kaynak isteğe bağlı

        var items = new List<WingetUpgrade>();
        for (var i = sep + 1; i < lines.Length && items.Count < MaxItems; i++)
        {
            var line = lines[i];

            // Boş satır ya da kimlik sütununa bile ulaşmayan kısa satır: tablo bitti (altbilgi)
            if (string.IsNullOrWhiteSpace(line) || line.Length <= starts[1]) break;

            var name = Cell(line, starts, 0);
            var id = Cell(line, starts, 1);
            if (name == "" || id == "" || id.Contains(' ')) break;   // uyarı metni, tablo değil

            items.Add(new WingetUpgrade(
                Cut(name, 100), Cut(id, 100),
                Cut(Cell(line, starts, 2), 50), Cut(Cell(line, starts, 3), 50),
                Cut(Cell(line, starts, 4), 30)));
        }

        return new WingetParseResult(items, true);
    }

    private static string Cell(string line, List<int> starts, int col)
    {
        if (col >= starts.Count || starts[col] >= line.Length) return "";
        var from = starts[col];
        var to = col + 1 < starts.Count ? Math.Min(starts[col + 1], line.Length) : line.Length;
        return line.Substring(from, to - from).Trim();
    }

    private static string Cut(string s, int max) => s.Length > max ? s[..max] : s;
}