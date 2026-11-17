using System.Text.RegularExpressions;

namespace BtPlatform.Core.Services;

// Gerçek makinenin okunamayan bilgileri: formdan elle girilir
public record RealComputerForm(
    string AssetTag, string AssignedUser, string Department,
    string Location, int Vlan, DateTime WarrantyEnd);

// Form girdisine güvenmeyiz: sunucuda yeniden doğrulanır
public static class RealComputerFormValidator
{
    private static readonly Regex TagPattern = new("^[A-Za-z0-9-]{3,20}$", RegexOptions.Compiled);

    public static List<string> Validate(RealComputerForm f, DateTime today)
    {
        var errors = new List<string>();

        if (!TagPattern.IsMatch((f.AssetTag ?? "").Trim()))
            errors.Add("Zimmet etiketi 3-20 karakter olmalı (harf, rakam, tire).");
        if (string.IsNullOrWhiteSpace(f.AssignedUser) || f.AssignedUser.Trim().Length > 100)
            errors.Add("Kullanıcı adı boş olamaz ve 100 karakteri geçemez.");
        if (string.IsNullOrWhiteSpace(f.Department) || f.Department.Trim().Length > 100)
            errors.Add("Departman boş olamaz ve 100 karakteri geçemez.");
        if ((f.Location ?? "").Trim().Length > 100)
            errors.Add("Konum 100 karakteri geçemez.");
        if (f.Vlan < 1 || f.Vlan > 4094)
            errors.Add("VLAN 1 ile 4094 arasında olmalı.");
        if (f.WarrantyEnd < today.AddYears(-10) || f.WarrantyEnd > today.AddYears(10))
            errors.Add("Garanti bitiş tarihi makul bir aralıkta olmalı (±10 yıl).");

        return errors;
    }
}