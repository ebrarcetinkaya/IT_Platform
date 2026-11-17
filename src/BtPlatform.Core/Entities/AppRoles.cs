namespace BtPlatform.Core.Entities;

// Rol adlarını her yerde elle yazmak yerine tek yerde tutuyoruz (yazım hatasını önler).
// Değerler UserRole enum adlarıyla aynı olmak zorunda: giriş sırasında cookie'ye enum adı yazılıyor.
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Technician = "Technician";
    public const string Viewer = "Viewer";

    // İşlem yapabilen roller (izleyici hariç)
    public const string Operators = "Admin,Technician";
}