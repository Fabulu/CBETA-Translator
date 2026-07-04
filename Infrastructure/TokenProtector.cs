// Infrastructure/TokenProtector.cs
using System;
using System.Security.Cryptography;
using System.Text;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// At-rest protection for the persisted GitHub OAuth token (audit P1.5 / R3-H3).
///
/// The portable install keeps config.json next to the exe by design (user decision D8),
/// which used to mean a plaintext OAuth token in the app folder. On Windows the token is
/// now wrapped with DPAPI (CurrentUser scope) before it hits disk; the in-memory
/// AppConfig always carries the plaintext.
///
/// Protected values are marked with the "dpapi:v1:" prefix so legacy plaintext configs
/// keep loading (and get migrated on their next save). Unprotecting on a different
/// Windows user or machine fails BY DESIGN — for a portable folder that moved hosts the
/// token is dropped and the user re-authenticates, which is the safe outcome.
/// On non-Windows platforms there is no DPAPI; values pass through unchanged (same
/// behavior as before this change).
/// </summary>
public static class TokenProtector
{
    private const string Prefix = "dpapi:v1:";

    // App-specific entropy: prevents another DPAPI-using program of the same Windows
    // user from transparently unprotecting our blob.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ReadZen.GitHubAccessToken.v1");

    public static bool IsProtected(string? value)
        => value != null && value.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Wraps a plaintext secret for persistence. Pass-through off Windows.</summary>
    public static string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;
        if (!OperatingSystem.IsWindows()) return plaintext;

        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext), Entropy, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Returns the plaintext for a stored value: unprotects "dpapi:v1:" values, passes
    /// legacy plaintext through, and returns null when unprotection fails (different
    /// user/machine or corrupt blob) so the caller treats the token as absent.
    /// </summary>
    public static string? TryUnprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return stored;
        if (!IsProtected(stored)) return stored; // legacy plaintext config
        if (!OperatingSystem.IsWindows()) return null; // protected blob is Windows-only

        try
        {
            var bytes = Convert.FromBase64String(stored.Substring(Prefix.Length));
            var plain = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }
}
