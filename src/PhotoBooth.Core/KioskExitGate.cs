using System.Security.Cryptography;

namespace PhotoBooth.Core;

/// <summary>
/// Application-level PIN check only, not an OS kiosk boundary. Persist the credential
/// and lockout state using Windows protected storage before shipping a Windows host.
/// </summary>
public sealed class KioskExitGate
{
    private const int Iterations = 600_000;
    private readonly byte[] salt;
    private readonly byte[] expectedHash;
    private readonly TimeProvider time;
    private int failures;
    private DateTimeOffset lockedUntil;

    private KioskExitGate(byte[] salt, byte[] expectedHash, TimeProvider time)
    {
        this.salt = salt;
        this.expectedHash = expectedHash;
        this.time = time;
    }

    public static KioskExitGate Create(string pin, TimeProvider? time = null)
    {
        if (!IsPin(pin)) throw new ArgumentException("Choose 6 to 12 ASCII digits; there is no default PIN.", nameof(pin));
        var salt = RandomNumberGenerator.GetBytes(16);
        return new(salt, Hash(pin, salt), time ?? TimeProvider.System);
    }

    public bool TryAuthorizeExit(string pin)
    {
        var now = time.GetUtcNow();
        if (now < lockedUntil) return false;
        if (lockedUntil != default)
        {
            failures = 0;
            lockedUntil = default;
        }
        var valid = false;
        if (IsPin(pin))
        {
            var candidate = Hash(pin, salt);
            try { valid = CryptographicOperations.FixedTimeEquals(candidate, expectedHash); }
            finally { CryptographicOperations.ZeroMemory(candidate); }
        }
        if (valid) { failures = 0; return true; }
        if (++failures >= 5) lockedUntil = now.AddSeconds(30);
        return false;
    }

    private static bool IsPin(string? pin) => pin is { Length: >= 6 and <= 12 } && pin.All(c => c is >= '0' and <= '9');
    private static byte[] Hash(string pin, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, 32);
}
