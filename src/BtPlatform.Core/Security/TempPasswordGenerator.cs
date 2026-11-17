using System.Security.Cryptography;

namespace BtPlatform.Core.Security;

// Parola sıfırlamada kullanıcıya verilecek geçici parolayı üretir.
public static class TempPasswordGenerator
{
    // Karıştırılan karakterler (0/O, 1/l/I) bilerek çıkarıldı: parola telefonda ya da
    // kağıttan okunurken hata olmasın.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*?";

    public static string Generate(int length = 12)
    {
        if (length < 8) throw new ArgumentOutOfRangeException(nameof(length), "En az 8 karakter olmalı.");

        var all = Upper + Lower + Digits + Symbols;

        // Her karakter sınıfından en az bir tane (AD parola karmaşıklık kuralı)
        var chars = new List<char> { Pick(Upper), Pick(Lower), Pick(Digits), Pick(Symbols) };
        while (chars.Count < length) chars.Add(Pick(all));

        // Fisher-Yates karıştırma: ilk dört karakterin sabit sırada kalmasını önler
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }

    // Random yerine kriptografik rastgelelik: tahmin edilemez
    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}