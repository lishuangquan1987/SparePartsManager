using System.Security.Cryptography;

namespace SparePartsManager;

/// <summary>
/// .NET Framework 4.7.2 兼容性辅助方法
/// </summary>
internal static class Compat
{
    public static string ToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    public static byte[] FromHex(string hex)
    {
        if (hex.Length % 2 != 0) throw new ArgumentException("Invalid hex string length");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    public static byte[] GetRandomBytes(int count)
    {
        var bytes = new byte[count];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    public static byte[] Pbkdf2Hash(string password, byte[] salt, int iterations, int outputLength)
    {
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(outputLength);
    }
}

internal static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default!)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
}

internal static class EnumerableExtensions
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        return new HashSet<T>(source);
    }
}
