using System.Numerics;
using System.Security.Cryptography;

namespace Codexus.ExampleMod.Extensions;

public static class ProtocolExtensions
{
    public static string ToServerId(this MemoryStream data)
    {
        string text;
        using var sha = SHA1.Create();
        var array = sha.ComputeHash(data);

        Array.Reverse(array);
        var bigInteger = new BigInteger(array);
        if (bigInteger < 0L)
            text = "-" + (-bigInteger).ToString("x").TrimStart('0');
        else
            text = bigInteger.ToString("x").TrimStart('0');

        return text;
    }
}