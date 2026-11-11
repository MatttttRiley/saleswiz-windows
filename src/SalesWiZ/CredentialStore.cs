using System.Security.Cryptography;
using System.Text;
using Windows.Security.Credentials;

namespace SalesWiZ;

/// <summary>
/// Stores the shared CRM password on the PC only.
/// Primary: Windows Credential Locker (PasswordVault, resource "SalesWiZ").
/// Fallback: DPAPI-encrypted file under %LOCALAPPDATA%\SalesWiZ, used only
/// if the Locker is unavailable on this machine. The password is never
/// written to the repo, logs, or the app package.
/// </summary>
public static class CredentialStore
{
    private const string Resource = "SalesWiZ";
    private const string UserName = "crm-password";

    public static bool SavePassword(string password)
    {
        try
        {
            var vault = new PasswordVault();
            RemoveFromVault(vault);
            vault.Add(new PasswordCredential(Resource, UserName, password));
            return true;
        }
        catch
        {
            return SaveToDpapiFile(password);
        }
    }

    public static string? ReadPassword()
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.FindAllByResource(Resource).FirstOrDefault();
            if (credential is null) return ReadFromDpapiFile();
            credential.RetrievePassword();
            return string.IsNullOrEmpty(credential.Password) ? null : credential.Password;
        }
        catch
        {
            return ReadFromDpapiFile();
        }
    }

    public static void DeletePassword()
    {
        try
        {
            RemoveFromVault(new PasswordVault());
        }
        catch
        {
            // Best effort; fall through to the file.
        }
        try
        {
            var path = DpapiPath();
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best effort.
        }
    }

    private static void RemoveFromVault(PasswordVault vault)
    {
        foreach (var credential in vault.FindAllByResource(Resource))
        {
            vault.Remove(credential);
        }
    }

    // MARK: DPAPI fallback

    private static string DpapiPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SalesWiZ");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "credentials.dat");
    }

    private static bool SaveToDpapiFile(string password)
    {
        try
        {
            var bytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(DpapiPath(), bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadFromDpapiFile()
    {
        try
        {
            var path = DpapiPath();
            if (!File.Exists(path)) return null;
            var bytes = ProtectedData.Unprotect(
                File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            var password = Encoding.UTF8.GetString(bytes);
            return string.IsNullOrEmpty(password) ? null : password;
        }
        catch
        {
            return null;
        }
    }
}
