using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CareHub.Desktop.Services;

/// <summary>DPAPI-protected refresh token for the current Windows user (dev desktop).</summary>
public static class RefreshTokenStore
{
    private static readonly byte[] Entropy = "CareHubDesktopRefreshV1"u8.ToArray();

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CareHub",
            "refresh.dat");

    public static void Save(string refreshToken)
    {
        var raw = Encoding.UTF8.GetBytes(refreshToken);
        var blob = ProtectedData.Protect(raw, Entropy, DataProtectionScope.CurrentUser);
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(FilePath, blob);
    }

    public static string? Load()
    {
        if (!File.Exists(FilePath)) return null;
        var blob = File.ReadAllBytes(FilePath);
        try
        {
            var raw = ProtectedData.Unprotect(blob, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(raw);
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch
        {
            /* ignore */
        }
    }
}
